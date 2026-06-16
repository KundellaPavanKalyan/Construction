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
public class ImpressController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ImpressRowResponse>>> GetRows(
        [FromQuery] byte month,
        [FromQuery] int year,
        CancellationToken ct)
    {
        if (!YearPolicy.IsValidPayrollYear(year))
            return BadRequest(new
            {
                message = $"Year must be between {YearPolicy.MinPayrollYear} and {YearPolicy.MaxPayrollYear}."
            });

        if (!await db.Payrolls.AsNoTracking().AnyAsync(p => p.Month == month && p.Year == year, ct))
            return Ok(Array.Empty<ImpressRowResponse>());

        var employeeIds = await db.Employees.AsNoTracking().Select(e => e.EmployeeId).ToListAsync(ct);
        var any = false;
        foreach (var id in employeeIds)
            any |= await PeriodEnrollment.EnsurePayrollAndAttendanceAsync(db, id, month, year, ct, false);
        if (any)
            await db.SaveChangesAsync(ct);

        var raw = await (
            from e in db.Employees.AsNoTracking()
            join p in db.Payrolls.AsNoTracking() on e.EmployeeId equals p.EmployeeId
            where p.Month == month && p.Year == year
            join a in db.Attendances.AsNoTracking() on new { e.EmployeeId, Month = p.Month, Year = p.Year } equals new
                { a.EmployeeId, a.Month, a.Year }
            join w in db.ImpressWeeklyAmounts.AsNoTracking() on p.PayrollId equals w.PayrollId into wj
            from w in wj.DefaultIfEmpty()
            orderby e.EmployeeName
            select new
            {
                e.EmployeeId,
                e.EmployeeName,
                p.PayrollId,
                p.AdvanceAmount,
                Weekly = w
            }).ToListAsync(ct);

        var rows = raw.Select(r =>
        {
            decimal week1, week2, week3, week4;
            if (r.Weekly is not null)
            {
                week1 = r.Weekly.Week1;
                week2 = r.Weekly.Week2;
                week3 = r.Weekly.Week3;
                week4 = r.Weekly.Week4;
            }
            else
            {
                week1 = r.AdvanceAmount;
                week2 = week3 = week4 = 0;
            }

            return new ImpressRowResponse(
                r.EmployeeId,
                r.EmployeeName,
                r.PayrollId,
                week1,
                week2,
                week3,
                week4,
                week1 + week2 + week3 + week4);
        }).ToList();

        return Ok(rows);
    }

    [HttpPost("save")]
    public async Task<ActionResult> Save([FromBody] SaveImpressRequest body, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (!YearPolicy.IsValidPayrollYear(body.Year))
            return BadRequest(new
            {
                message = $"Year must be between {YearPolicy.MinPayrollYear} and {YearPolicy.MaxPayrollYear}."
            });

        if (!await db.Payrolls.AnyAsync(p => p.Month == body.Month && p.Year == body.Year, ct))
            return BadRequest(new { message = "Create this payroll month first from the dashboard." });

        foreach (var row in body.Rows)
        {
            var payroll = await db.Payrolls
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(
                    p => p.EmployeeId == row.EmployeeId && p.Month == body.Month && p.Year == body.Year, ct);
            if (payroll is null) continue;

            var weekly = await db.ImpressWeeklyAmounts
                .FirstOrDefaultAsync(w => w.PayrollId == payroll.PayrollId, ct);
            if (weekly is null)
            {
                weekly = new ImpressWeeklyAmount { PayrollId = payroll.PayrollId };
                db.ImpressWeeklyAmounts.Add(weekly);
            }

            weekly.Week1 = row.Week1;
            weekly.Week2 = row.Week2;
            weekly.Week3 = row.Week3;
            weekly.Week4 = row.Week4;

            var total = row.Week1 + row.Week2 + row.Week3 + row.Week4;
            payroll.AdvanceAmount = total;
            PayrollCalculator.Apply(
                payroll.Employee.DailyWage,
                payroll.DaysPresent,
                payroll.OtHours,
                payroll.AdvanceAmount,
                out _,
                out var otAmt,
                out var totalPay);
            payroll.OtAmount = otAmt;
            payroll.TotalAmount = totalPay;
        }

        await db.SaveChangesAsync(ct);
        return Ok(new { saved = body.Rows.Count, message = "Weekly amounts saved. Total applied to Advance in payroll." });
    }
}
