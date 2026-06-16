using EmployeePayroll.Api.Models;
using Microsoft.EntityFrameworkCore;
using EmployeePayroll.Api.Data;

namespace EmployeePayroll.Api.Services;

public static class InvoiceDuplicateChecker
{
    public static string? NormalizeInvoiceNumber(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    public static async Task<Invoice?> FindDuplicateAsync(
        AppDbContext db,
        string? invoiceNumber,
        DateOnly? invoiceDate,
        string? vendorName,
        decimal? totalAmount,
        int? excludeInvoiceId,
        CancellationToken ct)
    {
        var normalizedNumber = NormalizeInvoiceNumber(invoiceNumber);
        if (normalizedNumber is not null)
        {
            var query = db.Invoices.AsNoTracking()
                .Where(i => i.InvoiceNumber != null && i.InvoiceNumber.ToUpper() == normalizedNumber);
            if (excludeInvoiceId is not null)
                query = query.Where(i => i.InvoiceId != excludeInvoiceId.Value);
            return await query.FirstOrDefaultAsync(ct);
        }

        if (invoiceDate is null || totalAmount is null || string.IsNullOrWhiteSpace(vendorName))
            return null;

        var vendorKey = vendorName.Trim().ToUpperInvariant();
        var queryComposite = db.Invoices.AsNoTracking()
            .Where(i =>
                i.InvoiceDate == invoiceDate
                && i.TotalAmount == totalAmount
                && i.VendorName != null
                && i.VendorName.ToUpper() == vendorKey);
        if (excludeInvoiceId is not null)
            queryComposite = queryComposite.Where(i => i.InvoiceId != excludeInvoiceId.Value);

        return await queryComposite.FirstOrDefaultAsync(ct);
    }
}
