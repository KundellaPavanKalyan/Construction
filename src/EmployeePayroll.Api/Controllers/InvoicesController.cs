using System.Globalization;
using EmployeePayroll.Api.Contracts;
using EmployeePayroll.Api.Data;
using EmployeePayroll.Api.Models;
using EmployeePayroll.Api.Services;
using EmployeePayroll.Api.Services.Gemini;
using EmployeePayroll.Api.Services.Material;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeePayroll.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InvoicesController(
    AppDbContext db,
    InvoiceFileStorage storage,
    MaterialInvoiceExtractionService extraction,
    MaterialCatalogService materialCatalog,
    InvoiceExcelExportService excelExport,
    IConfiguration configuration) : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".jpg", ".jpeg", ".png"
    };

    private long MaxBytes => configuration.GetValue("InvoiceUpload:MaxFileSizeBytes", 10 * 1024 * 1024);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceListItemResponse>>> List(CancellationToken ct)
    {
        await BackfillMissingProjectNamesAsync(ct);

        var items = await db.Invoices.AsNoTracking()
            .OrderByDescending(i => i.UploadedAtUtc)
            .Select(i => ToListItem(i))
            .ToListAsync(ct);
        return Ok(items);
    }

    private async Task BackfillMissingProjectNamesAsync(CancellationToken ct)
    {
        var rows = await db.Invoices
            .Where(i => i.ProjectName == null || i.ProjectName == "")
            .OrderByDescending(i => i.UploadedAtUtc)
            .Take(10)
            .ToListAsync(ct);

        var changed = false;
        foreach (var row in rows)
        {
            var path = storage.GetFullPath(row.StoredFileName);
            if (path is null) continue;

            var ext = Path.GetExtension(path);
            if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext)) continue;

            try
            {
                await using var stream = System.IO.File.OpenRead(path);
                var extracted = await extraction.ExtractAsync(stream, ext, ct);
                if (string.IsNullOrWhiteSpace(extracted.ProjectName)) continue;

                row.ProjectName = extracted.ProjectName.Trim();
                row.ExtractionNotes = string.IsNullOrWhiteSpace(row.ExtractionNotes)
                    ? "Project name backfilled from invoice."
                    : $"{row.ExtractionNotes} Project name backfilled from invoice.";
                changed = true;
            }
            catch
            {
                // Keep list loading even if an older uploaded file cannot be reprocessed.
            }
        }

        if (!changed) return;

        await db.SaveChangesAsync(ct);
        foreach (var projectName in rows.Select(r => r.ProjectName).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct())
            await materialCatalog.EnsureProjectAsync(projectName, ct);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvoiceDetailResponse>> Get(int id, CancellationToken ct)
    {
        var row = await db.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.InvoiceId == id, ct);
        if (row is null) return NotFound();
        return Ok(ToDetail(row));
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<InvoiceUploadResponse>> Upload(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Choose an invoice file to upload (PDF, DOCX, JPG, or PNG)." });

        if (file.Length > MaxBytes)
            return BadRequest(new { message = $"File exceeds maximum size of {MaxBytes / (1024 * 1024)} MB." });

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            return BadRequest(new { message = "Supported formats: PDF, DOCX, JPG, JPEG, PNG." });

        var (storedName, size) = await storage.SaveAsync(file, ext, ct);

        MaterialExtractionResult extracted;
        await using (var read = System.IO.File.OpenRead(storage.GetFullPath(storedName)!))
        {
            extracted = await extraction.ExtractAsync(read, ext, ct);
        }

        var duplicate = await InvoiceDuplicateChecker.FindDuplicateAsync(
            db,
            extracted.InvoiceNumber,
            extracted.InvoiceDate,
            extracted.VendorName,
            extracted.TotalAmount,
            excludeInvoiceId: null,
            ct);
        if (duplicate is not null)
        {
            storage.TryDelete(storedName);
            return Conflict(new
            {
                message = DuplicateMessage(duplicate)
            });
        }

        var entity = new Invoice
        {
            OriginalFileName = file.FileName,
            StoredFileName = storedName,
            ContentType = ContentTypeFor(ext, file.ContentType),
            FileSizeBytes = size,
            UploadedAtUtc = DateTime.UtcNow,
            ExtractedText = extracted.FullText,
            InvoiceNumber = extracted.InvoiceNumber,
            InvoiceDate = extracted.InvoiceDate,
            VendorName = extracted.VendorName,
            ProjectName = extracted.ProjectName,
            SgstAmount = extracted.SgstAmount,
            CgstAmount = extracted.CgstAmount,
            IgstAmount = extracted.IgstAmount,
            TransportCharges = extracted.TransportCharges,
            BasicTotal = extracted.BasicTotal,
            TotalAmount = extracted.TotalAmount,
            ExtractionStatus = extracted.Status,
            ExtractionNotes = extracted.Notes
        };

        foreach (var line in extracted.Materials)
        {
            entity.MaterialItems.Add(new MaterialInvoiceItem
            {
                MaterialName = line.MaterialName,
                Quantity = line.Quantity
            });
        }

        db.Invoices.Add(entity);
        await db.SaveChangesAsync(ct);

        await materialCatalog.EnsureProjectAsync(entity.ProjectName, ct);
        await materialCatalog.EnsureVendorAsync(entity.VendorName, ct);

        return Ok(new InvoiceUploadResponse(ToDetail(entity)));
    }

    [HttpGet("{id:int}/file")]
    public async Task<IActionResult> DownloadFile(int id, [FromQuery] bool inline = false, CancellationToken ct = default)
    {
        var row = await db.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.InvoiceId == id, ct);
        if (row is null) return NotFound();

        var path = storage.GetFullPath(row.StoredFileName);
        if (path is null) return NotFound(new { message = "Stored file not found on disk." });

        if (inline)
            return PhysicalFile(path, row.ContentType);

        return PhysicalFile(path, row.ContentType, row.OriginalFileName);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<InvoiceDetailResponse>> Update(int id, [FromBody] UpdateInvoiceRequest body, CancellationToken ct)
    {
        var row = await db.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == id, ct);
        if (row is null) return NotFound();

        var newNumber = string.IsNullOrWhiteSpace(body.InvoiceNumber) ? null : body.InvoiceNumber.Trim();
        var newDate = InvoiceDateParser.Parse(body.InvoiceDate);
        var newVendor = string.IsNullOrWhiteSpace(body.VendorName) ? null : body.VendorName.Trim();

        var duplicate = await InvoiceDuplicateChecker.FindDuplicateAsync(
            db,
            newNumber,
            newDate,
            newVendor,
            body.TotalAmount,
            excludeInvoiceId: id,
            ct);
        if (duplicate is not null)
            return Conflict(new { message = DuplicateMessage(duplicate) });

        row.InvoiceNumber = newNumber;
        row.InvoiceDate = newDate;
        row.VendorName = newVendor;
        row.ProjectName = string.IsNullOrWhiteSpace(body.ProjectName) ? null : body.ProjectName.Trim();
        row.SgstAmount = body.SgstAmount;
        row.CgstAmount = body.CgstAmount;
        row.IgstAmount = body.IgstAmount;
        row.TransportCharges = body.TransportCharges;
        row.BasicTotal = body.BasicTotal;
        row.TotalAmount = body.TotalAmount;
        row.ExtractionNotes = "Saved after user review.";

        await db.SaveChangesAsync(ct);
        await materialCatalog.EnsureProjectAsync(row.ProjectName, ct);
        await materialCatalog.EnsureVendorAsync(row.VendorName, ct);
        return Ok(ToDetail(row));
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportExcel(CancellationToken ct)
    {
        var rows = await db.Invoices.AsNoTracking()
            .OrderByDescending(i => i.UploadedAtUtc)
            .ToListAsync(ct);

        if (rows.Count == 0)
            return NotFound(new { message = "No invoices to export." });

        var bytes = excelExport.BuildWorkbook(rows);
        var fileName = $"Invoices_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var row = await db.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == id, ct);
        if (row is null) return NotFound();

        storage.TryDelete(row.StoredFileName);
        db.Invoices.Remove(row);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string ContentTypeFor(string ext, string? uploaded) =>
        !string.IsNullOrWhiteSpace(uploaded) ? uploaded : ext.ToLowerInvariant() switch
        {
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/pdf"
        };

    private static string DuplicateMessage(Invoice existing)
    {
        var label = !string.IsNullOrWhiteSpace(existing.InvoiceNumber)
            ? $"invoice number \"{existing.InvoiceNumber}\""
            : $"vendor \"{existing.VendorName}\" on {FormatDisplayDate(existing.InvoiceDate)} with total {existing.TotalAmount}";
        return $"Duplicate invoice not allowed. An invoice with the same {label} already exists.";
    }

    private static string? FormatDisplayDate(DateOnly? date)
    {
        if (date is null) return null;
        var d = date.Value;
        var mon = d.ToString("MMM", CultureInfo.InvariantCulture).ToLowerInvariant();
        return $"{d.Day}-{mon}-{d.Year}";
    }

    private static InvoiceListItemResponse ToListItem(Invoice i) =>
        new(
            i.InvoiceId,
            i.OriginalFileName,
            i.InvoiceNumber,
            FormatDisplayDate(i.InvoiceDate),
            i.VendorName,
            i.SgstAmount,
            i.CgstAmount,
            i.IgstAmount,
            i.TransportCharges,
            i.BasicTotal,
            i.TotalAmount,
            i.ProjectName,
            i.ExtractionStatus);

    private static InvoiceDetailResponse ToDetail(Invoice i) =>
        new(
            i.InvoiceId,
            i.OriginalFileName,
            i.InvoiceNumber,
            FormatDisplayDate(i.InvoiceDate),
            i.VendorName,
            i.ProjectName,
            i.SgstAmount,
            i.CgstAmount,
            i.IgstAmount,
            i.TransportCharges,
            i.BasicTotal,
            i.TotalAmount,
            i.ExtractionStatus,
            i.ExtractionNotes,
            i.ExtractedText);
}
