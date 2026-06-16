using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace EmployeePayroll.Api.Services;

public sealed class InvoiceDocumentExtractor
{
    private static readonly Regex InvoiceNumberRx = new(
        @"(?i)(?:invoice\s*no\.?|invoice\s*number|inv\.?\s*no\.?|bill\s*no\.?)\s*[-–:#]?\s*([0-9A-Z][0-9A-Z\-\/\.]+)",
        RegexOptions.Compiled);

    private static readonly Regex InvoiceNumberSlashRx = new(
        @"\b(\d{2,6}\/[A-Z0-9]{1,10}\/\d{2}-\d{2})\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex InvoiceDateNamedRx = new(
        @"(?i)(?:invoice\s*date|bill\s*date|dated|date)\s*[-–:.]?\s*(\d{1,2}[\-\/\.\s]+(?:jan|feb|mar|apr|may|jun|jul|aug|sep|sept|oct|nov|dec)[a-z]*[\-\/\.\s]+\d{2,4})",
        RegexOptions.Compiled);

    private static readonly Regex InvoiceDateNumericRx = new(
        @"(?i)(?:invoice\s*date|bill\s*date|dated|date)\s*[-–:.]?\s*(\d{1,2}[\-/\.]\d{1,2}[\-/\.]\d{2,4})",
        RegexOptions.Compiled);

    private static readonly Regex LooseDateNamedRx = new(
        @"\b(\d{1,2}[\-\/\.\s]+(?:jan|feb|mar|apr|may|jun|jul|aug|sep|sept|oct|nov|dec)[a-z]*[\-\/\.\s]+\d{2,4})\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SgstRx = new(@"(?i)\bsgst\b\s*[-–:.]?\s*(?:₹|rs\.?|inr)?\s*([\d,]+(?:\.\d{1,2})?)", RegexOptions.Compiled);
    private static readonly Regex CgstRx = new(@"(?i)\bcgst\b\s*[-–:.]?\s*(?:₹|rs\.?|inr)?\s*([\d,]+(?:\.\d{1,2})?)", RegexOptions.Compiled);
    private static readonly Regex IgstRx = new(@"(?i)\bigst\b\s*[-–:.]?\s*(?:₹|rs\.?|inr)?\s*([\d,]+(?:\.\d{1,2})?)", RegexOptions.Compiled);
    private static readonly Regex TransportRx = new(
        @"(?i)transport(?:ation)?\s*(?:charges?|cost|fee)?\s*[-–:.]?\s*(?:₹|rs\.?|inr)?\s*([\d,]+(?:\.\d{1,2})?)",
        RegexOptions.Compiled);
    private static readonly Regex BasicTotalRx = new(
        @"(?i)basic\s*total\s*[-–:.]?\s*(?:₹|rs\.?|inr)?\s*([\d,]+(?:\.\d{1,2})?)",
        RegexOptions.Compiled);
    private static readonly Regex TotalRx = new(
        @"(?i)(?:grand\s*total|total\s*amount|totalamount|net\s*(?:amount|payable)|amount\s*due)\s*[-–:.]?\s*(?:₹|rs\.?|inr)?\s*([\d,]+(?:\.\d{1,2})?)",
        RegexOptions.Compiled);

    private static readonly Regex VendorRx = new(
        @"(?i)(?:vendor|vender|vendorname|supplier|party\s*name|sold\s*by|from|m/s)\s*[-–:.]?\s*(.+)",
        RegexOptions.Compiled);

    private static readonly Regex CompanyLineRx = new(
        @"(?i)^[A-Za-z][A-Za-z0-9\s&\.\-]{4,90}(?:pvt\.?\s*ltd\.?|ltd\.?|llp|inc\.?|engineering|traders|industries).*$",
        RegexOptions.Compiled);

    public InvoiceExtractionResult Extract(Stream stream, string extension)
    {
        try
        {
            var fullText = extension switch
            {
                ".pdf" => ExtractPdfText(stream),
                ".docx" => ExtractDocxText(stream),
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(fullText))
            {
                return new InvoiceExtractionResult
                {
                    FullText = string.Empty,
                    Status = "Failed",
                    Notes = "No readable text found. Scanned PDFs need OCR."
                };
            }

            var normalized = NormalizeText(fullText);
            var invoiceNumber = ParseInvoiceNumber(normalized);
            var invoiceDate = ParseDate(MatchFirst(InvoiceDateNamedRx, normalized))
                              ?? ParseDate(MatchFirst(InvoiceDateNumericRx, normalized))
                              ?? ParseDate(MatchFirst(LooseDateNamedRx, normalized));
            var vendor = ParseVendor(normalized);
            var sgst = ParseAmount(FindLastAmountMatch(SgstRx, normalized));
            var cgst = ParseAmount(FindLastAmountMatch(CgstRx, normalized));
            var igst = ParseAmount(FindLastAmountMatch(IgstRx, normalized));
            var transport = ParseAmount(FindLastAmountMatch(TransportRx, normalized));
            var basic = ParseAmount(FindLastAmountMatch(BasicTotalRx, normalized));
            var total = ParseAmount(FindLastAmountMatch(TotalRx, normalized));

            var fieldsFound = CountFields(invoiceNumber, invoiceDate, vendor, sgst, cgst, igst, transport, basic, total);
            var status = fieldsFound >= 3 ? "Success" : fieldsFound >= 1 ? "Partial" : "Partial";

            return new InvoiceExtractionResult
            {
                FullText = fullText,
                InvoiceNumber = invoiceNumber,
                InvoiceDate = invoiceDate,
                VendorName = vendor,
                SgstAmount = sgst,
                CgstAmount = cgst,
                IgstAmount = igst,
                TransportCharges = transport,
                BasicTotal = basic,
                TotalAmount = total,
                Status = status,
                Notes = fieldsFound >= 3
                    ? "Fields mapped to invoice table from document text."
                    : "Some fields missing — check document labels (Invoice No, Date, SGST, etc.)."
            };
        }
        catch (Exception ex)
        {
            return new InvoiceExtractionResult
            {
                Status = "Failed",
                Notes = $"Document read error: {ex.Message}"
            };
        }
    }

    private static string NormalizeText(string text) =>
        text.Replace('\u00A0', ' ')
            .Replace("–", "-")
            .Replace("—", "-");

    private static string? ParseInvoiceNumber(string text)
    {
        var fromLabel = MatchFirst(InvoiceNumberRx, text);
        if (!string.IsNullOrWhiteSpace(fromLabel)) return fromLabel.Trim();

        var slash = MatchFirst(InvoiceNumberSlashRx, text);
        if (!string.IsNullOrWhiteSpace(slash)) return slash.Trim();

        return null;
    }

    private static int CountFields(
        string? invoiceNumber, DateOnly? invoiceDate, string? vendor,
        decimal? sgst, decimal? cgst, decimal? igst, decimal? transport, decimal? basic, decimal? total)
    {
        var n = 0;
        if (!string.IsNullOrWhiteSpace(invoiceNumber)) n++;
        if (invoiceDate is not null) n++;
        if (!string.IsNullOrWhiteSpace(vendor)) n++;
        if (sgst is not null) n++;
        if (cgst is not null) n++;
        if (igst is not null) n++;
        if (transport is not null) n++;
        if (basic is not null) n++;
        if (total is not null) n++;
        return n;
    }

    private static string ExtractPdfText(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        var sb = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            if (!string.IsNullOrWhiteSpace(page.Text))
                sb.AppendLine(page.Text);
        }
        return sb.ToString().Trim();
    }

    private static string ExtractDocxText(Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, false);
        return doc.MainDocumentPart?.Document?.Body?.InnerText?.Trim() ?? string.Empty;
    }

    private static string? MatchFirst(Regex rx, string text)
    {
        var m = rx.Match(text);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static string? FindLastAmountMatch(Regex rx, string text)
    {
        Match? last = null;
        foreach (Match m in rx.Matches(text))
            last = m;
        return last is { Success: true } ? last.Groups[1].Value : null;
    }

    private static decimal? ParseAmount(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var cleaned = raw.Replace(",", "").Trim();
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static DateOnly? ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        raw = Regex.Replace(raw.Trim(), @"\s+", "-");
        var formats = new[]
        {
            "d-MMM-yyyy", "dd-MMM-yyyy", "d-MMMM-yyyy", "dd-MMMM-yyyy",
            "d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy",
            "d.M.yyyy", "dd.MM.yyyy"
        };
        foreach (var fmt in formats)
        {
            if (DateOnly.TryParseExact(raw, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;
        }
        return DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var loose) ? loose : null;
    }

    private static string? ParseVendor(string text)
    {
        var m = VendorRx.Match(text);
        if (m.Success)
        {
            var line = m.Groups[1].Value.Split(['\n', '\r'])[0].Trim();
            if (line.Length >= 3 && line.Length <= 200) return line;
        }

        foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var t = line.Trim();
            if (CompanyLineRx.IsMatch(t)) return t;
        }

        foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var t = line.Trim();
            if (t.Length < 5 || t.Length > 120) continue;
            if (Regex.IsMatch(t, @"(?i)invoice|tax|gst|total|bill to|ship to|page \d|amount|rupees")) continue;
            if (Regex.IsMatch(t, @"(?i)pvt|ltd|engineering|traders|industries|llp"))
                return t;
        }

        return null;
    }
}
