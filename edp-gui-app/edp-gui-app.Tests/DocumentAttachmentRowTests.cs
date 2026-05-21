using edp_gui_app;

namespace edp_gui_app.Tests;

[TestClass]
public sealed class DocumentAttachmentRowTests
{
    [TestMethod]
    public void FromDocument_DisablesCopyWhenFilePathIsMissing()
    {
        var row = DocumentAttachmentRow.FromDocument(new OwnedDocumentAttachment(
            1,
            "Valid Government ID",
            "Uploaded",
            "Submitted",
            string.Empty));

        Assert.AreEqual("Valid Government ID", row.Name);
        Assert.AreEqual("Submitted", row.Status);
        Assert.IsFalse(row.CanCopyPath);
    }

    [TestMethod]
    public void FromDocument_EnablesCopyWhenFilePathExists()
    {
        var row = DocumentAttachmentRow.FromDocument(new OwnedDocumentAttachment(
            1,
            "Valid Government ID",
            "Uploaded",
            "Submitted",
            @"C:\Temp\id.pdf"));

        Assert.AreEqual(@"C:\Temp\id.pdf", row.FilePath);
        Assert.IsTrue(row.CanCopyPath);
    }
}
