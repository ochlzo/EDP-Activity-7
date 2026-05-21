using MySqlConnector;

namespace edp_gui_admin;

public sealed partial class AdminRecordService
{
    private static async Task EnsureOwnerStatusColumnAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
                AND table_name = 'site_owner'
                AND column_name = 'is_active';
            """;

        var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync(cancellationToken)) > 0;
        if (exists)
        {
            return;
        }

        await using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = """
            ALTER TABLE site_owner
            ADD COLUMN is_active TINYINT(1) NOT NULL DEFAULT 1;
            """;
        await alterCommand.ExecuteNonQueryAsync(cancellationToken);
    }
}
