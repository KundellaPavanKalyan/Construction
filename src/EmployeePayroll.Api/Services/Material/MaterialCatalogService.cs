using EmployeePayroll.Api.Data;
using EmployeePayroll.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeePayroll.Api.Services.Material;

public sealed class MaterialCatalogService(AppDbContext db)
{
    public async Task EnsureProjectAsync(string? projectName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(projectName)) return;
        var name = projectName.Trim();
        var exists = await db.TrackingProjects.AsNoTracking()
            .AnyAsync(p => p.ProjectName.ToUpper() == name.ToUpper(), ct);
        if (exists) return;
        db.TrackingProjects.Add(new TrackingProject
        {
            ProjectName = name,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task EnsureVendorAsync(string? vendorName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vendorName)) return;
        var name = vendorName.Trim();
        var exists = await db.TrackingVendors.AsNoTracking()
            .AnyAsync(v => v.VendorName.ToUpper() == name.ToUpper(), ct);
        if (exists) return;
        db.TrackingVendors.Add(new TrackingVendor
        {
            VendorName = name,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<string>> ListProjectsAsync(CancellationToken ct) =>
        await db.TrackingProjects.AsNoTracking()
            .OrderBy(p => p.ProjectName)
            .Select(p => p.ProjectName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> ListVendorsAsync(CancellationToken ct) =>
        await db.TrackingVendors.AsNoTracking()
            .OrderBy(v => v.VendorName)
            .Select(v => v.VendorName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> ListProjectsFromInvoicesAsync(CancellationToken ct)
    {
        var fromInvoices = await db.Invoices.AsNoTracking()
            .Where(i => i.ProjectName != null && i.ProjectName != "")
            .Select(i => i.ProjectName!)
            .Distinct()
            .ToListAsync(ct);
        var fromTable = await ListProjectsAsync(ct);
        return fromInvoices.Concat(fromTable)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> ListVendorsFromInvoicesAsync(CancellationToken ct)
    {
        var fromInvoices = await db.Invoices.AsNoTracking()
            .Where(i => i.VendorName != null && i.VendorName != "")
            .Select(i => i.VendorName!)
            .Distinct()
            .ToListAsync(ct);
        var fromTable = await ListVendorsAsync(ct);
        return fromInvoices.Concat(fromTable)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<bool> DeleteProjectAsync(string name, CancellationToken ct)
    {
        var key = name.Trim();
        if (string.IsNullOrEmpty(key)) return false;

        var projects = (await db.TrackingProjects.ToListAsync(ct))
            .Where(p => NamesMatch(p.ProjectName, key))
            .ToList();
        if (projects.Count > 0)
            db.TrackingProjects.RemoveRange(projects);

        var invoices = (await db.Invoices
                .Where(i => i.ProjectName != null && i.ProjectName != "")
                .ToListAsync(ct))
            .Where(i => NamesMatch(i.ProjectName!, key))
            .ToList();
        foreach (var inv in invoices)
            inv.ProjectName = null;

        if (projects.Count == 0 && invoices.Count == 0)
            return false;

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteVendorAsync(string name, CancellationToken ct)
    {
        var key = name.Trim();
        if (string.IsNullOrEmpty(key)) return false;

        var vendors = (await db.TrackingVendors.ToListAsync(ct))
            .Where(v => NamesMatch(v.VendorName, key))
            .ToList();
        if (vendors.Count > 0)
            db.TrackingVendors.RemoveRange(vendors);

        var invoices = (await db.Invoices
                .Where(i => i.VendorName != null && i.VendorName != "")
                .ToListAsync(ct))
            .Where(i => NamesMatch(i.VendorName!, key))
            .ToList();
        foreach (var inv in invoices)
            inv.VendorName = null;

        if (vendors.Count == 0 && invoices.Count == 0)
            return false;

        await db.SaveChangesAsync(ct);
        return true;
    }

    private static bool NamesMatch(string stored, string requested) =>
        string.Equals(stored.Trim(), requested.Trim(), StringComparison.OrdinalIgnoreCase);
}
