using System.Globalization;
using ClosedXML.Excel;
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
public class PayrollController(AppDbContext db) : ControllerBase
{
    private static string MonthFileToken(byte month) =>
        CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);

    [HttpGet]
    public async Task<ActionResult<PagedResult<PayrollRowResponse>>> GetPage(
        [FromQuery] byte month,
        [FromQuery] int year,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] string? qualification = null,
        [FromQuery] decimal? minSalary = null,
        [FromQuery] decimal? maxSalary = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDir = "asc",
        CancellationToken ct = default)
    {
        if (!YearPolicy.IsValidPayrollYear(year))
            return BadRequest(new
            {
                message = $"Year must be between {YearPolicy.MinPayrollYear} and {YearPolicy.MaxPayrollYear} (no future years)."
            });

        if (page < 1) page = 1;
        if (pageSize is < 1 or > 200) pageSize = 20;

        var hasMonth = await db.Payrolls.AsNoTracking().AnyAsync(p => p.Month == month && p.Year == year, ct);
        if (!hasMonth)
            return Ok(new PagedResult<PayrollRowResponse>(Array.Empty<PayrollRowResponse>(), 0, page, pageSize));

        await EnsureMissingEmployeesForPeriodAsync(month, year, ct);

        var baseQuery = db.Payrolls
            .AsNoTracking()
            .Include(p => p.Employee)
            .Where(p => p.Month == month && p.Year == year);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            baseQuery = baseQuery.Where(p => p.Employee.EmployeeName.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var r = role.Trim();
            baseQuery = baseQuery.Where(p => p.Employee.Role != null && p.Employee.Role.Contains(r));
        }

        if (!string.IsNullOrWhiteSpace(qualification))
        {
            var q = qualification.Trim();
            baseQuery = baseQuery.Where(p => p.Employee.Qualification != null && p.Employee.Qualification.Contains(q));
        }

        if (minSalary is not null)
            baseQuery = baseQuery.Where(p => p.Employee.DailyWage * p.DaysPresent >= minSalary);
        if (maxSalary is not null)
            baseQuery = baseQuery.Where(p => p.Employee.DailyWage * p.DaysPresent <= maxSalary);

        var descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        baseQuery = (sortBy?.ToLowerInvariant()) switch
        {
            "wage" => descending
                ? baseQuery.OrderByDescending(p => p.Employee.DailyWage)
                : baseQuery.OrderBy(p => p.Employee.DailyWage),
            "days" or "attendance" => descending
                ? baseQuery.OrderByDescending(p => p.DaysPresent)
                : baseQuery.OrderBy(p => p.DaysPresent),
            "total" or "salary" => descending
                ? baseQuery.OrderByDescending(p => p.TotalAmount)
                : baseQuery.OrderBy(p => p.TotalAmount),
            _ => descending
                ? baseQuery.OrderByDescending(p => p.Employee.EmployeeName)
                : baseQuery.OrderBy(p => p.Employee.EmployeeName)
        };

        var total = await baseQuery.CountAsync(ct);
        var rows = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PayrollRowResponse(
                p.PayrollId,
                p.EmployeeId,
                p.Employee.EmployeeName,
                p.Employee.Qualification,
                p.Employee.Role,
                p.Employee.DailyWage,
                p.DaysPresent,
                Math.Round(p.Employee.DailyWage * p.DaysPresent, 2, MidpointRounding.AwayFromZero),
                p.OtHours,
                p.OtAmount,
                p.AdvanceAmount,
                p.TotalAmount))
            .ToListAsync(ct);

        return Ok(new PagedResult<PayrollRowResponse>(rows, total, page, pageSize));
    }

    [HttpPost("months")]
    public async Task<ActionResult> CreateMonth([FromBody] CreatePayrollMonthRequest body, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (!YearPolicy.IsValidPayrollYear(body.Year))
            return BadRequest(new
            {
                message = $"Year must be between {YearPolicy.MinPayrollYear} and {YearPolicy.MaxPayrollYear} (no future years)."
            });
        var exists = await db.Payrolls.AnyAsync(p => p.Month == body.Month && p.Year == body.Year, ct);
        if (exists)
            return Conflict(new { message = "Payroll for this month and year already exists. Previous data is preserved; use another period or edit existing rows." });

        var employees = await db.Employees.AsNoTracking().Select(e => e.EmployeeId).ToListAsync(ct);
        var any = false;
        foreach (var employeeId in employees)
            any |= await PeriodEnrollment.EnsurePayrollAndAttendanceAsync(db, employeeId, body.Month, body.Year, ct, false);
        if (any)
            await db.SaveChangesAsync(ct);

        return Ok(new { created = employees.Count, body.Month, body.Year });
    }

    [HttpPut("{payrollId:int}")]
    public async Task<ActionResult<PayrollRowResponse>> Update(int payrollId, [FromBody] UpdatePayrollRequest body,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var p = await db.Payrolls.Include(x => x.Employee).FirstOrDefaultAsync(x => x.PayrollId == payrollId, ct);
        if (p is null) return NotFound();

        p.DaysPresent = body.DaysPresent;
        p.OtHours = body.OtHours;
        p.AdvanceAmount = body.AdvanceAmount;
        PayrollCalculator.Apply(p.Employee.DailyWage, p.DaysPresent, p.OtHours, p.AdvanceAmount,
            out _, out var ot, out var total);
        p.OtAmount = ot;
        p.TotalAmount = total;
        await db.SaveChangesAsync(ct);

        var monthly = Math.Round(p.Employee.DailyWage * p.DaysPresent, 2, MidpointRounding.AwayFromZero);
        return Ok(new PayrollRowResponse(
            p.PayrollId,
            p.EmployeeId,
            p.Employee.EmployeeName,
            p.Employee.Qualification,
            p.Employee.Role,
            p.Employee.DailyWage,
            p.DaysPresent,
            monthly,
            p.OtHours,
            p.OtAmount,
            p.AdvanceAmount,
            p.TotalAmount));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] byte month,
        [FromQuery] int year,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] string? qualification = null,
        [FromQuery] decimal? minSalary = null,
        [FromQuery] decimal? maxSalary = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDir = "asc",
        CancellationToken ct = default)
    {
        if (!YearPolicy.IsValidPayrollYear(year))
            return BadRequest(new
            {
                message = $"Year must be between {YearPolicy.MinPayrollYear} and {YearPolicy.MaxPayrollYear} (no future years)."
            });

        var hasMonth = await db.Payrolls.AsNoTracking().AnyAsync(p => p.Month == month && p.Year == year, ct);
        if (!hasMonth)
            return NotFound(new { message = "No payroll data for this month." });

        await EnsureMissingEmployeesForPeriodAsync(month, year, ct);

        var baseQuery = db.Payrolls
            .AsNoTracking()
            .Include(p => p.Employee)
            .Where(p => p.Month == month && p.Year == year);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            baseQuery = baseQuery.Where(p => p.Employee.EmployeeName.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var r = role.Trim();
            baseQuery = baseQuery.Where(p => p.Employee.Role != null && p.Employee.Role.Contains(r));
        }

        if (!string.IsNullOrWhiteSpace(qualification))
        {
            var q = qualification.Trim();
            baseQuery = baseQuery.Where(p => p.Employee.Qualification != null && p.Employee.Qualification.Contains(q));
        }

        if (minSalary is not null)
            baseQuery = baseQuery.Where(p => p.Employee.DailyWage * p.DaysPresent >= minSalary);
        if (maxSalary is not null)
            baseQuery = baseQuery.Where(p => p.Employee.DailyWage * p.DaysPresent <= maxSalary);

        var descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        baseQuery = (sortBy?.ToLowerInvariant()) switch
        {
            "wage" => descending
                ? baseQuery.OrderByDescending(p => p.Employee.DailyWage)
                : baseQuery.OrderBy(p => p.Employee.DailyWage),
            "days" or "attendance" => descending
                ? baseQuery.OrderByDescending(p => p.DaysPresent)
                : baseQuery.OrderBy(p => p.DaysPresent),
            "total" or "salary" => descending
                ? baseQuery.OrderByDescending(p => p.TotalAmount)
                : baseQuery.OrderBy(p => p.TotalAmount),
            _ => descending
                ? baseQuery.OrderByDescending(p => p.Employee.EmployeeName)
                : baseQuery.OrderBy(p => p.Employee.EmployeeName)
        };

        var rows = await baseQuery.ToListAsync(ct);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Payroll");
        var headers = new[]
        {
            "Employee Name", "Role", "Wage", "Present Days", "OT Hours", "Salary", "Final Amount"
        };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var rNum = 2;
        foreach (var p in rows)
        {
            var monthly = Math.Round(p.Employee.DailyWage * p.DaysPresent, 2, MidpointRounding.AwayFromZero);
            ws.Cell(rNum, 1).Value = p.Employee.EmployeeName;
            ws.Cell(rNum, 2).Value = p.Employee.Role ?? string.Empty;
            ws.Cell(rNum, 3).Value = p.Employee.DailyWage;
            ws.Cell(rNum, 4).Value = p.DaysPresent;
            ws.Cell(rNum, 5).Value = p.OtHours;
            ws.Cell(rNum, 6).Value = monthly;
            ws.Cell(rNum, 7).Value = p.TotalAmount;
            rNum++;
        }

        ws.RangeUsed()?.SetAutoFilter();
        ws.Columns().AdjustToContents();

        await using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();
        var fileName = $"Payroll_{MonthFileToken(month)}_{year}.xlsx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private async Task EnsureMissingEmployeesForPeriodAsync(byte month, int year, CancellationToken ct)
    {
        var employeeIds = await db.Employees.AsNoTracking().Select(e => e.EmployeeId).ToListAsync(ct);
        var covered = await db.Payrolls.Where(p => p.Month == month && p.Year == year).Select(p => p.EmployeeId)
            .ToHashSetAsync(ct);
        var any = false;
        foreach (var id in employeeIds.Where(id => !covered.Contains(id)))
            any |= await PeriodEnrollment.EnsurePayrollAndAttendanceAsync(db, id, month, year, ct, false);
        if (any)
            await db.SaveChangesAsync(ct);
    }
}
