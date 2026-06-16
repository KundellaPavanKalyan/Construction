using EmployeePayroll.Api.Data;
using EmployeePayroll.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeePayroll.Api.Services;

public static class PeriodEnrollment
{
    public static async Task<bool> EnsurePayrollAndAttendanceAsync(AppDbContext db, int employeeId, byte month, int year,
        CancellationToken ct = default, bool persist = true)
    {
        var emp = await db.Employees.AsNoTracking().FirstAsync(e => e.EmployeeId == employeeId, ct);

        var changed = false;
        var payroll = await db.Payrolls.FirstOrDefaultAsync(p => p.EmployeeId == employeeId && p.Month == month && p.Year == year, ct);
        if (payroll is null)
        {
            payroll = new Payroll
            {
                EmployeeId = employeeId,
                Month = month,
                Year = year,
                DaysPresent = 0,
                OtHours = 0,
                OtAmount = 0,
                AdvanceAmount = 0,
                TotalAmount = 0
            };
            PayrollCalculator.Apply(emp.DailyWage, payroll.DaysPresent, payroll.OtHours, payroll.AdvanceAmount,
                out _, out var ot, out var total);
            payroll.OtAmount = ot;
            payroll.TotalAmount = total;
            db.Payrolls.Add(payroll);
            changed = true;
        }

        var att = await db.Attendances.FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Month == month && a.Year == year, ct);
        if (att is null)
        {
            db.Attendances.Add(new Attendance
            {
                EmployeeId = employeeId,
                Month = month,
                Year = year,
                PresentByDayJson = "{}",
                OtByDayJson = "{}"
            });
            changed = true;
        }

        if (persist && changed)
            await db.SaveChangesAsync(ct);

        return changed;
    }

    public static async Task EnsureNewEmployeeInAllExistingPeriodsAsync(AppDbContext db, int newEmployeeId,
        CancellationToken ct = default)
    {
        var periods = await db.Payrolls.AsNoTracking()
            .Select(p => new { p.Month, p.Year })
            .Distinct()
            .ToListAsync(ct);

        foreach (var pr in periods)
            await EnsurePayrollAndAttendanceAsync(db, newEmployeeId, pr.Month, pr.Year, ct, persist: true);
    }
}
