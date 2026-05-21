using edp_gui_app;
using MySqlConnector;

namespace edp_gui_app.Tests;

[TestClass]
public sealed class TenantDocumentRequestServiceTests
{
    private const string ConnectionString =
        "Server=127.0.0.1;Port=3306;Database=site_management;User ID=root;Password=NewStrongPassword123!;SslMode=None;";

    [TestMethod]
    public async Task CreatePendingTenantDocumentsAsync_CreatesPendingSubmissionRows()
    {
        var ids = await CreateOccupiedRoomAsync();
        var today = DateTime.UtcNow.Date;
        try
        {
            var service = new SiteOwnerAuthService(ConnectionString);

            await service.CreatePendingTenantDocumentsAsync(
                ids.TenantId,
                ids.SiteId,
                ids.OwnerId,
                ["Valid Government ID", "Proof of Billing"]);

            var documents = await service.LoadTenantDocumentsAsync(ids.TenantId, ids.SiteId, ids.OwnerId);
            Assert.HasCount(2, documents);
            Assert.IsTrue(documents.All(document => document.Status == "Pending Submission"));
            Assert.IsTrue(documents.All(document => string.IsNullOrWhiteSpace(document.FilePath)));

            var timestamps = await LoadDocumentTimestampsAsync(ids.TenantId);
            Assert.IsTrue(timestamps.All(timestamp => timestamp.IssuedAt?.Date == today));
            Assert.IsTrue(timestamps.All(timestamp => timestamp.SubmittedAt is null));
        }
        finally
        {
            await DeleteCreatedDataAsync(ids);
        }
    }

    [TestMethod]
    public async Task CreateTenantDocumentAsync_UpdatesMatchingPendingSubmission()
    {
        var ids = await CreateOccupiedRoomAsync();
        var today = DateTime.UtcNow.Date;
        try
        {
            var service = new SiteOwnerAuthService(ConnectionString);
            await service.CreatePendingTenantDocumentsAsync(
                ids.TenantId,
                ids.SiteId,
                ids.OwnerId,
                ["Valid Government ID"]);

            await service.CreateTenantDocumentAsync(
                ids.TenantId,
                "Valid Government ID",
                @"C:\Temp\valid-id.pdf");

            var documents = await service.LoadTenantDocumentsAsync(ids.TenantId, ids.SiteId, ids.OwnerId);
            Assert.HasCount(1, documents);
            Assert.AreEqual("Submitted", documents[0].Status);
            Assert.AreEqual(@"C:\Temp\valid-id.pdf", documents[0].FilePath);

            var timestamp = (await LoadDocumentTimestampsAsync(ids.TenantId)).Single();
            Assert.AreEqual(today, timestamp.IssuedAt?.Date);
            Assert.AreEqual(today, timestamp.SubmittedAt?.Date);
        }
        finally
        {
            await DeleteCreatedDataAsync(ids);
        }
    }

    private static async Task<TestIds> CreateOccupiedRoomAsync()
    {
        var ownerEmail = $"codex_doc_{Guid.NewGuid():N}@example.com";
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        var ownerId = await InsertAsync(connection, """
            INSERT INTO site_owner (owner_name, owner_email, password)
            VALUES ('Codex Owner', @value, 'password');
            """, ownerEmail);
        var siteId = await InsertAsync(connection, """
            INSERT INTO site (site_name, owner_id)
            VALUES ('Document Site', @value);
            """, ownerId);
        var riserId = await InsertAsync(connection, """
            INSERT INTO riser (riser_name, site_id)
            VALUES ('Riser A', @value);
            """, siteId);
        var tenantId = await InsertAsync(connection, """
            INSERT INTO tenant (tenant_name)
            VALUES ('Acme Tenant');
            """, DBNull.Value);
        var roomId = await InsertAsync(connection, """
            INSERT INTO room (room_name, riser_id, tenant_id)
            VALUES ('Room 101', @value, @tenantId);
            """, riserId, tenantId);

        return new TestIds(ownerEmail, ownerId, siteId, riserId, roomId, tenantId);
    }

    private static async Task<int> InsertAsync(
        MySqlConnection connection,
        string commandText,
        object value,
        int? tenantId = null)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.Parameters.AddWithValue("@value", value);
        command.Parameters.AddWithValue("@tenantId", tenantId ?? (object)DBNull.Value);
        await command.ExecuteNonQueryAsync();
        return Convert.ToInt32(command.LastInsertedId);
    }

    private static async Task DeleteCreatedDataAsync(TestIds ids)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, "DELETE FROM document WHERE tenant_id = @id;", ids.TenantId);
        await ExecuteAsync(connection, "DELETE FROM room WHERE room_id = @id;", ids.RoomId);
        await ExecuteAsync(connection, "DELETE FROM tenant WHERE tenant_id = @id;", ids.TenantId);
        await ExecuteAsync(connection, "DELETE FROM riser WHERE riser_id = @id;", ids.RiserId);
        await ExecuteAsync(connection, "DELETE FROM site WHERE site_id = @id;", ids.SiteId);
        await ExecuteAsync(connection, "DELETE FROM site_owner WHERE owner_email = @email;", ids.OwnerEmail);
    }

    private static async Task ExecuteAsync(MySqlConnection connection, string commandText, object value)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.Parameters.AddWithValue("@id", value);
        command.Parameters.AddWithValue("@email", value);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<IReadOnlyList<(DateTime? IssuedAt, DateTime? SubmittedAt)>> LoadDocumentTimestampsAsync(int tenantId)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT issued_at, submitted_at
            FROM document
            WHERE tenant_id = @tenantId
            ORDER BY document_id;
            """;
        command.Parameters.AddWithValue("@tenantId", tenantId);

        await using var reader = await command.ExecuteReaderAsync();
        var timestamps = new List<(DateTime? IssuedAt, DateTime? SubmittedAt)>();
        while (await reader.ReadAsync())
        {
            timestamps.Add((
                reader.IsDBNull(reader.GetOrdinal("issued_at")) ? null : reader.GetDateTime("issued_at"),
                reader.IsDBNull(reader.GetOrdinal("submitted_at")) ? null : reader.GetDateTime("submitted_at")));
        }

        return timestamps;
    }

    private sealed record TestIds(
        string OwnerEmail,
        int OwnerId,
        int SiteId,
        int RiserId,
        int RoomId,
        int TenantId);
}
