namespace edp_gui_app;

public enum RoomNameSortOrder
{
    Ascending,
    Descending
}

public static class OwnedRoomSort
{
    public static IReadOnlyList<OwnedRoom> Apply(IEnumerable<OwnedRoom> rooms, RoomNameSortOrder sortOrder)
    {
        var orderedRooms = sortOrder == RoomNameSortOrder.Descending
            ? rooms.OrderByDescending(room => room.RoomName, StringComparer.OrdinalIgnoreCase).ThenBy(room => room.RoomId)
            : rooms.OrderBy(room => room.RoomName, StringComparer.OrdinalIgnoreCase).ThenBy(room => room.RoomId);

        return orderedRooms.ToArray();
    }
}
