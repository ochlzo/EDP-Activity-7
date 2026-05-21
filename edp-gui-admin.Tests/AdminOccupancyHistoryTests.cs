using edp_gui_admin;
using MySqlConnector;

namespace edp_gui_admin.Tests;

[TestClass]
public sealed class AdminOccupancyHistoryTests
{
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=site_management;User ID=root;Password=NewStrongPassword123!;SslMode=None;";

    [TestMethod]
    public async Task LoadOccupancyHistoryAsync_ReturnsHistoryRowsForTheRequestedRoom()
    {
        var ids = await SeedRoomAndTenantAsync();
        var service = new AdminTransactionService(ConnectionString);

        try
        {
            await service.EnsureSchemaAsync();

            await service.AssignTenantToRoomAsync(
                ids.RoomId!.Value,
                ids.InitialTenantId,
                "admin",
                "Initial assignment");

            var replacementTenant = await SeedTenantAsync("Replacement Tenant");
            ids.TenantIds.Add(replacementTenant);
            await UpdateRoomTenantAsync(ids.RoomId!.Value, replacementTenant);
            await InsertOccupancyHistoryRowAsync(
                ids.RoomId!.Value,
                replacementTenant,
                "Replaced",
                "Replacement assignment");

            var history = await service.LoadOccupancyHistoryAsync(ids.RoomId.Value);

            Assert.AreEqual(2, history.Count);
            Assert.IsTrue(history.All(row => row.RoomId == ids.RoomId.Value));

            var initial = history.Single(row => row.Notes == "Initial assignment");
            var replacement = history.Single(row => row.Notes == "Replacement assignment");

            Assert.AreEqual("Initial Tenant", initial.TenantName);
            Assert.AreEqual("Replacement Tenant", replacement.TenantName);
            Assert.AreEqual(ids.RoomId.Value, initial.RoomId);
            Assert.AreEqual(ids.RoomId.Value, replacement.RoomId);
        }
        finally
        {
            await DeleteTenantAsync(ids.TenantIds);
            await DeleteSeedAsync(ids);
        }
    }

    private static async Task<SeedIds> SeedRoomAndTenantAsync()
    {
        var records = new AdminRecordService(ConnectionString);
        var owner = await records.CreateOwnerAsync("Occupancy Owner", $"occupancy_{Guid.NewGuid():N}@example.com", "Password123!");
        var site = await records.CreateSiteAsync("Occupancy Site", owner.OwnerId);
        var riser = await records.CreateRiserAsync("Occupancy Riser", site.SiteId);
        var room = await records.CreateRoomAsync("Occupancy Room", riser.RiserId);
        var tenant = await records.CreateTenantAsync("Initial Tenant");
        return new SeedIds(owner.OwnerId, site.SiteId, riser.RiserId, room.RoomId, tenant.TenantId);
    }

    private static async Task<int> SeedTenantAsync(string tenantName)
    {
        var records = new AdminRecordService(ConnectionString);
        return (await records.CreateTenantAsync(tenantName)).TenantId;
    }

    private static async Task DeleteSeedAsync(SeedIds ids)
    {
        await DeleteByIdAsync("room", "room_id", ids.RoomId);
        await DeleteByIdAsync("riser", "riser_id", ids.RiserId);
        await DeleteByIdAsync("site", "site_id", ids.SiteId);
        await DeleteByIdAsync("site_owner", "owner_id", ids.OwnerId);
    }

    private static async Task UpdateRoomTenantAsync(int roomId, int tenantId)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using (var clearCommand = connection.CreateCommand())
        {
            clearCommand.CommandText = "UPDATE room SET tenant_id = NULL WHERE room_id = @roomId;";
            clearCommand.Parameters.AddWithValue("@roomId", roomId);
            await clearCommand.ExecuteNonQueryAsync();
        }

        await using (var updateCommand = connection.CreateCommand())
        {
            updateCommand.CommandText = "UPDATE room SET tenant_id = @tenantId WHERE room_id = @roomId;";
            updateCommand.Parameters.AddWithValue("@tenantId", tenantId);
            updateCommand.Parameters.AddWithValue("@roomId", roomId);
            await updateCommand.ExecuteNonQueryAsync();
        }
    }

    private static async Task InsertOccupancyHistoryRowAsync(
        int roomId,
        int tenantId,
        string transactionType,
        string notes)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO room_occupancy_transaction
                (room_id, tenant_id, transaction_type, effective_at, notes, created_by)
            VALUES
                (@roomId, @tenantId, @transactionType, @effectiveAt, @notes, 'admin');
            """;
        command.Parameters.AddWithValue("@roomId", roomId);
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@transactionType", transactionType);
        command.Parameters.AddWithValue("@effectiveAt", DateTime.Now);
        command.Parameters.AddWithValue("@notes", notes);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DeleteTenantAsync(IEnumerable<int> tenantIds)
    {
        var values = tenantIds.Distinct().ToArray();
        foreach (var tenantId in values)
        {
            await DeleteByIdAsync("tenant", "tenant_id", tenantId);
        }
    }

    private static async Task DeleteByIdAsync(string tableName, string keyName, int? id)
    {
        if (id is null)
        {
            return;
        }

        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {tableName} WHERE {keyName} = @id;";
        command.Parameters.AddWithValue("@id", id.Value);
        await command.ExecuteNonQueryAsync();
    }

    private sealed record SeedIds(
        int OwnerId,
        int? SiteId,
        int? RiserId,
        int? RoomId,
        int InitialTenantId)
    {
        public List<int> TenantIds { get; } = [InitialTenantId];
    }
}
