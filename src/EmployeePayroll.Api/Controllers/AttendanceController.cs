using EmployeePayroll.Api;
using EmployeePayroll.Api.Contracts;
using EmployeePayroll.Api.Data;
using EmployeePayroll.Api.Models;
using EmployeePayroll.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeePayroll.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AttendanceController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AttendanceEmployeeRow>>> GetGrid(
        [FromQuery] byte month,
        [FromQuery] int year,
        CancellationToken ct)
    {
        if (!YearPolicy.IsValidPayrollYear(year))
            return BadRequest(new
            {
                message = $"Year must be between {YearPolicy.MinPayrollYear} and {YearPolicy.MaxPayrollYear} (no future years)."
            });

        var hasMonth = await db.Payrolls.AsNoTracking().AnyAsync(p => p.Month == month && p.Year == year, ct);
        if (!hasMonth)
            return Ok(Array.Empty<AttendanceEmployeeRow>());

        var employeeIds = await db.Employees.AsNoTracking().Select(e => e.EmployeeId).ToListAsync(ct);
        var any = false;
        foreach (var id in employeeIds)
            any |= await PeriodEnrollment.EnsurePayrollAndAttendanceAsync(db, id, month, year, ct, false);
        if (any)
            await db.SaveChangesAsync(ct);

        var rows = await (
            from e in db.Employees.AsNoTracking()
            join p in db.Payrolls.AsNoTracking() on e.EmployeeId equals p.EmployeeId
            where p.Month == month && p.Year == year
            join a in db.Attendances.AsNoTracking() on new { e.EmployeeId, Month = p.Month, Year = p.Year } equals new
                { a.EmployeeId, a.Month, a.Year }
            orderby e.EmployeeName
            select new AttendanceEmployeeRow(
                e.EmployeeId,
                e.EmployeeName,
                e.Role,
                e.DailyWage,
                p.PayrollId,
                p.AdvanceAmount,
                a.PresentByDayJson,
                a.OtByDayJson)).ToListAsync(ct);

        return Ok(rows);
    }

    [HttpPost("save")]
    public async Task<ActionResult> Save([FromBody] SaveAttendanceRequest body, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (!YearPolicy.IsValidPayrollYear(body.Year))
            return BadRequest(new
            {
                message = $"Year must be between {YearPolicy.MinPayrollYear} and {YearPolicy.MaxPayrollYear} (no future years)."
            });
        if (!await db.Payrolls.AnyAsync(p => p.Month == body.Month && p.Year == body.Year, ct))
            return BadRequest(new { message = "Create this payroll month first from the dashboard." });

        foreach (var row in body.Rows.Select(r => r.EmployeeId).Distinct())
            await PeriodEnrollment.EnsurePayrollAndAttendanceAsync(db, row, body.Month, body.Year, ct, false);
        await db.SaveChangesAsync(ct);

        foreach (var row in body.Rows)
        {
            var att = await db.Attendances.FirstAsync(
                a => a.EmployeeId == row.EmployeeId && a.Month == body.Month && a.Year == body.Year, ct);
            att.PresentByDayJson = AttendancePayrollSync.NormalizePresentJson(row.PresentByDayJson);
            att.OtByDayJson = AttendancePayrollSync.NormalizeOtJson(row.OtByDayJson);

            var payroll = await db.Payrolls.FirstAsync(
                p => p.EmployeeId == row.EmployeeId && p.Month == body.Month && p.Year == body.Year, ct);
            var emp = await db.Employees.FirstAsync(e => e.EmployeeId == row.EmployeeId, ct);
            AttendancePayrollSync.ApplyToPayroll(emp, payroll, att);
        }

        await db.SaveChangesAsync(ct);
        return Ok(new { saved = body.Rows.Count });
    }
}
