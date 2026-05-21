namespace edp_gui_app;

public sealed record DocumentAttachmentRow(string Name, string Status, string FilePath)
{
    public bool CanCopyPath => !string.IsNullOrWhiteSpace(FilePath);

    public static DocumentAttachmentRow FromDocument(OwnedDocumentAttachment document)
    {
        return new DocumentAttachmentRow(document.Name, document.Status, document.FilePath);
    }
}
