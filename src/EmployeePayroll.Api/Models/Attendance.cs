using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeePayroll.Api.Models;

/// <summary>
/// Per-employee monthly attendance: P/A per calendar day (JSON) and OT hours per day (JSON). Syncs to Payroll.
/// </summary>
public class Attendance
{
    public int AttendanceId { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public byte Month { get; set; }
    public int Year { get; set; }

    /// <summary>JSON object: keys "1".."31", values "P" or "A".</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string PresentByDayJson { get; set; } = "{}";

    /// <summary>JSON object: keys "1".."31", values OT hours (numbers only).</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string OtByDayJson { get; set; } = "{}";
}
