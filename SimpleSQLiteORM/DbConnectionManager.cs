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

    public void Dispose()
    {
        if (Connection != null)
        {
            Connection.Close();
            Connection.Dispose();
            Connection = null;
        }
    }
}
