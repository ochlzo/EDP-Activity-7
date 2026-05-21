namespace edp_gui_admin;

public sealed partial class AdminTransactionService
{
    public async Task<IReadOnlyList<AdminReportRow>> LoadReportRowsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT 'Monthly Tenant Accommodations' AS category, room.room_name AS parent_name,
                   COALESCE(tenant.tenant_name, '') AS item_name,
                   tx.transaction_type AS status, tx.created_at AS `Date`, tx.notes
            FROM room_occupancy_transaction tx
            INNER JOIN room ON room.room_id = tx.room_id
            LEFT JOIN tenant ON tenant.tenant_id = tx.tenant_id
            WHERE tx.transaction_type IN ('Assigned', 'Transferred')
                AND YEAR(tx.created_at) = YEAR(CURRENT_DATE())
            UNION ALL
            SELECT 'Maintenance', site.site_name, ticket.title, ticket.status,
                   ticket.requested_at, ticket.description
            FROM maintenance_ticket ticket
            INNER JOIN site ON site.site_id = ticket.site_id
            UNION ALL
            SELECT 'Document Compliance', tenant.tenant_name, document.doc_name,
                   document.doc_status, document.submitted_at, document.notes
            FROM document
            INNER JOIN tenant ON tenant.tenant_id = document.tenant_id
            UNION ALL
            SELECT 'Activity Log', log.actor_name,
                   CONCAT(log.entity_type, ' #', log.entity_id), log.action,
                   log.created_at, log.description
            FROM activity_log log
            ORDER BY `Date` DESC, category, item_name;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<AdminReportRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new AdminReportRow(
                GetNullableString(reader, "category"),
                GetNullableString(reader, "parent_name"),
                GetNullableString(reader, "item_name"),
                GetNullableString(reader, "status"),
                reader.IsDBNull(reader.GetOrdinal("Date")) ? null : reader.GetDateTime("Date"),
                GetNullableString(reader, "notes")));
        }

        return rows;
    }
}
