using System.ComponentModel.DataAnnotations;

namespace EmployeePayroll.Api.Contracts;

public record MonthlyTrackingResponse(
    int MonthlyTrackingId,
    byte Month,
    int Year,
    string ProjectSiteName,
    string? WorkDescription,
    string Status,
    string? Remarks,
    DateTime RecordedAtUtc);

public class SaveMonthlyTrackingRequest
{
    [Range(1, 12)]
    public byte Month { get; set; }

    [Range(2000, 9999)]
    public int Year { get; set; }

    [Required]
    [MaxLength(200)]
    public string ProjectSiteName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? WorkDescription { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "In Progress";

    [MaxLength(300)]
    public string? Remarks { get; set; }
}

public record MaterialTrackingResponse(
    int MaterialTrackingId,
    byte Month,
    int Year,
    string MaterialName,
    decimal Quantity,
    string Unit,
    decimal UnitRate,
    decimal TotalAmount,
    string? SupplierName,
    string? ReceivedDate,
    string? Remarks,
    DateTime RecordedAtUtc);

public class SaveMaterialTrackingRequest
{
    [Range(1, 12)]
    public byte Month { get; set; }

    [Range(2000, 9999)]
    public int Year { get; set; }

    [Required]
    [MaxLength(200)]
    public string MaterialName { get; set; } = string.Empty;

    [Range(0.0001, double.MaxValue)]
    public decimal Quantity { get; set; }

    [MaxLength(30)]
    public string Unit { get; set; } = "nos";

    [Range(0, double.MaxValue)]
    public decimal UnitRate { get; set; }

    [MaxLength(200)]
    public string? SupplierName { get; set; }

    public DateOnly? ReceivedDate { get; set; }

    [MaxLength(300)]
    public string? Remarks { get; set; }
}
