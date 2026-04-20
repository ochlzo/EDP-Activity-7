using edp_gui_app;

namespace edp_gui_app.Tests;

[TestClass]
public sealed class OwnedRoomSortTests
{
    private static readonly IReadOnlyList<OwnedRoom> Rooms =
    [
        new OwnedRoom(12, "Zulu Room", false),
        new OwnedRoom(34, "alpha room", true),
        new OwnedRoom(56, "Harbor Room", false)
    ];

    [TestMethod]
    public void Apply_ReturnsAscendingRooms_WhenSortOrderIsAscending()
    {
        var sorted = OwnedRoomSort.Apply(Rooms, RoomNameSortOrder.Ascending);

        CollectionAssert.AreEqual(
            new[] { "alpha room", "Harbor Room", "Zulu Room" },
            sorted.Select(room => room.RoomName).ToArray());
    }

    [TestMethod]
    public void Apply_ReturnsDescendingRooms_WhenSortOrderIsDescending()
    {
        var sorted = OwnedRoomSort.Apply(Rooms, RoomNameSortOrder.Descending);

        CollectionAssert.AreEqual(
            new[] { "Zulu Room", "Harbor Room", "alpha room" },
            sorted.Select(room => room.RoomName).ToArray());
    }
}
