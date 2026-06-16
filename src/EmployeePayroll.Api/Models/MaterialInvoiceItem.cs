namespace EmployeePayroll.Api.Models;

public class MaterialInvoiceItem
{
    public int MaterialInvoiceItemId { get; set; }
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public string MaterialName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}
