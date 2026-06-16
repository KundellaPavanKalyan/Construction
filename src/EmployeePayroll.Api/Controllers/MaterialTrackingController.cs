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
[Route("api/material-tracking")]
public class MaterialTrackingController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MaterialTrackingResponse>>> List(
        [FromQuery] byte month,
        [FromQuery] int year,
        CancellationToken ct)
    {
        if (!YearPolicy.IsValidPayrollYear(year))
            return BadRequest(new { message = "Invalid year." });

        var rows = await db.MaterialTrackingEntries.AsNoTracking()
            .Where(x => x.Month == month && x.Year == year)
            .OrderByDescending(x => x.RecordedAtUtc)
            .Select(x => ToResponse(x))
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<MaterialTrackingResponse>> Create(
        [FromBody] SaveMaterialTrackingRequest body,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (!YearPolicy.IsValidPayrollYear(body.Year))
            return BadRequest(new { message = "Invalid year." });

        var total = Math.Round(body.Quantity * body.UnitRate, 2, MidpointRounding.AwayFromZero);
        var entity = new MaterialTrackingEntry
        {
            Month = body.Month,
            Year = body.Year,
            MaterialName = body.MaterialName.Trim(),
            Quantity = body.Quantity,
            Unit = string.IsNullOrWhiteSpace(body.Unit) ? "nos" : body.Unit.Trim(),
            UnitRate = body.UnitRate,
            TotalAmount = total,
            SupplierName = string.IsNullOrWhiteSpace(body.SupplierName) ? null : body.SupplierName.Trim(),
            ReceivedDate = body.ReceivedDate,
            Remarks = string.IsNullOrWhiteSpace(body.Remarks) ? null : body.Remarks.Trim(),
            RecordedAtUtc = DateTime.UtcNow
        };
        db.MaterialTrackingEntries.Add(entity);
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MaterialTrackingResponse>> Update(
        int id,
        [FromBody] SaveMaterialTrackingRequest body,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var entity = await db.MaterialTrackingEntries.FirstOrDefaultAsync(x => x.MaterialTrackingId == id, ct);
        if (entity is null) return NotFound();

        entity.Month = body.Month;
        entity.Year = body.Year;
        entity.MaterialName = body.MaterialName.Trim();
        entity.Quantity = body.Quantity;
        entity.Unit = string.IsNullOrWhiteSpace(body.Unit) ? "nos" : body.Unit.Trim();
        entity.UnitRate = body.UnitRate;
        entity.TotalAmount = Math.Round(body.Quantity * body.UnitRate, 2, MidpointRounding.AwayFromZero);
        entity.SupplierName = string.IsNullOrWhiteSpace(body.SupplierName) ? null : body.SupplierName.Trim();
        entity.ReceivedDate = body.ReceivedDate;
        entity.Remarks = string.IsNullOrWhiteSpace(body.Remarks) ? null : body.Remarks.Trim();
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(entity));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await db.MaterialTrackingEntries.FirstOrDefaultAsync(x => x.MaterialTrackingId == id, ct);
        if (entity is null) return NotFound();
        db.MaterialTrackingEntries.Remove(entity);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static MaterialTrackingResponse ToResponse(MaterialTrackingEntry x) =>
        new(
            x.MaterialTrackingId,
            x.Month,
            x.Year,
            x.MaterialName,
            x.Quantity,
            x.Unit,
            x.UnitRate,
            x.TotalAmount,
            x.SupplierName,
            x.ReceivedDate?.ToString("yyyy-MM-dd"),
            x.Remarks,
            x.RecordedAtUtc);
}
