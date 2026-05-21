using edp_gui_app;

namespace edp_gui_app.Tests;

[TestClass]
public sealed class DocumentRequestCatalogTests
{
    [TestMethod]
    public void RequestableDocuments_ReturnsFixedRealDocumentNames()
    {
        var names = DocumentRequestCatalog.RequestableDocuments;

        CollectionAssert.Contains(names.ToList(), "Valid Government ID");
        CollectionAssert.Contains(names.ToList(), "Proof of Billing");
        CollectionAssert.Contains(names.ToList(), "Lease Agreement");
        Assert.IsTrue(names.All(name => !string.IsNullOrWhiteSpace(name)));
    }
}
