using UglyToad.PdfPig;

namespace EmployeePayroll.Api.Services;

/// <summary>
/// Prepares invoice files for Gemini vision. PDFs are sent natively when possible;
/// scanned PDFs with no text layer are flagged for image-based extraction via Gemini.
/// </summary>
public sealed class PdfConversionService
{
    public bool PdfHasExtractableText(Stream pdfStream)
    {
        pdfStream.Position = 0;
        var bytes = ReadAllBytes(pdfStream);
        try
        {
            using var document = PdfDocument.Open(bytes);
            foreach (var page in document.GetPages())
            {
                if (!string.IsNullOrWhiteSpace(page.Text))
                    return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public byte[] ReadAllBytes(Stream stream)
    {
        stream.Position = 0;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
