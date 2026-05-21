using MySqlConnector;

namespace edp_gui_app;

public sealed partial class SiteOwnerAuthService
{
    public async Task<OwnedTenant> LoadTenantDetailsAsync(
        int tenantId,
        int siteId,
        int ownerId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureTenantDetailsColumnsAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT tenant.tenant_id, tenant.tenant_name, tenant.tenant_email,
                   tenant.tenant_address, tenant.tenant_contact_number
            FROM tenant
            INNER JOIN room ON room.tenant_id = tenant.tenant_id
            INNER JOIN riser ON riser.riser_id = room.riser_id
            INNER JOIN site ON site.site_id = riser.site_id
            WHERE tenant.tenant_id = @tenantId
                AND site.site_id = @siteId
                AND site.owner_id = @ownerId;
            """;
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@siteId", siteId);
        command.Parameters.AddWithValue("@ownerId", ownerId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Tenant could not be found.");
        }

        return new OwnedTenant(
            reader.GetInt32("tenant_id"),
            GetNullableString(reader, "tenant_name"),
            GetNullableString(reader, "tenant_email"),
            GetNullableString(reader, "tenant_address"),
            GetNullableString(reader, "tenant_contact_number"));
    }

    public async Task<IReadOnlyList<OwnedDocumentAttachment>> LoadTenantDocumentsAsync(
        int tenantId,
        int siteId,
        int ownerId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureDocumentFilePathColumnAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT document.document_id, document.doc_name, document.doc_type,
                   document.doc_status, document.file_path
            FROM document
            INNER JOIN tenant ON tenant.tenant_id = document.tenant_id
            INNER JOIN room ON room.tenant_id = tenant.tenant_id
            INNER JOIN riser ON riser.riser_id = room.riser_id
            INNER JOIN site ON site.site_id = riser.site_id
            WHERE tenant.tenant_id = @tenantId
                AND site.site_id = @siteId
                AND site.owner_id = @ownerId
            ORDER BY document.doc_name, document.document_id;
            """;
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@siteId", siteId);
        command.Parameters.AddWithValue("@ownerId", ownerId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var documents = new List<OwnedDocumentAttachment>();
        while (await reader.ReadAsync(cancellationToken))
        {
            documents.Add(new OwnedDocumentAttachment(
                reader.GetInt32("document_id"),
                GetNullableString(reader, "doc_name"),
                GetNullableString(reader, "doc_type"),
                GetNullableString(reader, "doc_status"),
                GetNullableString(reader, "file_path")));
        }

        return documents;
    }

    public async Task UpdateTenantDetailsAsync(
        int tenantId,
        int siteId,
        int ownerId,
        string name,
        string email,
        string address,
        string contactNumber,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureTenantDetailsColumnsAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE tenant
            INNER JOIN room ON room.tenant_id = tenant.tenant_id
            INNER JOIN riser ON riser.riser_id = room.riser_id
            INNER JOIN site ON site.site_id = riser.site_id
            SET tenant.tenant_name = @name,
                tenant.tenant_email = @email,
                tenant.tenant_address = @address,
                tenant.tenant_contact_number = @contactNumber
            WHERE tenant.tenant_id = @tenantId
                AND site.site_id = @siteId
                AND site.owner_id = @ownerId;
            """;
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@address", address);
        command.Parameters.AddWithValue("@contactNumber", contactNumber);
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@siteId", siteId);
        command.Parameters.AddWithValue("@ownerId", ownerId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Tenant could not be updated.");
        }
    }

    public async Task CreateTenantDocumentAsync(
        int tenantId,
        string documentName,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureDocumentFilePathColumnAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE document
            SET doc_type = 'Uploaded',
                doc_status = 'Submitted',
                submitted_at = UTC_DATE(),
                file_path = @filePath
            WHERE tenant_id = @tenantId
                AND doc_name = @documentName
                AND doc_status = 'Pending Submission'
                AND COALESCE(file_path, '') = ''
            ORDER BY document_id DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@documentName", documentName);
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@filePath", filePath);

        if (await command.ExecuteNonQueryAsync(cancellationToken) > 0)
        {
            return;
        }

        await using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = """
            INSERT INTO document
                (doc_name, tenant_id, doc_type, doc_status, issued_at, submitted_at, file_path)
            VALUES
                (@documentName, @tenantId, 'Uploaded', 'Submitted', NULL, UTC_DATE(), @filePath);
            """;
        insertCommand.Parameters.AddWithValue("@documentName", documentName);
        insertCommand.Parameters.AddWithValue("@tenantId", tenantId);
        insertCommand.Parameters.AddWithValue("@filePath", filePath);

        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureTenantDetailsColumnsAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await AddColumnIfMissingAsync(connection, "tenant", "tenant_email", "VARCHAR(255) NOT NULL DEFAULT ''", cancellationToken);
        await AddColumnIfMissingAsync(connection, "tenant", "tenant_address", "VARCHAR(255) NOT NULL DEFAULT ''", cancellationToken);
        await AddColumnIfMissingAsync(connection, "tenant", "tenant_contact_number", "VARCHAR(80) NOT NULL DEFAULT ''", cancellationToken);
    }

    private static Task EnsureDocumentFilePathColumnAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        return EnsureDocumentRequestColumnsAsync(connection, cancellationToken);
    }

    private static async Task EnsureDocumentRequestColumnsAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await AddColumnIfMissingAsync(connection, "document", "doc_type", "VARCHAR(80) NOT NULL DEFAULT 'General'", cancellationToken);
        await AddColumnIfMissingAsync(connection, "document", "doc_status", "VARCHAR(40) NOT NULL DEFAULT 'Active'", cancellationToken);
        await AddColumnIfMissingAsync(connection, "document", "issued_at", "DATE NULL", cancellationToken);
        await AddColumnIfMissingAsync(connection, "document", "submitted_at", "DATE NULL", cancellationToken);
        await DropColumnIfExistsAsync(connection, "document", "expires_at", cancellationToken);
        await AddColumnIfMissingAsync(connection, "document", "file_path", "TEXT NULL", cancellationToken);
    }

    private static async Task AddColumnIfMissingAsync(
        MySqlConnection connection,
        string tableName,
        string columnName,
        string definition,
        CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(connection, tableName, columnName, cancellationToken))
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {definition};";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DropColumnIfExistsAsync(
        MySqlConnection connection,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        if (!await ColumnExistsAsync(connection, tableName, columnName, cancellationToken))
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER TABLE {tableName} DROP COLUMN {columnName};";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> ColumnExistsAsync(
        MySqlConnection connection,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
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
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private static string GetNullableString(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    }
}
