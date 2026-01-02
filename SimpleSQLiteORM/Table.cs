using SimpleSQLiteORM.Attributes;
using System.Reflection;

namespace SimpleSQLiteORM;

public class Table<T>(DbConnectionManager db) where T : new()
{
    /// <summary>
    /// Gets the database connection manager for this table.
    /// </summary>
    public DbConnectionManager DbConnection { get; } = db;

    private readonly Type _type = typeof(T);

    private string TableName => _type.Name;

    /// <summary>
    /// Creates the table if it doesn't exist, mapping entity properties to columns.
    /// Recognizes PrimaryKey and AutoIncrement attributes.
    /// </summary>
    public void CreateTable()
    {
        var props = _type.GetProperties();
        var columns = new List<string>();

        foreach (var prop in props)
        {
            var columnDef = prop.Name + " " + Table<T>.SqliteType(prop.PropertyType);

            if (Attribute.IsDefined(prop, typeof(PrimaryKeyAttribute)))
                columnDef += " PRIMARY KEY";

            if (Attribute.IsDefined(prop, typeof(AutoIncrementAttribute)))
                columnDef += " AUTOINCREMENT";

            columns.Add(columnDef);
        }

        var sql = $"CREATE TABLE IF NOT EXISTS {TableName} ({string.Join(",", columns)});";
        if (DbConnection.Connection is not SqliteConnection connection)
            throw new Exception("No connection");

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a single entity into the table.
    /// Excludes properties marked with AutoIncrement attribute.
    /// </summary>
    /// <param name="entity">The entity to insert</param>
    /// <exception cref="Exception">Thrown when database connection is unavailable</exception>
    public void Insert(T entity)
    {
        var type = typeof(T);
        var props = type.GetProperties()
                        .Where(p => !Attribute.IsDefined(p, typeof(AutoIncrementAttribute)))
                        .ToArray();

        var columns = string.Join(",", props.Select(p => p.Name));
        var parameters = string.Join(",", props.Select(p => $"@{p.Name}"));

        var sql = $"INSERT INTO {type.Name} ({columns}) VALUES ({parameters});";

        if (DbConnection.Connection is not SqliteConnection connection)
            throw new Exception("No connection");
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        foreach (var prop in props)
            cmd.Parameters.AddWithValue($"@{prop.Name}", NormalizeForSqlite(prop.GetValue(entity)));

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts multiple entities into the table within a single transaction for better performance.
    /// </summary>
    /// <param name="items">The collection of entities to insert</param>
    /// <exception cref="Exception">Thrown when database connection is unavailable</exception>
    public void InsertMany(IEnumerable<T> items)
    {
        if (DbConnection.Connection is not SqliteConnection connection)
            throw new Exception("No connection");
        using var tx = connection.BeginTransaction();

        foreach (var item in items)
        {
            InsertInternal(connection, item);
        }

        tx.Commit();
    }

    private void InsertInternal(SqliteConnection conn, T item)
    {
        var (columns, values) = Table<T>.GetColumnsAndValues(item);

        var colNames = string.Join(", ", columns);
        var paramNames = string.Join(", ", columns.Select(c => "@" + c));

        var sql = $"INSERT INTO {TableName} ({colNames}) VALUES ({paramNames})";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        for (var i = 0; i < columns.Count; i++)
        {
            cmd.Parameters.AddWithValue("@" + columns[i], NormalizeForSqlite(values[i]));
        }

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Updates an existing entity in the table based on its primary key.
    /// </summary>
    /// <param name="entity">The entity to update</param>
    /// <exception cref="Exception">Thrown when no primary key is defined or connection is unavailable</exception>
    public void Update(T entity)
    {
        var type = typeof(T);
        var props = type.GetProperties();
        var pk = props.FirstOrDefault(p => Attribute.IsDefined(p, typeof(PrimaryKeyAttribute))) ?? throw new Exception("PrimaryKey attribute required for Update.");
        var setClauses = props
            .Where(p => !Attribute.IsDefined(p, typeof(PrimaryKeyAttribute)))
            .Select(p => $"{p.Name}=@{p.Name}");

        var sql = $"UPDATE {type.Name} SET {string.Join(",", setClauses)} WHERE {pk.Name}=@{pk.Name};";

        if (DbConnection.Connection is not SqliteConnection connection)
            throw new Exception("No connection");
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        foreach (var prop in props)
            cmd.Parameters.AddWithValue($"@{prop.Name}", NormalizeForSqlite(prop.GetValue(entity)));

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Updates multiple entities in the table within a single transaction.
    /// Each entity is updated based on its primary key.
    /// </summary>
    /// <param name="items">The collection of entities to update</param>
    /// <exception cref="Exception">Thrown when database connection is unavailable</exception>
    /// <exception cref="InvalidOperationException">Thrown when no primary key is found</exception>
    public void UpdateMany(IEnumerable<T> items)
    {
        if (DbConnection.Connection is not SqliteConnection connection)
            throw new Exception("No connection");
        using var tx = connection.BeginTransaction();

        foreach (var item in items)
        {
            UpdateInternal(connection, item);
        }

        tx.Commit();
    }

    private void UpdateInternal(SqliteConnection conn, T item)
    {
        var keyProp = Table<T>.GetPrimaryKeyProperty()
            ?? throw new InvalidOperationException("No primary key found");
        var keyValue = keyProp.GetValue(item);

        var (columns, values) = Table<T>.GetColumnsAndValues(item);

        var setters = string.Join(", ", columns.Select(c => $"{c} = @{c}"));
        var sql = $"UPDATE {TableName} SET {setters} WHERE {keyProp.Name} = @pk";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        for (var i = 0; i < columns.Count; i++)
        {
            cmd.Parameters.AddWithValue("@" + columns[i], NormalizeForSqlite(values[i]));
        }

        cmd.Parameters.AddWithValue("@pk", NormalizeForSqlite(keyValue));

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Deletes an entity from the table based on its primary key.
    /// </summary>
    /// <param name="entity">The entity to delete</param>
    /// <exception cref="Exception">Thrown when no primary key is defined or connection is unavailable</exception>
    public void Delete(T entity)
    {
        var type = typeof(T);
        var props = type.GetProperties();
        var pk = props.FirstOrDefault(p => Attribute.IsDefined(p, typeof(PrimaryKeyAttribute)))
            ?? throw new Exception("PrimaryKey attribute required for Delete.");
        var sql = $"DELETE FROM {type.Name} WHERE {pk.Name}=@{pk.Name};";

        if (DbConnection.Connection is not SqliteConnection connection)
            throw new Exception("No connection");
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue($"@{pk.Name}", NormalizeForSqlite(pk.GetValue(entity)));

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Drops the entire table from the database if it exists.
    /// WARNING: This operation is destructive and cannot be undone.
    /// </summary>
    /// <exception cref="Exception">Thrown when database connection is unavailable</exception>
    public void DropTable()
    {
        if (DbConnection.Connection is not SqliteConnection connection)
            throw new Exception("No connection");
        using var cmd = connection.CreateCommand();

        cmd.CommandText = $"DROP TABLE IF EXISTS {TableName}";
        cmd.ExecuteNonQuery();
    }

    #region Helpers
    /// <summary>
    /// Maps a .NET type to its corresponding SQLite type.
    /// </summary>
    /// <param name="type">The .NET type to map</param>
    /// <returns>The SQLite type string (INTEGER, REAL, TEXT, or BLOB)</returns>
    private static string SqliteType(Type type)
    {
        if (type == typeof(int) || type == typeof(long)) return "INTEGER";
        if (type == typeof(float) || type == typeof(double)) return "REAL";
        if (type == typeof(bool)) return "INTEGER"; // 0/1
        if (type == typeof(string)) return "TEXT";
        if (type == typeof(DateTime)) return "TEXT"; // store as ISO string
        return "BLOB";
    }

    /// <summary>
    /// Gets the property marked with PrimaryKeyAttribute from the entity type.
    /// </summary>
    /// <returns>The primary key property or null if none found</returns>
    private static PropertyInfo? GetPrimaryKeyProperty()
        => typeof(T).GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<PrimaryKeyAttribute>() != null);

    /// <summary>
    /// Extracts column names and values from an entity instance.
    /// Excludes properties marked with IgnoreAttribute and properties with null values.
    /// </summary>
    /// <param name="item">The entity instance to extract data from</param>
    /// <returns>A tuple containing lists of column names and their corresponding values</returns>
    private static (List<string> Columns, List<object> Values) GetColumnsAndValues(T item)
    {
        var pkProp = Table<T>.GetPrimaryKeyProperty();
        var props = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead &&
                                  p.GetCustomAttribute<IgnoreAttribute>() == null &&
                                  p != pkProp)
            .ToList();

        var columns = new List<string>(props.Count);
        var values = new List<object>(props.Count);

        foreach (var prop in props)
        {
            columns.Add(prop.Name);
            values.Add(prop.GetValue(item) ?? DBNull.Value);
        }

        return (columns, values);
    }
    #endregion

    private static object NormalizeForSqlite(object? value) => value == null ? DBNull.Value
    : value switch
    {
        bool b => b ? 1L : 0L,
        byte b => (long)b,
        short s => (long)s,
        int i => (long)i,
        uint u => (long)u,
        long l => l,
        Enum e => Convert.ToInt64(e),
        _ => value
    };
}
