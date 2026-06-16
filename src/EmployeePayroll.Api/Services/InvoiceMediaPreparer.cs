using DocumentFormat.OpenXml.Packaging;
using EmployeePayroll.Api.Services.Gemini;

namespace EmployeePayroll.Api.Services;

public sealed class InvoiceMediaPreparer(PdfConversionService pdfConversion)
{
    public IReadOnlyList<GeminiContentPart> Prepare(Stream stream, string extension)
    {
        extension = extension.ToLowerInvariant();
        stream.Position = 0;

        return extension switch
        {
            ".pdf" => PreparePdf(stream),
            ".jpg" or ".jpeg" => [new GeminiContentPart { MimeType = "image/jpeg", BinaryData = pdfConversion.ReadAllBytes(stream) }],
            ".png" => [new GeminiContentPart { MimeType = "image/png", BinaryData = pdfConversion.ReadAllBytes(stream) }],
            ".docx" => PrepareDocx(stream),
            _ => []
        };
    }

    private static IReadOnlyList<GeminiContentPart> PrepareDocx(Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, false);
        var text = doc.MainDocumentPart?.Document?.Body?.InnerText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return
        [
            new GeminiContentPart
            {
                Text = "Invoice document text (DOCX):\n\n" + text
            }
        ];
    }

    private IReadOnlyList<GeminiContentPart> PreparePdf(Stream stream)
    {
        var bytes = pdfConversion.ReadAllBytes(stream);
        return [new GeminiContentPart { MimeType = "application/pdf", BinaryData = bytes }];
    }
}
