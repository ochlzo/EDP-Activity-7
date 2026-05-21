namespace edp_gui_app;

public sealed record DocumentRequestEmailMessage(
    string ToEmail,
    string TenantName,
    string RequestUrl,
    IReadOnlyList<string> RequestedDocuments);
