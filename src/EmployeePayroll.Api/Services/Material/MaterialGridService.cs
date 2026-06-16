using System.Text.RegularExpressions;
using EmployeePayroll.Api.Contracts;
using EmployeePayroll.Api.Data;
using EmployeePayroll.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeePayroll.Api.Services.Material;

public sealed class MaterialGridService(
    AppDbContext db,
    InvoiceFileStorage storage,
    MaterialInvoiceExtractionService extraction)
{
    private const int MaxInvoiceColumns = 20;

    public async Task<MaterialGridResponse> BuildGridAsync(
        string mode,
        string entityName,
        byte month,
        int year,
        string? search,
        string? vendorFilter,
        string? projectFilter,
        CancellationToken ct)
    {
        var invoices = await QueryInvoicesAsync(mode, entityName, month, year, search, vendorFilter, projectFilter, ct);
        await BackfillMaterialItemsAsync(invoices, ct);

        // Reload material items after backfill
        var invoiceIds = invoices.Select(i => i.InvoiceId).ToList();
        var items = invoiceIds.Count == 0
            ? new List<MaterialInvoiceItem>()
            : await db.MaterialInvoiceItems.AsNoTracking()
                .Where(m => invoiceIds.Contains(m.InvoiceId))
                .ToListAsync(ct);

        var columns = invoices
            .Select(i => FormatInvoiceColumnHeader(i.InvoiceNumber, i.InvoiceId))
            .ToList();

        var invoiceOrder = invoices.Select(i => i.InvoiceId).ToList();
        var colCount = columns.Count;
        var byMaterial = new Dictionary<string, decimal?[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var colIndex = invoiceOrder.IndexOf(item.InvoiceId);
            if (colIndex < 0) continue;

            if (!byMaterial.TryGetValue(item.MaterialName, out var arr))
            {
                arr = new decimal?[colCount];
                byMaterial[item.MaterialName] = arr;
            }
            arr[colIndex] = (arr[colIndex] ?? 0) + item.Quantity;
        }

        var rows = byMaterial
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv =>
            {
                var qtys = kv.Value.Take(colCount).ToList();
                while (qtys.Count < colCount) qtys.Add(null);
                var total = qtys.Where(q => q.HasValue).Sum(q => q!.Value);
                return new MaterialGridRowDto(kv.Key, qtys, total);
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            rows = rows.Where(r =>
                    r.MaterialName.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || columns.Any(c => c.Contains(term, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var grand = rows.Sum(r => r.RowTotal);
        var hint = rows.Count == 0 ? await BuildEmptyHintAsync(mode, entityName, month, year, ct) : null;

        return new MaterialGridResponse(mode, entityName, month, year, columns, rows, grand, hint);
    }

    private async Task<string?> BuildEmptyHintAsync(
        string mode, string entityName, byte month, int year, CancellationToken ct)
    {
        var inPeriod = await db.Invoices.AsNoTracking()
            .Where(MatchesPeriod(month, year))
            .CountAsync(ct);

        if (inPeriod == 0)
            return $"No invoices uploaded in {MonthName(month)} {year}. Upload on the Invoices page, then click Apply filters.";

        var entityKey = entityName.Trim().ToUpperInvariant();
        var entityQuery = db.Invoices.AsNoTracking().Where(MatchesPeriod(month, year));
        entityQuery = string.Equals(mode, "project", StringComparison.OrdinalIgnoreCase)
            ? entityQuery.Where(i =>
                i.ProjectName != null
                && (i.ProjectName.ToUpper() == entityKey || i.ProjectName.ToUpper().Contains(entityKey)))
            : entityQuery.Where(i =>
                i.VendorName != null
                && (i.VendorName.ToUpper() == entityKey || i.VendorName.ToUpper().Contains(entityKey)));

        var inPeriodForEntity = await entityQuery.CountAsync(ct);

        if (inPeriodForEntity == 0)
        {
            var sampleVendor = await db.Invoices.AsNoTracking()
                .Where(MatchesPeriod(month, year))
                .Where(i => i.VendorName != null)
                .Select(i => i.VendorName!)
                .FirstOrDefaultAsync(ct);
            var sampleProject = await db.Invoices.AsNoTracking()
                .Where(MatchesPeriod(month, year))
                .Where(i => i.ProjectName != null)
                .Select(i => i.ProjectName!)
                .FirstOrDefaultAsync(ct);

            if (string.Equals(mode, "project", StringComparison.OrdinalIgnoreCase))
            {
                var msg = $"No invoices linked to project \"{entityName}\" in {MonthName(month)} {year}.";
                if (sampleProject is not null)
                    msg += $" Try opening project: \"{Truncate(sampleProject, 60)}\".";
                if (sampleVendor is not null)
                    msg += $" Or use Vendor tracking for \"{Truncate(sampleVendor, 40)}\".";
                msg += " Set Project name on the Invoices review screen after upload.";
                return msg;
            }

            var vmsg = $"No invoices for vendor \"{entityName}\" in {MonthName(month)} {year}.";
            if (sampleVendor is not null)
                vmsg += $" Try vendor: \"{Truncate(sampleVendor, 50)}\".";
            return vmsg;
        }

        return null;
    }

    private static System.Linq.Expressions.Expression<Func<Invoice, bool>> MatchesPeriod(byte month, int year) =>
        i => (i.UploadedAtUtc.Month == month && i.UploadedAtUtc.Year == year)
              || (i.InvoiceDate != null && i.InvoiceDate.Value.Month == month && i.InvoiceDate.Value.Year == year);

    private static string MonthName(byte month) => month switch
    {
        1 => "January", 2 => "February", 3 => "March", 4 => "April", 5 => "May", 6 => "June",
        7 => "July", 8 => "August", 9 => "September", 10 => "October", 11 => "November", 12 => "December",
        _ => month.ToString()
    };

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";

    private async Task BackfillMaterialItemsAsync(List<Invoice> invoices, CancellationToken ct)
    {
        foreach (var summary in invoices)
        {
            if (summary.MaterialItems.Count > 0) continue;

            var tracked = await db.Invoices
                .Include(i => i.MaterialItems)
                .FirstOrDefaultAsync(i => i.InvoiceId == summary.InvoiceId, ct);
            if (tracked is null || tracked.MaterialItems.Count > 0) continue;

            if (!string.IsNullOrWhiteSpace(tracked.ExtractedText))
            {
                foreach (var line in MaterialLineTextParser.Parse(tracked.ExtractedText))
                {
                    tracked.MaterialItems.Add(new MaterialInvoiceItem
                    {
                        MaterialName = line.MaterialName,
                        Quantity = line.Quantity
                    });
                }
            }

            if (tracked.MaterialItems.Count == 0)
            {
                var path = storage.GetFullPath(tracked.StoredFileName);
                if (path is not null && System.IO.File.Exists(path))
                {
                    var ext = Path.GetExtension(tracked.OriginalFileName);
                    await using var fs = System.IO.File.OpenRead(path);
                    var result = await extraction.ExtractAsync(fs, ext, ct);

                    if (string.IsNullOrWhiteSpace(tracked.ExtractedText))
                        tracked.ExtractedText = result.FullText;
                    if (string.IsNullOrWhiteSpace(tracked.InvoiceNumber))
                        tracked.InvoiceNumber = result.InvoiceNumber;
                    if (tracked.InvoiceDate is null)
                        tracked.InvoiceDate = result.InvoiceDate;
                    if (string.IsNullOrWhiteSpace(tracked.VendorName))
                        tracked.VendorName = result.VendorName;
                    if (string.IsNullOrWhiteSpace(tracked.ProjectName))
                        tracked.ProjectName = result.ProjectName;

                    foreach (var line in result.Materials)
                    {
                        tracked.MaterialItems.Add(new MaterialInvoiceItem
                        {
                            MaterialName = line.MaterialName,
                            Quantity = line.Quantity
                        });
                    }
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static string FormatInvoiceColumnHeader(string? invoiceNumber, int invoiceId)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            return $"INV-{invoiceId}";

        var t = invoiceNumber.Trim();
        if (Regex.IsMatch(t, @"(?i)invoice|inv\.?\s*no"))
            return t;
        if (Regex.IsMatch(t, @"^\d+$"))
            return $"No.{t}";
        return t;
    }

    private async Task<List<Invoice>> QueryInvoicesAsync(
        string mode,
        string entityName,
        byte month,
        int year,
        string? search,
        string? vendorFilter,
        string? projectFilter,
        CancellationToken ct)
    {
        var entityKey = entityName.Trim().ToUpperInvariant();

        var q = db.Invoices.AsNoTracking()
            .Include(i => i.MaterialItems)
            .Where(MatchesPeriod(month, year));

        if (string.Equals(mode, "project", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(i =>
                i.ProjectName != null
                && (i.ProjectName.ToUpper() == entityKey
                    || i.ProjectName.ToUpper().Contains(entityKey)));
        }
        else
        {
            q = q.Where(i =>
                i.VendorName != null
                && (i.VendorName.ToUpper() == entityKey
                    || i.VendorName.ToUpper().Contains(entityKey)));
        }

        if (!string.IsNullOrWhiteSpace(vendorFilter))
        {
            var v = vendorFilter.Trim();
            q = q.Where(i => i.VendorName != null && i.VendorName.Contains(v));
        }

        if (!string.IsNullOrWhiteSpace(projectFilter))
        {
            var p = projectFilter.Trim();
            q = q.Where(i => i.ProjectName != null && i.ProjectName.Contains(p));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(i =>
                (i.InvoiceNumber != null && i.InvoiceNumber.Contains(s))
                || (i.VendorName != null && i.VendorName.Contains(s))
                || (i.ProjectName != null && i.ProjectName.Contains(s))
                || i.MaterialItems.Any(m => m.MaterialName.Contains(s)));
        }

        return await q
            .OrderBy(i => i.InvoiceDate ?? DateOnly.FromDateTime(i.UploadedAtUtc))
            .ThenBy(i => i.InvoiceId)
            .Take(MaxInvoiceColumns)
            .ToListAsync(ct);
    }
}
