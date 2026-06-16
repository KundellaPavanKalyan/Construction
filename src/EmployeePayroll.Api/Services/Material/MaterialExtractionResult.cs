namespace EmployeePayroll.Api.Services.Material;

public sealed class MaterialLineItem
{
    public string MaterialName { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
}

public sealed class MaterialExtractionResult
{
    public string FullText { get; init; } = string.Empty;
    public string? InvoiceNumber { get; init; }
    public DateOnly? InvoiceDate { get; init; }
    public string? VendorName { get; init; }
    public string? ProjectName { get; init; }
    public IReadOnlyList<MaterialLineItem> Materials { get; init; } = Array.Empty<MaterialLineItem>();
    public decimal? SgstAmount { get; init; }
    public decimal? CgstAmount { get; init; }
    public decimal? IgstAmount { get; init; }
    public decimal? TransportCharges { get; init; }
    public decimal? BasicTotal { get; init; }
    public decimal? TotalAmount { get; init; }
    public string Status { get; init; } = "Failed";
    public string? Notes { get; init; }
}
