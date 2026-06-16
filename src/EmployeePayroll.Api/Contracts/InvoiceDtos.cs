namespace EmployeePayroll.Api.Contracts;

public record InvoiceListItemResponse(
    int InvoiceId,
    string OriginalFileName,
    string? InvoiceNumber,
    string? InvoiceDate,
    string? VendorName,
    decimal? SgstAmount,
    decimal? CgstAmount,
    decimal? IgstAmount,
    decimal? TransportCharges,
    decimal? BasicTotal,
    decimal? TotalAmount,
    string? ProjectName,
    string ExtractionStatus);

public record InvoiceDetailResponse(
    int InvoiceId,
    string OriginalFileName,
    string? InvoiceNumber,
    string? InvoiceDate,
    string? VendorName,
    string? ProjectName,
    decimal? SgstAmount,
    decimal? CgstAmount,
    decimal? IgstAmount,
    decimal? TransportCharges,
    decimal? BasicTotal,
    decimal? TotalAmount,
    string ExtractionStatus,
    string? ExtractionNotes,
    string? ExtractedText);

public record InvoiceUploadResponse(InvoiceDetailResponse Invoice);

public record UpdateInvoiceRequest(
    string? InvoiceNumber,
    string? InvoiceDate,
    string? VendorName,
    string? ProjectName,
    decimal? SgstAmount,
    decimal? CgstAmount,
    decimal? IgstAmount,
    decimal? TransportCharges,
    decimal? BasicTotal,
    decimal? TotalAmount);
