namespace EmployeePayroll.Api.Models;

public class TrackingVendor
{
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
