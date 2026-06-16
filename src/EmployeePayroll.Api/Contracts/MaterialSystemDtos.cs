namespace EmployeePayroll.Api.Contracts;

public record TrackingNameResponse(int Id, string Name);

public record MaterialGridRowDto(string MaterialName, IReadOnlyList<decimal?> Quantities, decimal RowTotal);

public record MaterialGridResponse(
    string Mode,
    string EntityName,
    byte Month,
    int Year,
    IReadOnlyList<string> InvoiceColumns,
    IReadOnlyList<MaterialGridRowDto> Rows,
    decimal GrandTotal,
    string? Hint = null);

public record MaterialInvoiceUploadResponse(
    int InvoiceId,
    string? InvoiceNumber,
    string? VendorName,
    string? ProjectName,
    int MaterialsCount,
    MaterialGridResponse Grid);

public record CreateTrackingNameRequest(string Name);
