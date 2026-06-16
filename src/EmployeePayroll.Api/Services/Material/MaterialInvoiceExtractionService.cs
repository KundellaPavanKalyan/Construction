using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using EmployeePayroll.Api.Services.Gemini;
using Microsoft.Extensions.Options;

namespace EmployeePayroll.Api.Services.Material;

public sealed class MaterialInvoiceExtractionService(
    GeminiService gemini,
    InvoiceMediaPreparer mediaPreparer,
    InvoiceDocumentExtractor regexExtractor,
    IOptions<GeminiOptions> options,
    ILogger<MaterialInvoiceExtractionService> logger)
{
    internal const string MaterialExtractionPrompt = """
        You are an invoice OCR system for construction material tracking.
        Extract invoice header fields AND every material/product line from the document.

        Return valid JSON only. No markdown.

        JSON schema (exact keys):
        {
          "invoiceNumber": "",
          "invoiceDate": "",
          "vendorName": "",
          "projectName": "",
          "cgst": null,
          "sgst": null,
          "igst": null,
          "basicTotal": null,
          "transportCharges": null,
          "totalAmount": null,
          "materials": [
            { "materialName": "Ball valve", "quantity": 10 }
          ]
        }

        Rules:
        - invoiceNumber: exact number from invoice (e.g. "7", "No.7", "3542/TS/24-25").
        - invoiceDate as ISO yyyy-MM-dd when possible.
        - projectName: exact buyer/site/project/ship-to/consignee name from the invoice.
        - For projectName, prefer labels like "Project", "Site", "Ship To", "Bill To", "Consignee", "Buyer", "Customer", or "Work Order".
        - Do not put the vendor/supplier name in projectName. Preserve the exact invoice wording as much as possible.
        - materials: ONLY line items that have both a material/product description and an explicit quantity.
        - If a material line has no quantity, do not include it in materials.
        - materialName must be the product description only, quantity as a positive number.
        """;

    public async Task<MaterialExtractionResult> ExtractAsync(Stream stream, string extension, CancellationToken ct)
    {
        extension = extension.ToLowerInvariant();
        stream.Position = 0;

        var fullText = ReadDocumentText(stream, extension, regexExtractor);
        stream.Position = 0;

        MaterialExtractionResult result;
        if (!gemini.IsConfigured)
        {
            var basic = regexExtractor.Extract(stream, extension);
            fullText = string.IsNullOrWhiteSpace(basic.FullText) ? fullText : basic.FullText;
            result = new MaterialExtractionResult
            {
                FullText = fullText,
                InvoiceNumber = basic.InvoiceNumber,
                InvoiceDate = basic.InvoiceDate,
                VendorName = basic.VendorName,
                Status = "Partial",
                Notes = "Gemini API key not configured — parsed header from text; materials from line parser."
            };
        }
        else
        {
            try
            {
                stream.Position = 0;
                var parts = mediaPreparer.Prepare(stream, extension);
                if (parts.Count == 0)
                    return FromTextOnly(fullText, "Could not read document.");

                var jsonText = await gemini.GenerateJsonAsync(parts, MaterialExtractionPrompt, ct);
                if (string.IsNullOrWhiteSpace(jsonText))
                    return FromTextOnly(fullText, "Gemini could not extract invoice data — used text parser.");

                var parsed = ParseJson(jsonText);
                if (parsed is null)
                    return FromTextOnly(fullText, "Invalid Gemini JSON — used text parser.");

                var model = options.Value.Model;
                result = new MaterialExtractionResult
                {
                    FullText = string.IsNullOrWhiteSpace(parsed.FullText) ? fullText : parsed.FullText,
                    InvoiceNumber = parsed.InvoiceNumber,
                    InvoiceDate = parsed.InvoiceDate,
                    VendorName = parsed.VendorName,
                    ProjectName = parsed.ProjectName,
                    SgstAmount = parsed.SgstAmount,
                    CgstAmount = parsed.CgstAmount,
                    IgstAmount = parsed.IgstAmount,
                    TransportCharges = parsed.TransportCharges,
                    BasicTotal = parsed.BasicTotal,
                    TotalAmount = parsed.TotalAmount,
                    Materials = parsed.Materials,
                    Status = "Partial",
                    Notes = $"Extracted via Google Gemini ({model})."
                };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Material invoice extraction failed.");
                return FromTextOnly(fullText, $"Gemini error: {ex.Message}");
            }
        }

        return MergeMaterialsFromText(result, fullText);
    }

    private static string ReadDocumentText(Stream stream, string extension, InvoiceDocumentExtractor extractor)
    {
        stream.Position = 0;
        return extension switch
        {
            ".pdf" or ".docx" => extractor.Extract(stream, extension).FullText,
            _ => string.Empty
        };
    }

    private static MaterialExtractionResult FromTextOnly(string fullText, string note)
    {
        var basic = MaterialLineTextParser.Parse(fullText);
        return new MaterialExtractionResult
        {
            FullText = fullText,
            Materials = basic,
            Status = basic.Count > 0 ? "Partial" : "Failed",
            Notes = note
        };
    }

    private static MaterialExtractionResult MergeMaterialsFromText(MaterialExtractionResult result, string fullText)
    {
        if (result.Materials.Count > 0)
        {
            return new MaterialExtractionResult
            {
                FullText = string.IsNullOrWhiteSpace(result.FullText) ? fullText : result.FullText,
                InvoiceNumber = result.InvoiceNumber,
                InvoiceDate = result.InvoiceDate,
                VendorName = result.VendorName,
                ProjectName = result.ProjectName,
                Materials = result.Materials,
                SgstAmount = result.SgstAmount,
                CgstAmount = result.CgstAmount,
                IgstAmount = result.IgstAmount,
                TransportCharges = result.TransportCharges,
                BasicTotal = result.BasicTotal,
                TotalAmount = result.TotalAmount,
                Status = "Success",
                Notes = result.Notes
            };
        }

        var fromText = MaterialLineTextParser.Parse(string.IsNullOrWhiteSpace(result.FullText) ? fullText : result.FullText);
        return new MaterialExtractionResult
        {
            FullText = string.IsNullOrWhiteSpace(result.FullText) ? fullText : result.FullText,
            InvoiceNumber = result.InvoiceNumber,
            InvoiceDate = result.InvoiceDate,
            VendorName = result.VendorName,
            ProjectName = result.ProjectName,
            SgstAmount = result.SgstAmount,
            CgstAmount = result.CgstAmount,
            IgstAmount = result.IgstAmount,
            TransportCharges = result.TransportCharges,
            BasicTotal = result.BasicTotal,
            TotalAmount = result.TotalAmount,
            Materials = fromText,
            Status = fromText.Count > 0 ? "Success" : result.Status,
            Notes = fromText.Count > 0
                ? (result.Notes ?? "") + " Materials parsed from document text."
                : result.Notes
        };
    }

    private static MaterialExtractionResult? ParseJson(string raw)
    {
        raw = StripMarkdownFence(raw);
        try
        {
            var dto = JsonSerializer.Deserialize<GeminiMaterialInvoiceJson>(raw, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (dto is null) return null;

            var materials = (dto.Materials ?? [])
                .Where(m => !string.IsNullOrWhiteSpace(m.MaterialName) && m.Quantity is > 0)
                .Select(m => new MaterialLineItem
                {
                    MaterialName = m.MaterialName!.Trim(),
                    Quantity = m.Quantity!.Value
                })
                .ToList();

            return new MaterialExtractionResult
            {
                InvoiceNumber = NullIfEmpty(dto.InvoiceNumber),
                InvoiceDate = ParseFlexibleDate(dto.InvoiceDate),
                VendorName = NullIfEmpty(dto.VendorName),
                ProjectName = NullIfEmpty(dto.ProjectName),
                SgstAmount = dto.Sgst,
                CgstAmount = dto.Cgst,
                IgstAmount = dto.Igst,
                BasicTotal = dto.BasicTotal,
                TransportCharges = dto.TransportCharges,
                TotalAmount = dto.TotalAmount,
                Materials = materials
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
        return DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var loose) ? loose : null;
    }
}
