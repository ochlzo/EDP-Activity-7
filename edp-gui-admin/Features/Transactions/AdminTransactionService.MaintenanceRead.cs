namespace edp_gui_admin;

public sealed partial class AdminTransactionService
{
    public async Task<IReadOnlyList<AdminMaintenanceTicket>> LoadMaintenanceTicketsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSchemaAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ticket.ticket_id, site.site_name, ticket.title, ticket.description,
                   ticket.priority, ticket.status, ticket.requested_at, ticket.resolved_at,
                   ticket.notes
            FROM maintenance_ticket ticket
            INNER JOIN site ON site.site_id = ticket.site_id
            ORDER BY ticket.requested_at DESC, ticket.ticket_id DESC;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var tickets = new List<AdminMaintenanceTicket>();
        while (await reader.ReadAsync(cancellationToken))
        {
            tickets.Add(new AdminMaintenanceTicket(
                reader.GetInt32("ticket_id"),
                GetNullableString(reader, "site_name"),
                GetNullableString(reader, "title"),
                GetNullableString(reader, "description"),
                GetNullableString(reader, "priority"),
                GetNullableString(reader, "status"),
                reader.GetDateTime("requested_at"),
                reader.IsDBNull(reader.GetOrdinal("resolved_at")) ? null : reader.GetDateTime("resolved_at"),
                GetNullableString(reader, "notes")));
        }

        return tickets;
    }
}
