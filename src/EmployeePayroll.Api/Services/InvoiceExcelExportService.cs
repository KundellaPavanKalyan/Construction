using ClosedXML.Excel;
using EmployeePayroll.Api.Models;

namespace EmployeePayroll.Api.Services;

public sealed class InvoiceExcelExportService
{
    public byte[] BuildWorkbook(IReadOnlyList<Invoice> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Invoices");

        var headers = new[]
        {
            "Invoice No", "Date", "Vendername", "SGST", "CGST", "IGST",
            "Transportcharges", "Basic Total", "Totalamount"
        };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        var headerRow = ws.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

        var rowNum = 2;
        decimal sumSgst = 0, sumCgst = 0, sumIgst = 0, sumTransport = 0, sumBasic = 0, sumTotal = 0;
        var hasSgst = false;
        var hasCgst = false;
        var hasIgst = false;
        var hasTransport = false;
        var hasBasic = false;
        var hasTotal = false;

        foreach (var inv in rows)
        {
            ws.Cell(rowNum, 1).Value = inv.InvoiceNumber ?? string.Empty;
            ws.Cell(rowNum, 2).Value = FormatDate(inv.InvoiceDate);
            ws.Cell(rowNum, 3).Value = inv.VendorName ?? string.Empty;
            WriteAmount(ws.Cell(rowNum, 4), inv.SgstAmount, ref sumSgst, ref hasSgst);
            WriteAmount(ws.Cell(rowNum, 5), inv.CgstAmount, ref sumCgst, ref hasCgst);
            WriteAmount(ws.Cell(rowNum, 6), inv.IgstAmount, ref sumIgst, ref hasIgst);
            WriteAmount(ws.Cell(rowNum, 7), inv.TransportCharges, ref sumTransport, ref hasTransport);
            WriteAmount(ws.Cell(rowNum, 8), inv.BasicTotal, ref sumBasic, ref hasBasic);
            WriteAmount(ws.Cell(rowNum, 9), inv.TotalAmount, ref sumTotal, ref hasTotal);
            rowNum++;
        }

        if (rows.Count > 0)
        {
            ws.Cell(rowNum, 1).Value = "TOTAL";
            ws.Cell(rowNum, 1).Style.Font.Bold = true;
            if (hasSgst) ws.Cell(rowNum, 4).Value = sumSgst;
            if (hasCgst) ws.Cell(rowNum, 5).Value = sumCgst;
            if (hasIgst) ws.Cell(rowNum, 6).Value = sumIgst;
            if (hasTransport) ws.Cell(rowNum, 7).Value = sumTransport;
            if (hasBasic) ws.Cell(rowNum, 8).Value = sumBasic;
            if (hasTotal) ws.Cell(rowNum, 9).Value = sumTotal;
            ws.Row(rowNum).Style.Font.Bold = true;
        }

        ws.RangeUsed()?.SetAutoFilter();
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteAmount(IXLCell cell, decimal? value, ref decimal sum, ref bool hasAny)
    {
        if (value is null) return;
        cell.Value = value.Value;
        sum += value.Value;
        hasAny = true;
    }

    private static string FormatDate(DateOnly? date) =>
        date is null
            ? string.Empty
            : $"{date.Value.Day}-{date.Value:MMM-yyyy}".ToLowerInvariant();
}
