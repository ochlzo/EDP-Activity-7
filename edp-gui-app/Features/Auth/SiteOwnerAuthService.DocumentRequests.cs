namespace edp_gui_app;

public sealed partial class SiteOwnerAuthService
{
    public async Task CreatePendingTenantDocumentsAsync(
        int tenantId,
        int siteId,
        int ownerId,
        IReadOnlyList<string> requestedDocuments,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnector.MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureDocumentFilePathColumnAsync(connection, cancellationToken);
        await EnsureTenantDetailsColumnsAsync(connection, cancellationToken);

        foreach (var document in requestedDocuments.Where(document => !string.IsNullOrWhiteSpace(document)))
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO document
                    (doc_name, tenant_id, doc_type, doc_status, issued_at, submitted_at, file_path)
                SELECT @documentName, tenant.tenant_id, 'Requested', 'Pending Submission', UTC_DATE(), NULL, ''
                FROM tenant
                INNER JOIN room ON room.tenant_id = tenant.tenant_id
                INNER JOIN riser ON riser.riser_id = room.riser_id
                INNER JOIN site ON site.site_id = riser.site_id
                WHERE tenant.tenant_id = @tenantId
                    AND site.site_id = @siteId
                    AND site.owner_id = @ownerId
                    AND NOT EXISTS (
                        SELECT 1
                        FROM document existing
                        WHERE existing.tenant_id = tenant.tenant_id
                            AND existing.doc_name = @documentName
                            AND existing.doc_status = 'Pending Submission'
                            AND COALESCE(existing.file_path, '') = ''
                    );
                """;
            command.Parameters.AddWithValue("@documentName", document);
            command.Parameters.AddWithValue("@tenantId", tenantId);
            command.Parameters.AddWithValue("@siteId", siteId);
            command.Parameters.AddWithValue("@ownerId", ownerId);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task SendDocumentRequestEmailAsync(
        OwnedTenant tenant,
        string requestUrl,
        IReadOnlyList<string> requestedDocuments,
        CancellationToken cancellationToken = default)
    {
        if (_emailSender is null)
        {
            throw new InvalidOperationException("Gmail email is not configured.");
        }

        await _emailSender.SendDocumentRequestAsync(
            new DocumentRequestEmailMessage(
                tenant.Email,
                tenant.Name,
                requestUrl,
                requestedDocuments),
            cancellationToken);
    }
}
