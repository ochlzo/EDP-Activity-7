using edp_gui_app;

namespace edp_gui_app.Tests;

[TestClass]
public sealed class DocumentRequestEmailBodyTests
{
    [TestMethod]
    public void Build_IncludesTenantUrlAndRequestedDocuments()
    {
        var body = DocumentRequestEmailBody.Build(new DocumentRequestEmailMessage(
            "tenant@example.com",
            "Acme Tenant",
            "http://localhost:5087/request/token",
            ["Valid Government ID", "Proof of Billing"]));

        StringAssert.Contains(body, "Acme Tenant");
        StringAssert.Contains(body, "http://localhost:5087/request/token");
        StringAssert.Contains(body, "- Valid Government ID");
        StringAssert.Contains(body, "- Proof of Billing");
    }
}
