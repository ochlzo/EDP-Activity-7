namespace edp_gui_app;

public sealed record OwnerMaintenanceRequest(
    int TicketId,
    string SiteName,
    string Title,
    string Description,
    string Priority,
    string Status,
    DateTime RequestedAt,
    DateTime? ResolvedAt,
    string Notes);
