using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeePayroll.Api.Models;

public class ImpressWeeklyAmount
{
    public int ImpressWeeklyAmountId { get; set; }

    public int PayrollId { get; set; }
    public Payroll Payroll { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Week1 { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Week2 { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Week3 { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Week4 { get; set; }
}
