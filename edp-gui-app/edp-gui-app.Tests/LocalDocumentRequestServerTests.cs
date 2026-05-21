using edp_gui_app;

namespace edp_gui_app.Tests;

[TestClass]
public sealed class LocalDocumentRequestServerTests
{
    [TestMethod]
    public void BuildRequestPageHtml_RendersOnlyRequestedDocuments()
    {
        var request = new DocumentRequestState(
            "token-1",
            10,
            "Acme Tenant",
            ["Valid Government ID", "Proof of Billing"]);

        var html = LocalDocumentRequestServer.BuildRequestPageHtml(request);

        StringAssert.Contains(html, "Valid Government ID");
        StringAssert.Contains(html, "Proof of Billing");
        Assert.IsFalse(html.Contains("Lease Agreement", StringComparison.Ordinal));
    }
}
