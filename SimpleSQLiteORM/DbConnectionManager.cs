namespace SimpleSQLiteORM;

/// <summary>
/// Manages SQLite database connections with automatic resource disposal.
/// Opens a connection on instantiation and ensures proper cleanup through IDisposable.
/// </summary>
public class DbConnectionManager : IDisposable
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new database connection manager and opens the connection.
    /// </summary>
    /// <param name="dbPath">The file path to the SQLite database</param>
    public DbConnectionManager(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        Connection = new SqliteConnection(_connectionString);
        Connection.Open();
    }

    /// <summary>
    /// Gets the active SQLite connection. May be null after disposal.
    /// </summary>
    public SqliteConnection? Connection { get; private set; }

    private SqliteTransaction? _transaction;

    /// <summary>
    /// Closes and disposes the database connection and any active transactions.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Connection != null)
        {
            Connection.Close();
            Connection.Dispose();
            Connection = null;
        }

        _transaction?.Dispose();
        _transaction = null;
    }
}
