namespace edp_gui_admin;

public static class AdminReportCatalog
{
    public static IReadOnlyList<string> ReportNames { get; } =
    [
        "Monthly Tenant Accommodations",
        "Maintenance",
        "Document Compliance"
    ];

    public static IReadOnlyList<AdminReportRow> FilterRows(
        string reportName,
        IReadOnlyList<AdminReportRow> rows)
    {
        return rows
            .Where(row => string.Equals(row.Category, reportName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
