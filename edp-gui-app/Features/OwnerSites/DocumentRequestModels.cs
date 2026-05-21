namespace edp_gui_app;

public sealed record DocumentRequestState(
    string Token,
    int TenantId,
    string TenantName,
    IReadOnlyList<string> RequestedDocuments);
