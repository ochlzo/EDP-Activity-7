using MySqlConnector;

namespace edp_gui_admin;

public sealed partial class AdminTransactionService
{
    private readonly string _connectionString;

    public AdminTransactionService(string connectionString)
    {
        _connectionString = connectionString;
    }

    private async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
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
