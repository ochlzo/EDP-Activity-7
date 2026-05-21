using MySqlConnector;

namespace edp_gui_admin;

public sealed partial class AdminRecordService
{
    private readonly string _connectionString;

    public AdminRecordService(string connectionString)
    {
        _connectionString = connectionString;
    }

    private async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static string GetNullableString(MySqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    }
}
