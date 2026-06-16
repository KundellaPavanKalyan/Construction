using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeePayroll.Api.Models;

public class Payroll
{
    public int PayrollId { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public byte Month { get; set; }
    public int Year { get; set; }

    public int DaysPresent { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OtHours { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OtAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AdvanceAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
}
