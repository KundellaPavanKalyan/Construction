using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeePayroll.Api.Models;

public class Employee
{
    public int EmployeeId { get; set; }

    [Required]
    [MaxLength(200)]
    public string EmployeeName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Qualification { get; set; }

    [MaxLength(150)]
    public string? Role { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DailyWage { get; set; }

    public ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
