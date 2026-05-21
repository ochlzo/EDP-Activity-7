using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using edp_gui_admin;
using C = DocumentFormat.OpenXml.Drawing.Charts;

namespace edp_gui_admin.Tests;

[TestClass]
public sealed class AdminExcelReportExporterTests
{
    [TestMethod]
    public void Export_CreatesWorkbookWithReportSheetSummarySheetAndChart()
    {
        var path = Path.Combine(Path.GetTempPath(), $"co-siter-report-{Guid.NewGuid():N}.xlsx");
        var rows = new[]
        {
            new AdminReportRow("Maintenance", "North Site", "Leak Repair", "Open", new DateTime(2026, 5, 15), "Pipe leak"),
            new AdminReportRow("Maintenance", "East Site", "Door Repair", "Resolved", new DateTime(2026, 5, 16), "Done")
        };

        try
        {
            AdminExcelReportExporter.Export("Maintenance", rows, "Maria Santos", path);

            using var document = SpreadsheetDocument.Open(path, false);
            var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part missing.");
            var workbook = workbookPart.Workbook ?? throw new InvalidOperationException("Workbook missing.");
            var sheetList = workbook.GetFirstChild<Sheets>() ?? throw new InvalidOperationException("Sheets missing.");
            var sheets = sheetList.Elements<Sheet>().ToArray();

            Assert.AreEqual("Maintenance Report", sheets[0].Name!.Value);
            Assert.AreEqual("Graph", sheets[1].Name!.Value);

            var reportPart = (WorksheetPart)workbookPart.GetPartById(sheets[0].Id!);
            var reportSheet = reportPart.Worksheet ?? throw new InvalidOperationException("Report sheet missing.");
            var reportText = string.Join("|", reportSheet.Descendants<Cell>().Select(ReadCellValue));
            StringAssert.Contains(reportText, "Co-Siter");
            StringAssert.Contains(reportText, "Prepared / Approved By: Maria Santos");

            var graphPart = (WorksheetPart)workbookPart.GetPartById(sheets[1].Id!);
            Assert.IsNotNull(graphPart.DrawingsPart);
            Assert.IsTrue(graphPart.DrawingsPart!.ChartParts.Any());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [TestMethod]
    public void Export_MaintenanceReportUsesMonthlyYearToDateLineChart()
    {
        var path = Path.Combine(Path.GetTempPath(), $"co-siter-maintenance-{Guid.NewGuid():N}.xlsx");
        var year = DateTime.Now.Year;
        var rows = new[]
        {
            new AdminReportRow("Maintenance", "North Site", "Leak Repair", "Open", new DateTime(year, 1, 12), ""),
            new AdminReportRow("Maintenance", "East Site", "Door Repair", "Resolved", new DateTime(year, 3, 5), "")
        };

        try
        {
            AdminExcelReportExporter.Export("Maintenance", rows, "Maria Santos", path);

            using var document = SpreadsheetDocument.Open(path, false);
            var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part missing.");
            var graphPart = GetWorksheetPart(workbookPart, "Graph");
            var graphText = string.Join("|", graphPart.Worksheet!.Descendants<Cell>().Select(ReadCellValue));
            var chartPart = graphPart.DrawingsPart!.ChartParts.Single();

            StringAssert.Contains(graphText, "Jan");
            StringAssert.Contains(graphText, "Mar");
            Assert.IsTrue(chartPart.ChartSpace!.Descendants<C.LineChart>().Any());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [TestMethod]
    public void Export_MaintenanceReportAddsOpenResolvedStatusBarChartSheet()
    {
        var path = Path.Combine(Path.GetTempPath(), $"co-siter-maintenance-status-{Guid.NewGuid():N}.xlsx");
        var year = DateTime.Now.Year;
        var rows = new[]
        {
            new AdminReportRow("Maintenance", "North Site", "Leak Repair", "Open", new DateTime(year, 1, 12), ""),
            new AdminReportRow("Maintenance", "East Site", "Door Repair", "Resolved", new DateTime(year, 3, 5), ""),
            new AdminReportRow("Maintenance", "West Site", "Window Repair", "Resolved", new DateTime(year, 3, 15), "")
        };

        try
        {
            AdminExcelReportExporter.Export("Maintenance", rows, "Maria Santos", path);

            using var document = SpreadsheetDocument.Open(path, false);
            var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part missing.");
            var statusPart = GetWorksheetPart(workbookPart, "Status Graph");
            var statusText = string.Join("|", statusPart.Worksheet!.Descendants<Cell>().Select(ReadCellValue));
            var chartPart = statusPart.DrawingsPart!.ChartParts.Single();

            StringAssert.Contains(statusText, "Open");
            StringAssert.Contains(statusText, "Resolved");
            Assert.IsTrue(chartPart.ChartSpace!.Descendants<C.BarChart>().Any());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static WorksheetPart GetWorksheetPart(WorkbookPart workbookPart, string sheetName)
    {
        var workbook = workbookPart.Workbook ?? throw new InvalidOperationException("Workbook missing.");
        var sheet = workbook.Descendants<Sheet>()
            .Single(row => string.Equals(row.Name?.Value, sheetName, StringComparison.Ordinal));
        return (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
    }

    private static string ReadCellValue(Cell cell)
    {
        return cell.InlineString?.Text?.Text ?? cell.CellValue?.Text ?? string.Empty;
    }
}
