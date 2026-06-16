using EmployeePayroll.Api;
using EmployeePayroll.Api.Contracts;
using EmployeePayroll.Api.Data;
using EmployeePayroll.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeePayroll.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/monthly-tracking")]
public class MonthlyTrackingController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MonthlyTrackingResponse>>> List(
        [FromQuery] byte month,
        [FromQuery] int year,
        CancellationToken ct)
    {
        if (!YearPolicy.IsValidPayrollYear(year))
            return BadRequest(new { message = "Invalid year." });

        var rows = await db.MonthlyTrackingEntries.AsNoTracking()
            .Where(x => x.Month == month && x.Year == year)
            .OrderByDescending(x => x.RecordedAtUtc)
            .Select(x => ToResponse(x))
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<MonthlyTrackingResponse>> Create(
        [FromBody] SaveMonthlyTrackingRequest body,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (!YearPolicy.IsValidPayrollYear(body.Year))
            return BadRequest(new { message = "Invalid year." });

        var entity = new MonthlyTrackingEntry
        {
            Month = body.Month,
            Year = body.Year,
            ProjectSiteName = body.ProjectSiteName.Trim(),
            WorkDescription = string.IsNullOrWhiteSpace(body.WorkDescription) ? null : body.WorkDescription.Trim(),
            Status = string.IsNullOrWhiteSpace(body.Status) ? "In Progress" : body.Status.Trim(),
            Remarks = string.IsNullOrWhiteSpace(body.Remarks) ? null : body.Remarks.Trim(),
            RecordedAtUtc = DateTime.UtcNow
        };
        db.MonthlyTrackingEntries.Add(entity);
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MonthlyTrackingResponse>> Update(
        int id,
        [FromBody] SaveMonthlyTrackingRequest body,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var entity = await db.MonthlyTrackingEntries.FirstOrDefaultAsync(x => x.MonthlyTrackingId == id, ct);
        if (entity is null) return NotFound();

        entity.Month = body.Month;
        entity.Year = body.Year;
        entity.ProjectSiteName = body.ProjectSiteName.Trim();
        entity.WorkDescription = string.IsNullOrWhiteSpace(body.WorkDescription) ? null : body.WorkDescription.Trim();
        entity.Status = string.IsNullOrWhiteSpace(body.Status) ? "In Progress" : body.Status.Trim();
        entity.Remarks = string.IsNullOrWhiteSpace(body.Remarks) ? null : body.Remarks.Trim();
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(entity));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await db.MonthlyTrackingEntries.FirstOrDefaultAsync(x => x.MonthlyTrackingId == id, ct);
        if (entity is null) return NotFound();
        db.MonthlyTrackingEntries.Remove(entity);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static MonthlyTrackingResponse ToResponse(MonthlyTrackingEntry x) =>
        new(x.MonthlyTrackingId, x.Month, x.Year, x.ProjectSiteName, x.WorkDescription, x.Status, x.Remarks, x.RecordedAtUtc);
}
