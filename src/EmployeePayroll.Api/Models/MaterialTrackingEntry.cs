namespace EmployeePayroll.Api.Models;

public class MaterialTrackingEntry
{
    public int MaterialTrackingId { get; set; }
    public byte Month { get; set; }
    public int Year { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "nos";
    public decimal UnitRate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? SupplierName { get; set; }
    public DateOnly? ReceivedDate { get; set; }
    public string? Remarks { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}
