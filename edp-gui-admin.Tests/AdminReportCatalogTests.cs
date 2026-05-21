using edp_gui_admin;

namespace edp_gui_admin.Tests;

[TestClass]
public sealed class AdminReportCatalogTests
{
    [TestMethod]
    public void FilterRows_ReturnsOnlySelectedPrimaryTransactionRows()
    {
        var rows = new[]
        {
            new AdminReportRow("Monthly Tenant Accommodations", "Room 1", "Tenant A", "Assigned", null, ""),
            new AdminReportRow("Maintenance", "Site A", "Leak", "Open", null, ""),
            new AdminReportRow("Activity Log", "admin", "Room #1", "Updated", null, "")
        };

        var filtered = AdminReportCatalog.FilterRows("Monthly Tenant Accommodations", rows);

        Assert.HasCount(1, filtered);
        Assert.AreEqual("Room 1", filtered[0].ParentName);
        CollectionAssert.AreEqual(
            new[] { "Monthly Tenant Accommodations", "Maintenance", "Document Compliance" },
            AdminReportCatalog.ReportNames.ToArray());
    }
}
