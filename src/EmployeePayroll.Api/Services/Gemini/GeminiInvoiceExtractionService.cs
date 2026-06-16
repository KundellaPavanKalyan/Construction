using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace EmployeePayroll.Api.Services.Gemini;

public sealed class GeminiInvoiceExtractionService(
    GeminiService gemini,
    InvoiceMediaPreparer mediaPreparer,
    InvoiceDocumentExtractor regexExtractor,
    IOptions<GeminiOptions> options,
    ILogger<GeminiInvoiceExtractionService> logger)
{
    public async Task<InvoiceExtractionResult> ExtractAsync(Stream stream, string extension, CancellationToken ct)
    {
        extension = extension.ToLowerInvariant();
        stream.Position = 0;

        if (!gemini.IsConfigured)
        {
            stream.Position = 0;
            var offline = regexExtractor.Extract(stream, extension);
            var note = string.IsNullOrWhiteSpace(offline.Notes)
                ? "Gemini API key not configured — used text extraction. Add Gemini:ApiKey in appsettings or user secrets."
                : offline.Notes + " (Gemini API key not configured.)";
            return Copy(offline, notes: note);
        }

        try
        {
            var parts = mediaPreparer.Prepare(stream, extension);
            if (parts.Count == 0)
            {
                stream.Position = 0;
                return regexExtractor.Extract(stream, extension);
            }

            var jsonText = await gemini.GenerateJsonAsync(parts, ct);
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                logger.LogInformation("Gemini returned no JSON for {Extension}; falling back to regex extraction.", extension);
                stream.Position = 0;
                return Fallback(stream, extension, "Gemini could not extract data — used local text parsing.");
            }

            var parsed = ParseGeminiJson(jsonText);
            if (parsed is null)
            {
                stream.Position = 0;
                return Fallback(stream, extension, "Gemini response was not valid JSON — used local text parsing.");
            }

            stream.Position = 0;
            var localText = extension is ".docx" or ".pdf"
                ? regexExtractor.Extract(stream, extension).FullText
                : string.Empty;

            var fieldsFound = CountFields(parsed);
            var status = fieldsFound >= 3 ? "Success" : fieldsFound >= 1 ? "Partial" : "Partial";
            var model = options.Value.Model;

            return new InvoiceExtractionResult
            {
                FullText = localText,
                InvoiceNumber = parsed.InvoiceNumber,
                InvoiceDate = parsed.InvoiceDate,
                VendorName = parsed.VendorName,
                SgstAmount = parsed.SgstAmount,
                CgstAmount = parsed.CgstAmount,
                IgstAmount = parsed.IgstAmount,
                TransportCharges = parsed.TransportCharges,
                BasicTotal = parsed.BasicTotal,
                TotalAmount = parsed.TotalAmount,
                Status = status,
                Notes = $"Extracted via Google Gemini ({model}). Review fields before final save."
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Gemini extraction failed for {Extension}.", extension);
            stream.Position = 0;
            return Fallback(stream, extension, $"Gemini error: {ex.Message}. Used local text parsing.");
        }
    }

    private InvoiceExtractionResult Fallback(Stream stream, string extension, string note)
    {
        var result = regexExtractor.Extract(stream, extension);
        var merged = string.IsNullOrWhiteSpace(result.Notes) ? note : $"{result.Notes} {note}";
        return Copy(result, notes: merged);
    }

    private static InvoiceExtractionResult Copy(InvoiceExtractionResult source, string? notes = null) =>
        new()
        {
            FullText = source.FullText,
            InvoiceNumber = source.InvoiceNumber,
            InvoiceDate = source.InvoiceDate,
            VendorName = source.VendorName,
            SgstAmount = source.SgstAmount,
            CgstAmount = source.CgstAmount,
            IgstAmount = source.IgstAmount,
            TransportCharges = source.TransportCharges,
            BasicTotal = source.BasicTotal,
            TotalAmount = source.TotalAmount,
            Status = source.Status,
            Notes = notes ?? source.Notes
        };

    private static InvoiceExtractionResult? ParseGeminiJson(string raw)
    {
        raw = StripMarkdownFence(raw);
        try
        {
            var dto = JsonSerializer.Deserialize<GeminiInvoiceJson>(raw, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (dto is null) return null;

            return new InvoiceExtractionResult
            {
                InvoiceNumber = NullIfEmpty(dto.InvoiceNumber),
                InvoiceDate = ParseFlexibleDate(dto.InvoiceDate),
                VendorName = NullIfEmpty(dto.VendorName),
                CgstAmount = dto.Cgst,
                SgstAmount = dto.Sgst,
                IgstAmount = dto.Igst,
                BasicTotal = dto.BasicTotal,
                TransportCharges = dto.TransportCharges,
                TotalAmount = dto.TotalAmount
            };
        }
        catch
        {
            return null;
        }
    }

    private static string StripMarkdownFence(string text)
    {
        text = text.Trim();
        var m = Regex.Match(text, @"```(?:json)?\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : text;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateOnly? ParseFlexibleDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        raw = raw.Trim();

        if (DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso))
            return iso;

        var formats = new[]
        {
            "d-MMM-yyyy", "dd-MMM-yyyy", "d-MMMM-yyyy", "dd-MMMM-yyyy",
            "d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy"
        };
        foreach (var fmt in formats)
        {
            if (DateOnly.TryParseExact(raw, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;
        }

        return DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var loose) ? loose : null;
    }

    private static int CountFields(InvoiceExtractionResult r) =>
        (string.IsNullOrWhiteSpace(r.InvoiceNumber) ? 0 : 1)
        + (r.InvoiceDate is null ? 0 : 1)
        + (string.IsNullOrWhiteSpace(r.VendorName) ? 0 : 1)
        + (r.SgstAmount is null ? 0 : 1)
        + (r.CgstAmount is null ? 0 : 1)
        + (r.IgstAmount is null ? 0 : 1)
        + (r.TransportCharges is null ? 0 : 1)
        + (r.BasicTotal is null ? 0 : 1)
        + (r.TotalAmount is null ? 0 : 1);
}
