namespace SimpleSQLiteORM;

/// <summary>
/// Provides a simple SQLite ORM service for performing CRUD operations on multiple named databases.
/// Automatically manages table creation and database file initialization.
/// </summary>
public class DatabaseService : IDatabaseService
{
    private readonly IPathProvider _pathProvider;
    private FileInfo GetDatabasePath(string name)
        => _pathProvider.Paths.TryGetValue(name, out var path) ? path
            : throw new ArgumentException($"Error finding database {name}");

    /// <summary>
    /// Initializes a new instance of the DatabaseService with the specified path provider.
    /// Creates all database directories and files if they don't exist.
    /// </summary>
    /// <param name="pathProvider">Provider that supplies database file paths</param>
    /// <exception cref="ArgumentException">Thrown when database paths are invalid</exception>
    public DatabaseService(IPathProvider pathProvider)
    {
        _pathProvider = pathProvider;
        foreach (var path in pathProvider.Paths)
        {
            var dir = path.Value.Directory ?? throw new ArgumentException($"Error with path {path.Value.FullName}");
            if (!dir.Exists)
                dir.Create();
            if(!path.Value.Exists)
                path.Value.Create().Dispose();
        }
    }

    /// <summary>
    /// Inserts a single item into the specified database.
    /// </summary>
    /// <typeparam name="T">The type of object to insert (must have parameterless constructor)</typeparam>
    /// <param name="item">The item to insert</param>
    /// <param name="target">The name of the target database</param>
    public void Insert<T>(T item, string target) where T : new() => Insert(item, GetDatabasePath(target));
    /// <summary>
    /// Inserts multiple items into the specified database.
    /// </summary>
    /// <typeparam name="T">The type of objects to insert (must have parameterless constructor)</typeparam>
    /// <param name="items">The list of items to insert</param>
    /// <param name="target">The name of the target database</param>
    public void Insert<T>(List<T> items, string target) where T : new() => Insert(items, GetDatabasePath(target));

    private static void Insert<T>(T item, FileInfo target) where T : new()
    {
        using var db = new DbConnectionManager(target.FullName);
        var table = new Table<T>(db);

        table.CreateTable();
        table.Insert(item);
    }

    private static void Insert<T>(List<T> items, FileInfo target) where T : new()
    {
        using var db = new DbConnectionManager(target.FullName);
        var table = new Table<T>(db);

        table.CreateTable();
        table.InsertMany(items);
    }

    /// <summary>
    /// Retrieves all records of the specified type from the target database.
    /// Creates the table if it doesn't exist.
    /// </summary>
    /// <typeparam name="T">The type of objects to retrieve (must have parameterless constructor)</typeparam>
    /// <param name="target">The name of the target database</param>
    /// <returns>A list of all records in the table</returns>
    public List<T> Read<T>(string target) where T : new() => Read<T>(GetDatabasePath(target));

    private static List<T> Read<T>(FileInfo dbPath) where T : new()
    {
        using var db = new DbConnectionManager(dbPath.FullName);
        var table = new Table<T>(db);

        try
        {
            return new QueryBuilder<T>(table).ToList();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 1)
        {
            table.CreateTable();
            return new QueryBuilder<T>(table).ToList();
        }
    }

    /// <summary>
    /// Updates a single item in the specified database.
    /// </summary>
    /// <typeparam name="T">The type of object to update (must have parameterless constructor)</typeparam>
    /// <param name="item">The item to update</param>
    /// <param name="target">The name of the target database</param>
    public void Update<T>(T item, string target) where T : new() => Update(item, GetDatabasePath(target));
    /// <summary>
    /// Updates multiple items in the specified database.
    /// </summary>
    /// <typeparam name="T">The type of objects to update (must have parameterless constructor)</typeparam>
    /// <param name="items">The list of items to update</param>
    /// <param name="target">The name of the target database</param>
    public void Update<T>(List<T> items, string target) where T : new() => Update(items, GetDatabasePath(target));

    private static void Update<T>(T item, FileInfo target) where T : new()
    {
        using var db = new DbConnectionManager(target.FullName);
        var table = new Table<T>(db);

        table.CreateTable();
        table.Update(item);
    }
    private static void Update<T>(List<T> items, FileInfo target) where T : new()
    {
        using var db = new DbConnectionManager(target.FullName);
        var table = new Table<T>(db);

        table.CreateTable();
        table.UpdateMany(items);
    }

    /// <summary>
    /// Deletes a single item from the specified database.
    /// </summary>
    /// <typeparam name="T">The type of object to delete (must have parameterless constructor)</typeparam>
    /// <param name="item">The item to delete</param>
    /// <param name="target">The name of the target database</param>
    public void Delete<T>(T item, string target) where T : new() => Delete(item, GetDatabasePath(target));
    /// <summary>
    /// Drops the entire table for the specified type from the target database.
    /// WARNING: This operation is destructive and cannot be undone.
    /// </summary>
    /// <typeparam name="T">The type representing the table to drop (must have parameterless constructor)</typeparam>
    /// <param name="target">The name of the target database</param>
    public void DropTable<T>(string target) where T : new() => DropTable<T>(GetDatabasePath(target));

    private static void Delete<T>(T item, FileInfo target) where T : new()
    {
        using var db = new DbConnectionManager(target.FullName);
        var table = new Table<T>(db);

        table.CreateTable();
        table.Delete(item);
    }

    private static void DropTable<T>(FileInfo target) where T : new()
    {
        using var db = new DbConnectionManager(target.FullName);
        var table = new Table<T>(db);

        table.DropTable();
    }
}