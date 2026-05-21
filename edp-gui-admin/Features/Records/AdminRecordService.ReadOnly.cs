using MySqlConnector;

namespace edp_gui_admin;

public sealed partial class AdminRecordService
{
    public async Task<IReadOnlyList<AdminTenant>> LoadTenantsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureTenantDetailsColumnsAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT tenant_id, tenant_name, tenant_email, tenant_address, tenant_contact_number
            FROM tenant
            ORDER BY tenant_name, tenant_id;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var tenants = new List<AdminTenant>();
        while (await reader.ReadAsync(cancellationToken))
        {
            tenants.Add(new AdminTenant(
                reader.GetInt32("tenant_id"),
                GetNullableString(reader, "tenant_name"),
                GetNullableString(reader, "tenant_email"),
                GetNullableString(reader, "tenant_address"),
                GetNullableString(reader, "tenant_contact_number")));
        }

        return tenants;
    }

    public async Task<AdminTenant> CreateTenantAsync(
        string tenantName,
        string tenantEmail = "",
        string tenantAddress = "",
        string tenantContactNumber = "",
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureTenantDetailsColumnsAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO tenant (tenant_name, tenant_email, tenant_address, tenant_contact_number)
            VALUES (@tenantName, @tenantEmail, @tenantAddress, @tenantContactNumber);
            """;
        command.Parameters.AddWithValue("@tenantName", tenantName);
        command.Parameters.AddWithValue("@tenantEmail", tenantEmail);
        command.Parameters.AddWithValue("@tenantAddress", tenantAddress);
        command.Parameters.AddWithValue("@tenantContactNumber", tenantContactNumber);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var tenant = new AdminTenant(
            Convert.ToInt32(command.LastInsertedId),
            tenantName,
            tenantEmail,
            tenantAddress,
            tenantContactNumber);
        await LogAdminActivityAsync(
            "Created",
            "Tenant",
            tenant.TenantId,
            $"Created tenant {tenant.TenantName}.",
            cancellationToken);
        return tenant;
    }

    public async Task UpdateTenantAsync(
        int tenantId,
        string tenantName,
        string tenantEmail = "",
        string tenantAddress = "",
        string tenantContactNumber = "",
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureTenantDetailsColumnsAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE tenant
            SET tenant_name = @tenantName,
                tenant_email = @tenantEmail,
                tenant_address = @tenantAddress,
                tenant_contact_number = @tenantContactNumber
            WHERE tenant_id = @tenantId;
            """;
        command.Parameters.AddWithValue("@tenantName", tenantName);
        command.Parameters.AddWithValue("@tenantEmail", tenantEmail);
        command.Parameters.AddWithValue("@tenantAddress", tenantAddress);
        command.Parameters.AddWithValue("@tenantContactNumber", tenantContactNumber);
        command.Parameters.AddWithValue("@tenantId", tenantId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Tenant could not be updated.");
        }

        await LogAdminActivityAsync(
            "Updated",
            "Tenant",
            tenantId,
            $"Updated tenant {tenantName}.",
            cancellationToken);
    }

    public async Task DeleteTenantAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM tenant WHERE tenant_id = @tenantId;";
        command.Parameters.AddWithValue("@tenantId", tenantId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Tenant could not be deleted.");
        }

        await LogAdminActivityAsync(
            "Deleted",
            "Tenant",
            tenantId,
            $"Deleted tenant {tenantId}.",
            cancellationToken);
    }

    public async Task<IReadOnlyList<AdminDocument>> LoadDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureDocumentComplianceColumnsAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT document.document_id, document.doc_name, document.tenant_id, tenant.tenant_name,
                   document.doc_type, document.doc_status, document.issued_at,
                   document.submitted_at, document.notes
            FROM document
            INNER JOIN tenant ON tenant.tenant_id = document.tenant_id
            ORDER BY document.doc_name, document.document_id;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var documents = new List<AdminDocument>();
        while (await reader.ReadAsync(cancellationToken))
        {
            documents.Add(new AdminDocument(
                reader.GetInt32("document_id"),
                GetNullableString(reader, "doc_name"),
                reader.GetInt32("tenant_id"),
                GetNullableString(reader, "tenant_name"),
                GetNullableString(reader, "doc_type"),
                GetNullableString(reader, "doc_status"),
                GetNullableDate(reader, "issued_at"),
                GetNullableDate(reader, "submitted_at"),
                GetNullableString(reader, "notes")));
        }

        return documents;
    }

    public async Task<AdminDocument> CreateDocumentAsync(
        string documentName,
        int tenantId,
        string documentType = "General",
        string documentStatus = "Active",
        string notes = "",
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureDocumentComplianceColumnsAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO document
                (doc_name, tenant_id, doc_type, doc_status, issued_at, submitted_at, notes)
            VALUES
                (@documentName, @tenantId, @documentType, @documentStatus, UTC_DATE(), NULL, @notes);
            """;
        command.Parameters.AddWithValue("@documentName", documentName);
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@documentType", documentType);
        command.Parameters.AddWithValue("@documentStatus", documentStatus);
        command.Parameters.AddWithValue("@notes", notes);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var document = new AdminDocument(
            Convert.ToInt32(command.LastInsertedId),
            documentName,
            tenantId,
            await LoadDocumentTenantNameAsync(connection, tenantId, cancellationToken),
            documentType,
            documentStatus,
            DateTime.UtcNow.Date,
            null,
            notes);
        await LogAdminActivityAsync(
            "Created",
            "Document",
            document.DocumentId,
            $"Created document {document.DocumentName}.",
            cancellationToken);
        return document;
    }

    public async Task UpdateDocumentAsync(
        int documentId,
        string documentName,
        int tenantId,
        string documentType = "General",
        string documentStatus = "Active",
        string notes = "",
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureDocumentComplianceColumnsAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE document
            SET doc_name = @documentName,
                tenant_id = @tenantId,
                doc_type = @documentType,
                doc_status = @documentStatus,
                notes = @notes,
                updated_at = UTC_TIMESTAMP()
            WHERE document_id = @documentId;
            """;
        command.Parameters.AddWithValue("@documentName", documentName);
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@documentType", documentType);
        command.Parameters.AddWithValue("@documentStatus", documentStatus);
        command.Parameters.AddWithValue("@notes", notes);
        command.Parameters.AddWithValue("@documentId", documentId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Document could not be updated.");
        }

        await LogAdminActivityAsync(
            "Updated",
            "Document",
            documentId,
            $"Updated document {documentName}.",
            cancellationToken);
    }

    public async Task DeleteDocumentAsync(int documentId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM document WHERE document_id = @documentId;";
        command.Parameters.AddWithValue("@documentId", documentId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Document could not be deleted.");
        }

        await LogAdminActivityAsync(
            "Deleted",
            "Document",
            documentId,
            $"Deleted document {documentId}.",
            cancellationToken);
    }

    private static async Task<string> LoadDocumentTenantNameAsync(
        MySqlConnection connection,
        int tenantId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT tenant_name FROM tenant WHERE tenant_id = @tenantId;";
        command.Parameters.AddWithValue("@tenantId", tenantId);
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken)) ?? string.Empty;
    }

    private static DateTime? GetNullableDate(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal).Date;
    }
}
