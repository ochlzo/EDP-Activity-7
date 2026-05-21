using MySqlConnector;

namespace edp_gui_admin;

public sealed partial class AdminRecordService
{
    public async Task<IReadOnlyList<AdminRiser>> LoadRisersAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT riser.riser_id, riser.riser_name, riser.site_id, site.site_name
            FROM riser
            INNER JOIN site ON site.site_id = riser.site_id
            ORDER BY riser.riser_name, riser.riser_id;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var risers = new List<AdminRiser>();
        while (await reader.ReadAsync(cancellationToken))
        {
            risers.Add(ReadRiser(reader));
        }

        return risers;
    }

    public async Task<AdminRiser> CreateRiserAsync(
        string riserName,
        int siteId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO riser (riser_name, site_id)
            VALUES (@riserName, @siteId);
            """;
        command.Parameters.AddWithValue("@riserName", riserName);
        command.Parameters.AddWithValue("@siteId", siteId);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var riser = new AdminRiser(
            Convert.ToInt32(command.LastInsertedId),
            riserName,
            siteId,
            await LoadSiteNameAsync(connection, siteId, cancellationToken));
        await LogAdminActivityAsync(
            "Created",
            "Riser",
            riser.RiserId,
            $"Created riser {riser.RiserName}.",
            cancellationToken);
        return riser;
    }

    public async Task UpdateRiserAsync(
        int riserId,
        string riserName,
        int siteId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE riser
            SET riser_name = @riserName, site_id = @siteId
            WHERE riser_id = @riserId;
            """;
        command.Parameters.AddWithValue("@riserName", riserName);
        command.Parameters.AddWithValue("@siteId", siteId);
        command.Parameters.AddWithValue("@riserId", riserId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Riser could not be updated.");
        }

        await LogAdminActivityAsync(
            "Updated",
            "Riser",
            riserId,
            $"Updated riser {riserName}.",
            cancellationToken);
    }

    public async Task DeleteRiserAsync(int riserId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM riser WHERE riser_id = @riserId;";
        command.Parameters.AddWithValue("@riserId", riserId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Riser could not be deleted.");
        }

        await LogAdminActivityAsync(
            "Deleted",
            "Riser",
            riserId,
            $"Deleted riser {riserId}.",
            cancellationToken);
    }

    private static AdminRiser ReadRiser(MySqlDataReader reader) => new(
        reader.GetInt32("riser_id"),
        GetNullableString(reader, "riser_name"),
        reader.GetInt32("site_id"),
        GetNullableString(reader, "site_name"));

    private static async Task<string> LoadSiteNameAsync(
        MySqlConnection connection,
        int siteId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT site_name FROM site WHERE site_id = @siteId;";
        command.Parameters.AddWithValue("@siteId", siteId);
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken)) ?? string.Empty;
    }
}
