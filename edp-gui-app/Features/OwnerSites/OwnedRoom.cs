namespace edp_gui_app;

public sealed record OwnedRoom(int RoomId, string RoomName, bool IsOccupied)
{
    public string Occupancy => IsOccupied ? "Occupied" : "Vacant";
}
