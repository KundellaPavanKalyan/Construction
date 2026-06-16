using System.ComponentModel.DataAnnotations;

namespace EmployeePayroll.Api.Contracts;

public record AttendanceEmployeeRow(
    int EmployeeId,
    string EmployeeName,
    string? Role,
    decimal DailyWage,
    int PayrollId,
    decimal AdvanceAmount,
    string PresentByDayJson,
    string OtByDayJson);

public class SaveAttendanceRequest
{
    [Range(1, 12)]
    public byte Month { get; set; }

    [Range(2000, 9999)]
    public int Year { get; set; }

    [Required]
    public List<AttendanceRowInput> Rows { get; set; } = new();
}

public class AttendanceRowInput
{
    public int EmployeeId { get; set; }

    public string PresentByDayJson { get; set; } = "{}";

    public string OtByDayJson { get; set; } = "{}";
}
