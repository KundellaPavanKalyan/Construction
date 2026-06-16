using ClosedXML.Excel;
using EmployeePayroll.Api.Contracts;

namespace EmployeePayroll.Api.Services.Material;

public sealed class MaterialGridExcelExportService
{
    public byte[] Build(MaterialGridResponse grid)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Material Tracking");

        ws.Cell(1, 1).Value = "Material Name";
        for (var c = 0; c < grid.InvoiceColumns.Count; c++)
            ws.Cell(1, c + 2).Value = grid.InvoiceColumns[c];
        ws.Cell(1, grid.InvoiceColumns.Count + 2).Value = "Total";

        ws.Row(1).Style.Font.Bold = true;
        ws.Row(1).Style.Fill.BackgroundColor = XLColor.LightGray;

        var rowNum = 2;
        foreach (var row in grid.Rows)
        {
            ws.Cell(rowNum, 1).Value = row.MaterialName;
            for (var c = 0; c < row.Quantities.Count; c++)
            {
                if (row.Quantities[c] is { } q)
                    ws.Cell(rowNum, c + 2).Value = q;
            }
            ws.Cell(rowNum, grid.InvoiceColumns.Count + 2).Value = row.RowTotal;
            rowNum++;
        }

        if (grid.Rows.Count > 0)
        {
            ws.Cell(rowNum, 1).Value = "GRAND TOTAL";
            ws.Cell(rowNum, grid.InvoiceColumns.Count + 2).Value = grid.GrandTotal;
            ws.Row(rowNum).Style.Font.Bold = true;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
