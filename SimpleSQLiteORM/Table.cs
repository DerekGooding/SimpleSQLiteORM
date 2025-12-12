using SimpleSQLiteORM.Attributes;
using System.Reflection;

namespace SimpleSQLiteORM;

public class Table<T>(DbConnectionManager db) where T : new()
{
    public DbConnectionManager DbConnection { get; } = db;

    private readonly Type _type = typeof(T);

    private string TableName => _type.Name;

    /// <summary>
    /// Creates table based on POCO attributes
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
    /// Insert entity into table
    /// </summary>
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
            cmd.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(entity) ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Insert list of entities into table
    /// </summary>
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
            cmd.Parameters.AddWithValue("@" + columns[i], values[i] ?? DBNull.Value);
        }

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Update entity based on primary key
    /// </summary>
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
            cmd.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(entity) ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Update list of entities based on primary key
    /// </summary>
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
            cmd.Parameters.AddWithValue("@" + columns[i], values[i] ?? DBNull.Value);
        }

        cmd.Parameters.AddWithValue("@pk", keyValue ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Delete entity based on primary key
    /// </summary>
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
        cmd.Parameters.AddWithValue($"@{pk.Name}", pk.GetValue(entity) ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    public void DropTable()
    {
        if (DbConnection.Connection is not SqliteConnection connection)
            throw new Exception("No connection");
        using var cmd = connection.CreateCommand();

        cmd.CommandText = $"DROP TABLE IF EXISTS {TableName}";
        cmd.ExecuteNonQuery();
    }

    #region Helpers
    private static string SqliteType(Type type)
    {
        if (type == typeof(int) || type == typeof(long)) return "INTEGER";
        if (type == typeof(float) || type == typeof(double)) return "REAL";
        if (type == typeof(bool)) return "INTEGER"; // 0/1
        if (type == typeof(string)) return "TEXT";
        if (type == typeof(DateTime)) return "TEXT"; // store as ISO string
        return "BLOB";
    }

    private static PropertyInfo? GetPrimaryKeyProperty()
        => typeof(T).GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<PrimaryKeyAttribute>() != null);

    private static (List<string> Columns, List<object> Values) GetColumnsAndValues(T item)
    {
        var props = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetCustomAttribute<IgnoreAttribute>() == null)
            .ToList();

        var columns = new List<string>(props.Count);
        var values = new List<object>(props.Count);

        foreach (var prop in props)
        {
            if (prop.GetValue(item) is not object value)
                continue;
            columns.Add(prop.Name);
            values.Add(value);
        }

        return (columns, values);
    }
    #endregion
}
