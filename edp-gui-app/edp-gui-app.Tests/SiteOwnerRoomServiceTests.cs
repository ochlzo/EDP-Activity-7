using edp_gui_app;
using MySqlConnector;

namespace edp_gui_app.Tests;

[TestClass]
public sealed class SiteOwnerRoomServiceTests
{
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=site_management;User ID=root;Password=NewStrongPassword123!;SslMode=None;";

    [TestMethod]
    public async Task LoadRoomsBySiteAsync_ReturnsOwnedSiteRooms_WithOccupancy()
    {
        var ownerOneEmail = BuildTestEmail();
        var ownerTwoEmail = BuildTestEmail();
        const string password = "SamplePassword123!";
        var ownerOneId = await InsertOwnerAsync(ownerOneEmail, password);
        var ownerTwoId = await InsertOwnerAsync(ownerTwoEmail, password);
        var tenantId = await InsertTenantAsync("Occupied Tenant");
        var siteIds = new List<int>();
        var riserIds = new List<int>();
        var roomIds = new List<int>();

        try
        {
            var siteId = await InsertSiteAsync(ownerOneId, "North Tower");
            var otherSiteId = await InsertSiteAsync(ownerTwoId, "Harbor Point");
            siteIds.Add(siteId);
            siteIds.Add(otherSiteId);

            var riserId = await InsertRiserAsync(siteId, "Riser A");
            var otherRiserId = await InsertRiserAsync(otherSiteId, "Riser B");
            riserIds.Add(riserId);
            riserIds.Add(otherRiserId);

            roomIds.Add(await InsertRoomAsync(riserId, "Zulu Room"));
            roomIds.Add(await InsertRoomAsync(riserId, "Alpha Room", tenantId));
            roomIds.Add(await InsertRoomAsync(otherRiserId, "Other Owner Room"));

            var service = new SiteOwnerAuthService(ConnectionString);

            var rooms = await service.LoadRoomsBySiteAsync(siteId, ownerOneId);

            CollectionAssert.AreEqual(
                new[] { "Alpha Room", "Zulu Room" },
                rooms.Select(room => room.RoomName).ToArray());
            CollectionAssert.AreEqual(
                new[] { "Occupied", "Vacant" },
                rooms.Select(room => room.Occupancy).ToArray());
        }
        finally
        {
            await DeleteRoomsAsync(roomIds);
            await DeleteRisersAsync(riserIds);
            await DeleteSitesAsync(siteIds);
            await DeleteTenantAsync(tenantId);
            await DeleteOwnerAsync(ownerOneEmail);
            await DeleteOwnerAsync(ownerTwoEmail);
        }
    }

    [TestMethod]
    public async Task UpdateRoomAsync_UpdatesOnlyOwnedRoomName()
    {
        var ownerOneEmail = BuildTestEmail();
        var ownerTwoEmail = BuildTestEmail();
        const string password = "SamplePassword123!";
        var ownerOneId = await InsertOwnerAsync(ownerOneEmail, password);
        var ownerTwoId = await InsertOwnerAsync(ownerTwoEmail, password);
        var siteIds = new List<int>();
        var riserIds = new List<int>();
        var roomIds = new List<int>();

        try
        {
            var siteId = await InsertSiteAsync(ownerOneId, "North Tower");
            var otherSiteId = await InsertSiteAsync(ownerTwoId, "Harbor Point");
            siteIds.Add(siteId);
            siteIds.Add(otherSiteId);

            var riserId = await InsertRiserAsync(siteId, "Riser A");
            var otherRiserId = await InsertRiserAsync(otherSiteId, "Riser B");
            riserIds.Add(riserId);
            riserIds.Add(otherRiserId);

            var editableRoomId = await InsertRoomAsync(riserId, "Old Name");
            roomIds.Add(editableRoomId);
            roomIds.Add(await InsertRoomAsync(otherRiserId, "Other Owner Room"));

            var service = new SiteOwnerAuthService(ConnectionString);

            await service.UpdateRoomAsync(editableRoomId, siteId, ownerOneId, "Updated Name");
            var rooms = await service.LoadRoomsBySiteAsync(siteId, ownerOneId);

            CollectionAssert.AreEqual(new[] { "Updated Name" }, rooms.Select(room => room.RoomName).ToArray());
        }
        finally
        {
            await DeleteRoomsAsync(roomIds);
            await DeleteRisersAsync(riserIds);
            await DeleteSitesAsync(siteIds);
            await DeleteOwnerAsync(ownerOneEmail);
            await DeleteOwnerAsync(ownerTwoEmail);
        }
    }

    [TestMethod]
    public async Task DeleteRoomAsync_DeletesOnlyOwnedRoom()
    {
        var ownerOneEmail = BuildTestEmail();
        var ownerTwoEmail = BuildTestEmail();
        const string password = "SamplePassword123!";
        var ownerOneId = await InsertOwnerAsync(ownerOneEmail, password);
        var ownerTwoId = await InsertOwnerAsync(ownerTwoEmail, password);
        var siteIds = new List<int>();
        var riserIds = new List<int>();
        var roomIds = new List<int>();

        try
        {
            var siteId = await InsertSiteAsync(ownerOneId, "North Tower");
            var otherSiteId = await InsertSiteAsync(ownerTwoId, "Harbor Point");
            siteIds.Add(siteId);
            siteIds.Add(otherSiteId);

            var riserId = await InsertRiserAsync(siteId, "Riser A");
            var otherRiserId = await InsertRiserAsync(otherSiteId, "Riser B");
            riserIds.Add(riserId);
            riserIds.Add(otherRiserId);

            var deletableRoomId = await InsertRoomAsync(riserId, "Delete Me");
            roomIds.Add(deletableRoomId);
            var remainingRoomId = await InsertRoomAsync(otherRiserId, "Keep Me");
            roomIds.Add(remainingRoomId);

            var service = new SiteOwnerAuthService(ConnectionString);

            await service.DeleteRoomAsync(deletableRoomId, siteId, ownerOneId);
            var ownerOneRooms = await service.LoadRoomsBySiteAsync(siteId, ownerOneId);
            var ownerTwoRooms = await service.LoadRoomsBySiteAsync(otherSiteId, ownerTwoId);

            Assert.IsEmpty(ownerOneRooms);
            CollectionAssert.AreEqual(new[] { remainingRoomId }, ownerTwoRooms.Select(room => room.RoomId).ToArray());
        }
        finally
        {
            await DeleteRoomsAsync(roomIds);
            await DeleteRisersAsync(riserIds);
            await DeleteSitesAsync(siteIds);
            await DeleteOwnerAsync(ownerOneEmail);
            await DeleteOwnerAsync(ownerTwoEmail);
        }
    }

    private static string BuildTestEmail() => $"codex_{Guid.NewGuid():N}@example.com";

    private static async Task<int> InsertOwnerAsync(string email, string password)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO site_owner (owner_name, owner_email, password)
            VALUES (@name, @email, @password);
            """;
        command.Parameters.AddWithValue("@name", "Codex Test Owner");
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@password", password);

        await command.ExecuteNonQueryAsync();
        return Convert.ToInt32(command.LastInsertedId);
    }

    private static async Task<int> InsertSiteAsync(int ownerId, string siteName)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO site (site_name, owner_id)
            VALUES (@siteName, @ownerId);
            """;
        command.Parameters.AddWithValue("@siteName", siteName);
        command.Parameters.AddWithValue("@ownerId", ownerId);

        await command.ExecuteNonQueryAsync();
        return Convert.ToInt32(command.LastInsertedId);
    }

    private static async Task<int> InsertRiserAsync(int siteId, string riserName)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO riser (riser_name, site_id)
            VALUES (@riserName, @siteId);
            """;
        command.Parameters.AddWithValue("@riserName", riserName);
        command.Parameters.AddWithValue("@siteId", siteId);

        await command.ExecuteNonQueryAsync();
        return Convert.ToInt32(command.LastInsertedId);
    }

    private static async Task<int> InsertTenantAsync(string tenantName)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO tenant (tenant_name) VALUES (@tenantName);";
        command.Parameters.AddWithValue("@tenantName", tenantName);

        await command.ExecuteNonQueryAsync();
        return Convert.ToInt32(command.LastInsertedId);
    }

    private static async Task<int> InsertRoomAsync(int riserId, string roomName, int? tenantId = null)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO room (room_name, riser_id, tenant_id)
            VALUES (@roomName, @riserId, @tenantId);
            """;
        command.Parameters.AddWithValue("@roomName", roomName);
        command.Parameters.AddWithValue("@riserId", riserId);
        command.Parameters.AddWithValue("@tenantId", tenantId ?? (object)DBNull.Value);

        await command.ExecuteNonQueryAsync();
        return Convert.ToInt32(command.LastInsertedId);
    }

    private static Task DeleteRoomsAsync(IEnumerable<int> roomIds) => DeleteByIdsAsync("room", "room_id", roomIds);

    private static Task DeleteRisersAsync(IEnumerable<int> riserIds) => DeleteByIdsAsync("riser", "riser_id", riserIds);

    private static Task DeleteSitesAsync(IEnumerable<int> siteIds) => DeleteByIdsAsync("site", "site_id", siteIds);

    private static async Task DeleteTenantAsync(int tenantId)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM tenant WHERE tenant_id = @tenantId;";
        command.Parameters.AddWithValue("@tenantId", tenantId);

        await command.ExecuteNonQueryAsync();
    }

    private static async Task DeleteOwnerAsync(string email)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM site_owner WHERE owner_email = @email;";
        command.Parameters.AddWithValue("@email", email);

        await command.ExecuteNonQueryAsync();
    }

    private static async Task DeleteByIdsAsync(string tableName, string columnName, IEnumerable<int> ids)
    {
        var values = ids.ToArray();
        if (values.Length == 0)
        {
            return;
        }

        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {tableName} WHERE {columnName} IN ({string.Join(", ", values)});";

        await command.ExecuteNonQueryAsync();
    }
}
