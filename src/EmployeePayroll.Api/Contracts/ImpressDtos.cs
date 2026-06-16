using System.ComponentModel.DataAnnotations;

namespace EmployeePayroll.Api.Contracts;

public record ImpressRowResponse(
    int EmployeeId,
    string EmployeeName,
    int PayrollId,
    decimal Week1,
    decimal Week2,
    decimal Week3,
    decimal Week4,
    decimal Total);

public class SaveImpressRequest
{
    [Range(1, 12)]
    public byte Month { get; set; }

    [Range(2000, 9999)]
    public int Year { get; set; }

    [MinLength(1)]
    public List<ImpressWeeklyRow> Rows { get; set; } = [];
}

public class ImpressWeeklyRow
{
    public int EmployeeId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Week1 { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Week2 { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Week3 { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Week4 { get; set; }
}
