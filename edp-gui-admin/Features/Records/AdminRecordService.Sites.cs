using MySqlConnector;

namespace edp_gui_admin;

public sealed partial class AdminRecordService
{
    public async Task<IReadOnlyList<AdminSite>> LoadSitesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT site.site_id, site.site_name, site.owner_id, site_owner.owner_name
            FROM site
            INNER JOIN site_owner ON site_owner.owner_id = site.owner_id
            ORDER BY site.site_name, site.site_id;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var sites = new List<AdminSite>();
        while (await reader.ReadAsync(cancellationToken))
        {
            sites.Add(ReadSite(reader));
        }

        return sites;
    }

    public async Task<AdminSite> CreateSiteAsync(
        string siteName,
        int ownerId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO site (site_name, owner_id)
            VALUES (@siteName, @ownerId);
            """;
        command.Parameters.AddWithValue("@siteName", siteName);
        command.Parameters.AddWithValue("@ownerId", ownerId);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var siteId = Convert.ToInt32(command.LastInsertedId);
        await LogAdminActivityAsync("Created", "Site", siteId, $"Created site {siteName}.", cancellationToken);

        return new AdminSite(
            siteId,
            siteName,
            ownerId,
            await LoadOwnerNameAsync(connection, ownerId, cancellationToken));
    }

    public async Task UpdateSiteAsync(
        int siteId,
        string siteName,
        int ownerId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE site
            SET site_name = @siteName, owner_id = @ownerId
            WHERE site_id = @siteId;
            """;
        command.Parameters.AddWithValue("@siteName", siteName);
        command.Parameters.AddWithValue("@ownerId", ownerId);
        command.Parameters.AddWithValue("@siteId", siteId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Site could not be updated.");
        }

        await LogAdminActivityAsync("Updated", "Site", siteId, $"Updated site {siteName}.", cancellationToken);
    }

    public async Task DeleteSiteAsync(int siteId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM site WHERE site_id = @siteId;";
        command.Parameters.AddWithValue("@siteId", siteId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new InvalidOperationException("Site could not be deleted.");
        }

        await LogAdminActivityAsync("Deleted", "Site", siteId, "Deleted site.", cancellationToken);
    }

    private static AdminSite ReadSite(MySqlDataReader reader) => new(
        reader.GetInt32("site_id"),
        GetNullableString(reader, "site_name"),
        reader.GetInt32("owner_id"),
        GetNullableString(reader, "owner_name"));

    private static async Task<string> LoadOwnerNameAsync(
        MySqlConnection connection,
        int ownerId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT owner_name FROM site_owner WHERE owner_id = @ownerId;";
        command.Parameters.AddWithValue("@ownerId", ownerId);
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken)) ?? string.Empty;
    }
}
