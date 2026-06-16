using EmployeePayroll.Api;
using EmployeePayroll.Api.Contracts;
using EmployeePayroll.Api.Data;
using EmployeePayroll.Api.Models;
using EmployeePayroll.Api.Services;
using EmployeePayroll.Api.Services.Material;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeePayroll.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/material-system")]
public class MaterialSystemController(
    AppDbContext db,
    InvoiceFileStorage storage,
    MaterialInvoiceExtractionService extraction,
    MaterialCatalogService catalog,
    MaterialGridService gridService,
    MaterialGridExcelExportService excelExport,
    IConfiguration configuration) : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };

    private long MaxBytes => configuration.GetValue("InvoiceUpload:MaxFileSizeBytes", 10 * 1024 * 1024);

    [HttpGet("projects")]
    public async Task<ActionResult<IReadOnlyList<string>>> ListProjects(CancellationToken ct) =>
        Ok(await catalog.ListProjectsFromInvoicesAsync(ct));

    [HttpGet("vendors")]
    public async Task<ActionResult<IReadOnlyList<string>>> ListVendors(CancellationToken ct) =>
        Ok(await catalog.ListVendorsFromInvoicesAsync(ct));

    [HttpPost("projects")]
    public async Task<ActionResult<TrackingNameResponse>> CreateProject(
        [FromBody] CreateTrackingNameRequest body,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(new { message = "Project name is required." });
        await catalog.EnsureProjectAsync(body.Name.Trim(), ct);
        var row = await db.TrackingProjects.AsNoTracking()
            .FirstAsync(p => p.ProjectName.ToUpper() == body.Name.Trim().ToUpper(), ct);
        return Ok(new TrackingNameResponse(row.ProjectId, row.ProjectName));
    }

    [HttpPost("vendors")]
    public async Task<ActionResult<TrackingNameResponse>> CreateVendor(
        [FromBody] CreateTrackingNameRequest body,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(new { message = "Vendor name is required." });
        await catalog.EnsureVendorAsync(body.Name.Trim(), ct);
        var row = await db.TrackingVendors.AsNoTracking()
            .FirstAsync(v => v.VendorName.ToUpper() == body.Name.Trim().ToUpper(), ct);
        return Ok(new TrackingNameResponse(row.VendorId, row.VendorName));
    }

    [HttpDelete("projects")]
    public async Task<IActionResult> DeleteProject([FromQuery] string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Project name is required." });

        var deleted = await catalog.DeleteProjectAsync(name.Trim(), ct);
        if (!deleted)
            return NotFound(new { message = $"Project \"{name.Trim()}\" was not found." });

        return NoContent();
    }

    [HttpDelete("vendors")]
    public async Task<IActionResult> DeleteVendor([FromQuery] string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Vendor name is required." });

        var deleted = await catalog.DeleteVendorAsync(name.Trim(), ct);
        if (!deleted)
            return NotFound(new { message = $"Vendor \"{name.Trim()}\" was not found." });

        return NoContent();
    }

    [HttpGet("grid")]
    public async Task<ActionResult<MaterialGridResponse>> GetGrid(
        [FromQuery] string mode,
        [FromQuery] string name,
        [FromQuery] byte month,
        [FromQuery] int year,
        [FromQuery] string? search = null,
        [FromQuery] string? vendor = null,
        [FromQuery] string? project = null,
        CancellationToken ct = default)
    {
        if (!YearPolicy.IsValidPayrollYear(year))
            return BadRequest(new { message = "Invalid year." });
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Project or vendor name is required." });
        if (mode is not ("project" or "vendor"))
            return BadRequest(new { message = "Mode must be project or vendor." });

        var grid = await gridService.BuildGridAsync(
            mode, name.Trim(), month, year, search, vendor, project, ct);
        return Ok(grid);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportGrid(
        [FromQuery] string mode,
        [FromQuery] string name,
        [FromQuery] byte month,
        [FromQuery] int year,
        [FromQuery] string? search = null,
        [FromQuery] string? vendor = null,
        [FromQuery] string? project = null,
        CancellationToken ct = default)
    {
        if (!YearPolicy.IsValidPayrollYear(year))
            return BadRequest(new { message = "Invalid year." });

        var grid = await gridService.BuildGridAsync(
            mode, name.Trim(), month, year, search, vendor, project, ct);
        if (grid.Rows.Count == 0)
            return NotFound(new { message = "No data to export for the current filters." });

        var bytes = excelExport.Build(grid);
        var safeName = string.Join('_', name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Material_{mode}_{safeName}_{month}_{year}.xlsx");
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<MaterialInvoiceUploadResponse>> Upload(
        IFormFile? file,
        [FromQuery] string mode,
        [FromQuery] string name,
        [FromQuery] byte month,
        [FromQuery] int year,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Choose an invoice file (PDF, JPG, or PNG)." });
        if (file.Length > MaxBytes)
            return BadRequest(new { message = $"File exceeds maximum size of {MaxBytes / (1024 * 1024)} MB." });
        if (mode is not ("project" or "vendor") || string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Mode (project/vendor) and name are required." });
        if (!YearPolicy.IsValidPayrollYear(year))
            return BadRequest(new { message = "Invalid year." });

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            return BadRequest(new { message = "Supported formats: PDF, JPG, JPEG, PNG." });

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
            return Conflict(new { message = "Invoice Already Exists" });
        }

        var projectName = string.Equals(mode, "project", StringComparison.OrdinalIgnoreCase)
            ? name.Trim()
            : extracted.ProjectName;
        var vendorName = string.Equals(mode, "vendor", StringComparison.OrdinalIgnoreCase)
            ? name.Trim()
            : extracted.VendorName;

        if (string.IsNullOrWhiteSpace(projectName) && mode == "project")
            projectName = name.Trim();
        if (string.IsNullOrWhiteSpace(vendorName) && mode == "vendor")
            vendorName = name.Trim();

        var invoiceDate = extracted.InvoiceDate ?? new DateOnly(year, month, 1);

        var entity = new Invoice
        {
            OriginalFileName = file.FileName,
            StoredFileName = storedName,
            ContentType = ContentTypeFor(ext, file.ContentType),
            FileSizeBytes = size,
            UploadedAtUtc = DateTime.UtcNow,
            ExtractedText = extracted.FullText,
            InvoiceNumber = extracted.InvoiceNumber,
            InvoiceDate = invoiceDate,
            VendorName = vendorName,
            ProjectName = projectName,
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

        if (entity.MaterialItems.Count == 0 && !string.IsNullOrWhiteSpace(extracted.Notes))
            entity.ExtractionNotes = (extracted.Notes ?? "") + " No material lines found on invoice.";

        db.Invoices.Add(entity);
        await db.SaveChangesAsync(ct);

        await catalog.EnsureProjectAsync(projectName, ct);
        await catalog.EnsureVendorAsync(vendorName, ct);

        var grid = await gridService.BuildGridAsync(mode, name.Trim(), month, year, null, null, null, ct);

        return Ok(new MaterialInvoiceUploadResponse(
            entity.InvoiceId,
            entity.InvoiceNumber,
            entity.VendorName,
            entity.ProjectName,
            entity.MaterialItems.Count,
            grid));
    }

    private static string ContentTypeFor(string ext, string? uploaded) =>
        !string.IsNullOrWhiteSpace(uploaded) ? uploaded : ext.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/pdf"
        };
}
