namespace edp_gui_app;

public sealed record OwnedRoom(
    int RoomId,
    string RoomName,
    bool IsOccupied,
    int? TenantId = null,
    string? TenantName = null,
    string? TenantEmail = null)
{
    public string Occupancy => IsOccupied ? "Occupied" : "Vacant";

    public string TenantDisplay => string.IsNullOrWhiteSpace(TenantName) ? "-" : TenantName;
}
