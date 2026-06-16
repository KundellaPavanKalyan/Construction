namespace EmployeePayroll.Api.Services;

public sealed class InvoiceExtractionResult
{
    public string FullText { get; init; } = string.Empty;
    public string? InvoiceNumber { get; init; }
    public DateOnly? InvoiceDate { get; init; }
    public string? VendorName { get; init; }
    public decimal? SgstAmount { get; init; }
    public decimal? CgstAmount { get; init; }
    public decimal? IgstAmount { get; init; }
    public decimal? TransportCharges { get; init; }
    public decimal? BasicTotal { get; init; }
    public decimal? TotalAmount { get; init; }
    public string Status { get; init; } = "Failed";
    public string? Notes { get; init; }
}
