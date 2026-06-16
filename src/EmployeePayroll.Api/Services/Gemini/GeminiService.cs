using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace EmployeePayroll.Api.Services.Gemini;

public sealed class GeminiService(IHttpClientFactory httpClientFactory, IOptions<GeminiOptions> options, ILogger<GeminiService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal const string ExtractionPrompt = """
        You are an invoice OCR and data extraction system for Indian GST invoices.
        Extract the following fields from the attached invoice document:
        - Invoice Number
        - Invoice Date
        - Vendor Name
        - CGST
        - SGST
        - IGST
        - Basic Total
        - Transport Charges
        - Total Amount

        Rules:
        - Return valid JSON only. No markdown, no explanation.
        - Use null for missing numeric fields.
        - invoiceDate as ISO yyyy-MM-dd when possible, otherwise the date as printed on the invoice.
        - Amounts as numbers without currency symbols or commas.

        JSON schema (exact keys):
        {
          "invoiceNumber": "",
          "invoiceDate": "",
          "vendorName": "",
          "cgst": null,
          "sgst": null,
          "igst": null,
          "basicTotal": null,
          "transportCharges": null,
          "totalAmount": null
        }
        """;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.ApiKey);

    public Task<string?> GenerateJsonAsync(IReadOnlyList<GeminiContentPart> documentParts, CancellationToken ct) =>
        GenerateJsonAsync(documentParts, ExtractionPrompt, ct);

    public async Task<string?> GenerateJsonAsync(
        IReadOnlyList<GeminiContentPart> documentParts,
        string prompt,
        CancellationToken ct)
    {
        var apiKey = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        var model = string.IsNullOrWhiteSpace(options.Value.Model) ? "gemini-2.5-flash" : options.Value.Model.Trim();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}";

        var parts = new List<object> { new { text = prompt } };
        foreach (var part in documentParts)
        {
            if (part.IsInlineData)
            {
                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = part.MimeType,
                        data = Convert.ToBase64String(part.BinaryData!)
                    }
                });
            }
            else if (!string.IsNullOrWhiteSpace(part.Text))
            {
                parts.Add(new { text = part.Text });
            }
        }

        var body = new
        {
            contents = new[] { new { parts } },
            generationConfig = new
            {
                temperature = 0.1,
                responseMimeType = "application/json"
            }
        };

        var client = httpClientFactory.CreateClient(nameof(GeminiService));
        using var response = await client.PostAsJsonAsync(url, body, JsonOptions, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Gemini API returned {StatusCode}: {Body}", (int)response.StatusCode, Truncate(raw, 500));
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
            return text;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not parse Gemini response envelope.");
            return null;
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";
}
