namespace EmployeePayroll.Api.Models;

public class MonthlyTrackingEntry
{
    public int MonthlyTrackingId { get; set; }
    public byte Month { get; set; }
    public int Year { get; set; }
    public string ProjectSiteName { get; set; } = string.Empty;
    public string? WorkDescription { get; set; }
    public string Status { get; set; } = "In Progress";
    public string? Remarks { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}
