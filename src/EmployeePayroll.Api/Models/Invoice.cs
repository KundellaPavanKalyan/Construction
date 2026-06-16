namespace EmployeePayroll.Api.Models;

public class Invoice
{
    public int InvoiceId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; }

    public string? ExtractedText { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateOnly? InvoiceDate { get; set; }
    public string? VendorName { get; set; }
    public string? ProjectName { get; set; }

    public ICollection<MaterialInvoiceItem> MaterialItems { get; set; } = new List<MaterialInvoiceItem>();
    public decimal? SgstAmount { get; set; }
    public decimal? CgstAmount { get; set; }
    public decimal? IgstAmount { get; set; }
    public decimal? TransportCharges { get; set; }
    public decimal? BasicTotal { get; set; }
    public decimal? TotalAmount { get; set; }

    /// <summary>Success, Partial, or Failed.</summary>
    public string ExtractionStatus { get; set; } = "Failed";
    public string? ExtractionNotes { get; set; }
}
