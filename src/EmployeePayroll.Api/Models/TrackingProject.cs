namespace EmployeePayroll.Api.Models;

public class TrackingProject
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
