using C = DocumentFormat.OpenXml.Drawing.Charts;

namespace edp_gui_admin;

public static partial class AdminExcelReportExporter
{
    private static bool IsMonthlyLineReport(string reportName)
    {
        return string.Equals(reportName, "Monthly Tenant Accommodations", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(reportName, "Maintenance", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMaintenanceReport(string reportName)
    {
        return string.Equals(reportName, "Maintenance", StringComparison.OrdinalIgnoreCase);
    }

    private static SortedDictionary<string, int> BuildMonthlyYearToDateSummary(IReadOnlyList<AdminReportRow> rows)
    {
        var now = DateTime.Now;
        var summary = new SortedDictionary<string, int>(new MonthLabelComparer());
        for (var month = 1; month <= now.Month; month++)
        {
            summary.Add(new DateTime(now.Year, month, 1).ToString("MMM"), 0);
        }

        foreach (var row in rows.Where(row => row.Date?.Year == now.Year && row.Date?.Month <= now.Month))
        {
            var month = row.Date!.Value.ToString("MMM");
            summary[month]++;
        }

        return summary;
    }

    private static SortedDictionary<string, int> BuildStatusSummary(IReadOnlyList<AdminReportRow> rows)
    {
        var summary = new SortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var key = string.IsNullOrWhiteSpace(row.Status) ? "Unspecified" : row.Status.Trim();
            summary[key] = summary.TryGetValue(key, out var count) ? count + 1 : 1;
        }

        return summary;
    }

    private static SortedDictionary<string, int> BuildMaintenanceOpenResolvedSummary(IReadOnlyList<AdminReportRow> rows)
    {
        var summary = new SortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Open"] = 0,
            ["Resolved"] = 0
        };

        foreach (var row in rows)
        {
            if (summary.ContainsKey(row.Status))
            {
                summary[row.Status]++;
            }
        }

        return summary;
    }

    private static C.CategoryAxis BuildCategoryAxis(uint categoryAxisId, uint valueAxisId)
    {
        return new C.CategoryAxis(
            new C.AxisId { Val = categoryAxisId },
            new C.Scaling(new C.Orientation { Val = C.OrientationValues.MinMax }),
            new C.AxisPosition { Val = C.AxisPositionValues.Bottom },
            new C.TickLabelPosition { Val = C.TickLabelPositionValues.NextTo },
            new C.CrossingAxis { Val = valueAxisId },
            new C.Crosses { Val = C.CrossesValues.AutoZero },
            new C.AutoLabeled { Val = true },
            new C.LabelAlignment { Val = C.LabelAlignmentValues.Center },
            new C.LabelOffset { Val = 100 });
    }

    private static C.ValueAxis BuildValueAxis(uint valueAxisId, uint categoryAxisId)
    {
        return new C.ValueAxis(
            new C.AxisId { Val = valueAxisId },
            new C.Scaling(new C.Orientation { Val = C.OrientationValues.MinMax }),
            new C.AxisPosition { Val = C.AxisPositionValues.Left },
            new C.MajorGridlines(),
            new C.NumberingFormat { FormatCode = "General", SourceLinked = true },
            new C.TickLabelPosition { Val = C.TickLabelPositionValues.NextTo },
            new C.CrossingAxis { Val = categoryAxisId },
            new C.Crosses { Val = C.CrossesValues.AutoZero },
            new C.CrossBetween { Val = C.CrossBetweenValues.Between });
    }

    private sealed class MonthLabelComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            return MonthIndex(x).CompareTo(MonthIndex(y));
        }

        private static int MonthIndex(string? label)
        {
            return label switch
            {
                "Jan" => 1,
                "Feb" => 2,
                "Mar" => 3,
                "Apr" => 4,
                "May" => 5,
                "Jun" => 6,
                "Jul" => 7,
                "Aug" => 8,
                "Sep" => 9,
                "Oct" => 10,
                "Nov" => 11,
                "Dec" => 12,
                _ => 13
            };
        }
    }
}
