using DocumentFormat.OpenXml.Packaging;
using A = DocumentFormat.OpenXml.Drawing;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using S = DocumentFormat.OpenXml.Spreadsheet;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace edp_gui_admin;

public static partial class AdminExcelReportExporter
{
    private static void AddReportChart(
        WorksheetPart worksheetPart,
        string reportName,
        IReadOnlyList<AdminReportRow> rows)
    {
        var useLineChart = IsMonthlyLineReport(reportName);
        AddChart(
            worksheetPart,
            useLineChart ? $"{reportName} Monthly Year-to-Date" : $"{reportName} Status Summary",
            useLineChart ? BuildMonthlyYearToDateSummary(rows) : BuildStatusSummary(rows),
            useLineChart);
    }

    private static void AddMaintenanceStatusChart(WorksheetPart worksheetPart, IReadOnlyList<AdminReportRow> rows)
    {
        AddChart(
            worksheetPart,
            "Maintenance Open vs Resolved",
            BuildMaintenanceOpenResolvedSummary(rows),
            useLineChart: false);
    }

    private static void AddChart(
        WorksheetPart worksheetPart,
        string title,
        SortedDictionary<string, int> summary,
        bool useLineChart)
    {
        if (summary.Count == 0)
        {
            summary.Add("No Records", 0);
        }

        var drawingsPart = worksheetPart.AddNewPart<DrawingsPart>();
        worksheetPart.Worksheet!.Append(new S.Drawing { Id = worksheetPart.GetIdOfPart(drawingsPart) });

        var chartPart = drawingsPart.AddNewPart<ChartPart>();
        chartPart.ChartSpace = new C.ChartSpace();
        chartPart.ChartSpace.Append(new C.EditingLanguage { Val = "en-US" });

        var chart = chartPart.ChartSpace.AppendChild(new C.Chart());
        chart.Append(BuildChartTitle(title));

        var plotArea = chart.AppendChild(new C.PlotArea());
        plotArea.AppendChild(new C.Layout());

        const uint categoryAxisId = 48650112U;
        const uint valueAxisId = 48672768U;
        if (useLineChart)
        {
            AddLineChart(plotArea, summary, categoryAxisId, valueAxisId);
        }
        else
        {
            AddBarChart(plotArea, summary, categoryAxisId, valueAxisId);
        }

        plotArea.Append(BuildCategoryAxis(categoryAxisId, valueAxisId));
        plotArea.Append(BuildValueAxis(valueAxisId, categoryAxisId));
        chart.Append(new C.Legend(new C.LegendPosition { Val = C.LegendPositionValues.Right }, new C.Layout()));
        chart.Append(new C.PlotVisibleOnly { Val = true });

        PositionChart(drawingsPart, chartPart);
    }

    private static void AddLineChart(C.PlotArea plotArea, SortedDictionary<string, int> summary, uint categoryAxisId, uint valueAxisId)
    {
        var lineChart = plotArea.AppendChild(new C.LineChart(
            new C.Grouping { Val = C.GroupingValues.Standard },
            new C.VaryColors { Val = false }));
        var series = lineChart.AppendChild(new C.LineChartSeries(
            new C.Index { Val = 0U },
            new C.Order { Val = 0U },
            new C.SeriesText(new C.NumericValue("Records")),
            new C.Marker(new C.Symbol { Val = C.MarkerStyleValues.Circle })));
        AddSeriesPoints(series.AppendChild(new C.CategoryAxisData()), series.AppendChild(new C.Values()), summary);
        lineChart.Append(new C.AxisId { Val = categoryAxisId }, new C.AxisId { Val = valueAxisId });
    }

    private static void AddBarChart(C.PlotArea plotArea, SortedDictionary<string, int> summary, uint categoryAxisId, uint valueAxisId)
    {
        var barChart = plotArea.AppendChild(new C.BarChart(
            new C.BarDirection { Val = C.BarDirectionValues.Column },
            new C.BarGrouping { Val = C.BarGroupingValues.Clustered }));
        var series = barChart.AppendChild(new C.BarChartSeries(
            new C.Index { Val = 0U },
            new C.Order { Val = 0U },
            new C.SeriesText(new C.NumericValue("Records"))));
        AddSeriesPoints(series.AppendChild(new C.CategoryAxisData()), series.AppendChild(new C.Values()), summary);
        barChart.Append(new C.AxisId { Val = categoryAxisId }, new C.AxisId { Val = valueAxisId });
    }

    private static void AddSeriesPoints(C.CategoryAxisData categoryData, C.Values values, SortedDictionary<string, int> summary)
    {
        var categoryLiteral = categoryData.AppendChild(new C.StringLiteral());
        categoryLiteral.Append(new C.PointCount { Val = (uint)summary.Count });

        var valueLiteral = values.AppendChild(new C.NumberLiteral());
        valueLiteral.Append(new C.FormatCode("General"));
        valueLiteral.Append(new C.PointCount { Val = (uint)summary.Count });

        var index = 0U;
        foreach (var item in summary)
        {
            categoryLiteral.AppendChild(new C.StringPoint { Index = index }).Append(new C.NumericValue(item.Key));
            valueLiteral.AppendChild(new C.NumericPoint { Index = index }).Append(new C.NumericValue(item.Value.ToString()));
            index++;
        }
    }

    private static C.Title BuildChartTitle(string title)
    {
        return new C.Title(
            new C.ChartText(new C.RichText(
                new A.BodyProperties(),
                new A.ListStyle(),
                new A.Paragraph(new A.Run(new A.Text(title))))));
    }

    private static void PositionChart(DrawingsPart drawingsPart, ChartPart chartPart)
    {
        drawingsPart.WorksheetDrawing = new Xdr.WorksheetDrawing();
        var anchor = drawingsPart.WorksheetDrawing.AppendChild(new Xdr.TwoCellAnchor());
        anchor.Append(
            new Xdr.FromMarker(new Xdr.ColumnId("3"), new Xdr.ColumnOffset("0"), new Xdr.RowId("2"), new Xdr.RowOffset("0")),
            new Xdr.ToMarker(new Xdr.ColumnId("10"), new Xdr.ColumnOffset("0"), new Xdr.RowId("18"), new Xdr.RowOffset("0")));

        var frame = anchor.AppendChild(new Xdr.GraphicFrame { Macro = string.Empty });
        frame.Append(
            new Xdr.NonVisualGraphicFrameProperties(
                new Xdr.NonVisualDrawingProperties { Id = 2U, Name = "Chart 1" },
                new Xdr.NonVisualGraphicFrameDrawingProperties()),
            new Xdr.Transform(new A.Offset { X = 0L, Y = 0L }, new A.Extents { Cx = 0L, Cy = 0L }),
            new A.Graphic(new A.GraphicData(new C.ChartReference { Id = drawingsPart.GetIdOfPart(chartPart) })
            {
                Uri = "http://schemas.openxmlformats.org/drawingml/2006/chart"
            }));
        anchor.Append(new Xdr.ClientData());
    }
}
