using edp_gui_app;

namespace edp_gui_app.Tests;

[TestClass]
public sealed class DocumentUploadStorageTests
{
    [TestMethod]
    public void SanitizeFileName_RemovesPathSeparators()
    {
        var fileName = DocumentUploadStorage.SanitizeFileName(@"..\tenant/docs\valid:id.pdf");

        Assert.AreEqual("tenant_docs_valid_id.pdf", fileName);
    }

    [TestMethod]
    public void BuildUploadPath_UsesTenantAndTokenFolder()
    {
        var storage = new DocumentUploadStorage(@"C:\Temp\Requests");

        var path = storage.BuildUploadPath(42, "abc123", "../id.pdf");

        Assert.AreEqual(
            Path.Combine(@"C:\Temp\Requests", "42", "abc123", "id.pdf"),
            path);
    }
}
