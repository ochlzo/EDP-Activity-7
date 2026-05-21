namespace edp_gui_app;

public sealed record OwnedTenant(
    int TenantId,
    string Name,
    string Email,
    string Address,
    string ContactNumber);
