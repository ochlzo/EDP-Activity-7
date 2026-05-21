namespace edp_gui_app;

public sealed record OwnedDocumentAttachment(
    int DocumentId,
    string Name,
    string Type,
    string Status,
    string FilePath);
