using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using S = DocumentFormat.OpenXml.Spreadsheet;

namespace edp_gui_admin;

public static partial class AdminExcelReportExporter
{
    public static void Export(
        string reportName,
        IReadOnlyList<AdminReportRow> rows,
        string signatoryName,
        string outputPath)
    {
        if (string.IsNullOrWhiteSpace(reportName))
        {
            throw new ArgumentException("Report name is required.", nameof(reportName));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path is required.", nameof(outputPath));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

        using var document = SpreadsheetDocument.Create(outputPath, SpreadsheetDocumentType.Workbook);
        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new S.Workbook();

        var reportPart = workbookPart.AddNewPart<WorksheetPart>();
        reportPart.Worksheet = BuildReportWorksheet(reportName, rows, signatoryName);

        var graphPart = workbookPart.AddNewPart<WorksheetPart>();
        graphPart.Worksheet = BuildGraphWorksheet(reportName, rows);

        WorksheetPart? statusGraphPart = null;
        if (IsMaintenanceReport(reportName))
        {
            statusGraphPart = workbookPart.AddNewPart<WorksheetPart>();
            statusGraphPart.Worksheet = BuildMaintenanceStatusGraphWorksheet(rows);
        }

        var sheets = workbookPart.Workbook.AppendChild(new S.Sheets());
        sheets.Append(new S.Sheet
        {
            Id = workbookPart.GetIdOfPart(reportPart),
            SheetId = 1,
            Name = SafeSheetName($"{reportName} Report")
        });
        sheets.Append(new S.Sheet
        {
            Id = workbookPart.GetIdOfPart(graphPart),
            SheetId = 2,
            Name = "Graph"
        });
        if (statusGraphPart is not null)
        {
            sheets.Append(new S.Sheet
            {
                Id = workbookPart.GetIdOfPart(statusGraphPart),
                SheetId = 3,
                Name = "Status Graph"
            });
        }

        AddReportChart(graphPart, reportName, rows);
        if (statusGraphPart is not null)
        {
            AddMaintenanceStatusChart(statusGraphPart, rows);
        }

        workbookPart.Workbook.Save();
    }

    private static S.Worksheet BuildReportWorksheet(
        string reportName,
        IReadOnlyList<AdminReportRow> rows,
        string signatoryName)
    {
        var data = new S.SheetData();
        data.Append(
            Row(1, TextCell("A1", "Co-Siter")),
            Row(2, TextCell("A2", $"{reportName} Report")),
            Row(3, TextCell("A3", $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")),
            Row(4, TextCell("A4", $"Prepared / Approved By: {signatoryName.Trim()}")),
            Row(6,
                TextCell("A6", "Category"),
                TextCell("B6", "Parent"),
                TextCell("C6", "Item"),
                TextCell("D6", "Status"),
                TextCell("E6", "Date"),
                TextCell("F6", "Notes")));

        var rowIndex = 7U;
        foreach (var row in rows)
        {
            data.Append(Row(rowIndex,
                TextCell($"A{rowIndex}", row.Category),
                TextCell($"B{rowIndex}", row.ParentName),
                TextCell($"C{rowIndex}", row.ItemName),
                TextCell($"D{rowIndex}", row.Status),
                TextCell($"E{rowIndex}", row.Date?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty),
                TextCell($"F{rowIndex}", row.Notes)));
            rowIndex++;
        }

        return new S.Worksheet(data);
    }

    private static S.Worksheet BuildGraphWorksheet(string reportName, IReadOnlyList<AdminReportRow> rows)
    {
        var summary = IsMonthlyLineReport(reportName)
            ? BuildMonthlyYearToDateSummary(rows)
            : BuildStatusSummary(rows);
        var data = new S.SheetData();
        data.Append(
            Row(1, TextCell("A1", "Graph Data")),
            Row(3, TextCell("A3", IsMonthlyLineReport(reportName) ? "Month" : "Status"), TextCell("B3", "Count")));

        var rowIndex = 4U;
        foreach (var item in summary)
        {
            data.Append(Row(rowIndex, TextCell($"A{rowIndex}", item.Key), NumberCell($"B{rowIndex}", item.Value)));
            rowIndex++;
        }

        return new S.Worksheet(data);
    }

    private static S.Worksheet BuildMaintenanceStatusGraphWorksheet(IReadOnlyList<AdminReportRow> rows)
    {
        var summary = BuildMaintenanceOpenResolvedSummary(rows);
        var data = new S.SheetData();
        data.Append(
            Row(1, TextCell("A1", "Maintenance Open vs Resolved")),
            Row(3, TextCell("A3", "Status"), TextCell("B3", "Count")));

        var rowIndex = 4U;
        foreach (var item in summary)
        {
            data.Append(Row(rowIndex, TextCell($"A{rowIndex}", item.Key), NumberCell($"B{rowIndex}", item.Value)));
            rowIndex++;
        }

        return new S.Worksheet(data);
    }

    private static S.Row Row(uint index, params S.Cell[] cells)
    {
        var row = new S.Row { RowIndex = index };
        row.Append(cells);
        return row;
    }

    private static S.Cell TextCell(string reference, string value)
    {
        return new S.Cell
        {
            CellReference = reference,
            DataType = S.CellValues.InlineString,
            InlineString = new S.InlineString(new S.Text(value ?? string.Empty))
        };
    }

    private static S.Cell NumberCell(string reference, int value)
    {
        return new S.Cell
        {
            CellReference = reference,
            DataType = S.CellValues.Number,
            CellValue = new S.CellValue(value)
        };
    }

    private static string SafeSheetName(string name)
    {
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());
        return cleaned.Length <= 31 ? cleaned : cleaned[..31];
    }
}
