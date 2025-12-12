namespace SimpleSQLiteORM;

public class DbConnectionManager : IDisposable
{
    private readonly string _connectionString;

    public DbConnectionManager(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        Connection = new SqliteConnection(_connectionString);
        Connection.Open();
    }

    public SqliteConnection? Connection { get; private set; }

    private SqliteTransaction? _transaction;

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
