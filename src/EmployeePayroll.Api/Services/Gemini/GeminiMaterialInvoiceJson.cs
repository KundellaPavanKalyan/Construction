using System.Text.Json.Serialization;

namespace EmployeePayroll.Api.Services.Gemini;

internal sealed class GeminiMaterialInvoiceJson
{
    [JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get; set; }

    [JsonPropertyName("invoiceDate")]
    public string? InvoiceDate { get; set; }

    [JsonPropertyName("vendorName")]
    public string? VendorName { get; set; }

    [JsonPropertyName("projectName")]
    public string? ProjectName { get; set; }

    [JsonPropertyName("cgst")]
    public decimal? Cgst { get; set; }

    [JsonPropertyName("sgst")]
    public decimal? Sgst { get; set; }

    [JsonPropertyName("igst")]
    public decimal? Igst { get; set; }

    [JsonPropertyName("basicTotal")]
    public decimal? BasicTotal { get; set; }

    [JsonPropertyName("transportCharges")]
    public decimal? TransportCharges { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal? TotalAmount { get; set; }

    [JsonPropertyName("materials")]
    public List<GeminiMaterialLineJson>? Materials { get; set; }
}

internal sealed class GeminiMaterialLineJson
{
    [JsonPropertyName("materialName")]
    public string? MaterialName { get; set; }

    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }
}
