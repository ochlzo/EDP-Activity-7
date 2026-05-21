using edp_gui_admin;
using MySqlConnector;

namespace edp_gui_admin.Tests;

[TestClass]
public sealed class AdminTransactionServiceTests
{
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=site_management;User ID=root;Password=NewStrongPassword123!;SslMode=None;";

    [TestMethod]
    public async Task EnsureSchemaAsync_CreatesReportReadyTransactionTables()
    {
        var service = new AdminTransactionService(ConnectionString);

        await service.EnsureSchemaAsync();

        Assert.IsTrue(await TableExistsAsync("room_occupancy_transaction"));
        Assert.IsTrue(await TableExistsAsync("maintenance_ticket"));
        Assert.IsTrue(await TableExistsAsync("maintenance_ticket_status_history"));
        Assert.IsTrue(await TableExistsAsync("activity_log"));
        Assert.IsTrue(await ColumnExistsAsync("document", "submitted_at"));
        Assert.IsTrue(await ColumnExistsAsync("maintenance_ticket", "notes"));
    }

    [TestMethod]
    public async Task AssignTenantToRoomAsync_UpdatesRoomAndWritesTransaction()
    {
        var ids = await SeedRoomAndTenantAsync();
        var service = new AdminTransactionService(ConnectionString);
        var before = DateTime.Now;

        try
        {
            await service.EnsureSchemaAsync();

            await service.AssignTenantToRoomAsync(
                ids.RoomId!.Value,
                ids.TenantId!.Value,
                "admin",
                "Initial lease");

            var room = (await new AdminRecordService(ConnectionString).LoadRoomsAsync())
                .Single(row => row.RoomId == ids.RoomId.Value);
            var transactions = await service.LoadOccupancyTransactionsAsync();
            var transaction = transactions.Single(row => row.RoomId == ids.RoomId.Value);

            Assert.AreEqual(ids.TenantId.Value, room.TenantId);
            Assert.AreEqual("Assigned", transaction.TransactionType);
            Assert.AreEqual("Initial lease", transaction.Notes);
            Assert.IsTrue(transaction.Date >= before.AddSeconds(-1));
            Assert.IsTrue(transaction.Date <= DateTime.Now.AddSeconds(5));
        }
        finally
        {
            await DeleteSeedAsync(ids);
        }
    }

    [TestMethod]
    public async Task LogActivityAsync_CreatesReportReadyAuditRow()
    {
        var service = new AdminTransactionService(ConnectionString);
        await service.EnsureSchemaAsync();

        await service.LogActivityAsync("Admin", "admin", "Updated", "Room", 12, "Changed room name");

        var logs = await service.LoadActivityLogsAsync();
        var log = logs.First(row => row.EntityType == "Room" && row.EntityId == 12);

        Assert.AreEqual("Updated", log.Action);
        Assert.AreEqual("Changed room name", log.Description);
    }

    [TestMethod]
    public async Task LoadReportRowsAsync_ReturnsTransactionRowsForExport()
    {
        var ids = await SeedRoomAndTenantAsync();
        var service = new AdminTransactionService(ConnectionString);
        var marker = $"report-{Guid.NewGuid():N}";

        try
        {
            await service.AssignTenantToRoomAsync(
                ids.RoomId!.Value,
                ids.TenantId!.Value,
                "admin",
                marker);
            await service.LogActivityAsync("Admin", "admin", "Export Test", "Room", ids.RoomId.Value, marker);

            var rows = await service.LoadReportRowsAsync();

            Assert.IsTrue(rows.Any(row => row.Category == "Monthly Tenant Accommodations" && row.Notes == marker));
            Assert.IsTrue(rows.Any(row => row.Category == "Activity Log" && row.Notes == marker));
        }
        finally
        {
            await DeleteActivityByDescriptionAsync(marker);
            await DeleteSeedAsync(ids);
        }
    }

    private static async Task<bool> TableExistsAsync(string tableName)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
                AND table_name = @tableName;
            """;
        command.Parameters.AddWithValue("@tableName", tableName);

        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    private static async Task<bool> ColumnExistsAsync(string tableName, string columnName)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
                AND table_name = @tableName
                AND column_name = @columnName;
            """;
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", columnName);

        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    private static async Task<SeedIds> SeedRoomAndTenantAsync()
    {
        var records = new AdminRecordService(ConnectionString);
        var owner = await records.CreateOwnerAsync("Transaction Owner", $"txn_{Guid.NewGuid():N}@example.com", "Password123!");
        var site = await records.CreateSiteAsync("Transaction Site", owner.OwnerId);
        var riser = await records.CreateRiserAsync("Transaction Riser", site.SiteId);
        var room = await records.CreateRoomAsync("Transaction Room", riser.RiserId);
        var tenant = await records.CreateTenantAsync("Transaction Tenant");
        return new SeedIds(owner.OwnerId, site.SiteId, riser.RiserId, room.RoomId, tenant.TenantId, null);
    }

    private static async Task DeleteSeedAsync(SeedIds ids)
    {
        await DeleteByIdAsync("document", "document_id", ids.DocumentId);
        await DeleteByIdAsync("room", "room_id", ids.RoomId);
        await DeleteByIdAsync("riser", "riser_id", ids.RiserId);
        await DeleteByIdAsync("site", "site_id", ids.SiteId);
        await DeleteByIdAsync("tenant", "tenant_id", ids.TenantId);
        await DeleteByIdAsync("site_owner", "owner_id", ids.OwnerId);
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

    private static async Task DeleteActivityByDescriptionAsync(string description)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM activity_log WHERE description = @description;";
        command.Parameters.AddWithValue("@description", description);
        await command.ExecuteNonQueryAsync();
    }

    private sealed record SeedIds(
        int OwnerId,
        int? SiteId,
        int? RiserId,
        int? RoomId,
        int? TenantId,
        int? DocumentId);
}
