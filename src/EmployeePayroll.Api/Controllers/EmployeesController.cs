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
public class EmployeesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmployeeResponse>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] string? qualification,
        CancellationToken ct)
    {
        var q = db.Employees.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(e => e.EmployeeName.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var r = role.Trim();
            q = q.Where(e => e.Role != null && e.Role.Contains(r));
        }

        if (!string.IsNullOrWhiteSpace(qualification))
        {
            var qual = qualification.Trim();
            q = q.Where(e => e.Qualification != null && e.Qualification.Contains(qual));
        }

        var list = await q
            .OrderBy(e => e.EmployeeName)
            .Select(e => new EmployeeResponse(e.EmployeeId, e.EmployeeName, e.Qualification, e.Role, e.DailyWage))
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeResponse>> GetById(int id, CancellationToken ct)
    {
        var e = await db.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.EmployeeId == id, ct);
        if (e is null) return NotFound();
        return Ok(new EmployeeResponse(e.EmployeeId, e.EmployeeName, e.Qualification, e.Role, e.DailyWage));
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeResponse>> Create([FromBody] CreateEmployeeRequest body, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (body.DailyWage <= 0) return BadRequest(new { message = "Daily wage must be positive." });
        var entity = new Employee
        {
            EmployeeName = body.EmployeeName.Trim(),
            Qualification = string.IsNullOrWhiteSpace(body.Qualification) ? null : body.Qualification.Trim(),
            Role = string.IsNullOrWhiteSpace(body.Role) ? null : body.Role.Trim(),
            DailyWage = body.DailyWage
        };
        db.Employees.Add(entity);
        await db.SaveChangesAsync(ct);
        await PeriodEnrollment.EnsureNewEmployeeInAllExistingPeriodsAsync(db, entity.EmployeeId, ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.EmployeeId },
            new EmployeeResponse(entity.EmployeeId, entity.EmployeeName, entity.Qualification, entity.Role, entity.DailyWage));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EmployeeResponse>> Update(int id, [FromBody] UpdateEmployeeRequest body, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (body.DailyWage <= 0) return BadRequest(new { message = "Daily wage must be positive." });
        var entity = await db.Employees.Include(e => e.Payrolls).FirstOrDefaultAsync(x => x.EmployeeId == id, ct);
        if (entity is null) return NotFound();
        entity.EmployeeName = body.EmployeeName.Trim();
        entity.Qualification = string.IsNullOrWhiteSpace(body.Qualification) ? null : body.Qualification.Trim();
        entity.Role = string.IsNullOrWhiteSpace(body.Role) ? null : body.Role.Trim();
        entity.DailyWage = body.DailyWage;

        foreach (var p in entity.Payrolls)
        {
            PayrollCalculator.Apply(entity.DailyWage, p.DaysPresent, p.OtHours, p.AdvanceAmount,
                out _, out var ot, out var total);
            p.OtAmount = ot;
            p.TotalAmount = total;
        }

        await db.SaveChangesAsync(ct);
        return Ok(new EmployeeResponse(entity.EmployeeId, entity.EmployeeName, entity.Qualification, entity.Role, entity.DailyWage));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await db.Employees.FirstOrDefaultAsync(x => x.EmployeeId == id, ct);
        if (entity is null) return NotFound();
        db.Employees.Remove(entity);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
