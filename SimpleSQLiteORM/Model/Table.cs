using SimpleSQLiteORM.Attributes;

namespace SimpleSQLiteORM.Model;

public class Table<T>(DbConnectionManager db) where T : new()
{
    public DbConnectionManager DbConnection { get; } = db;

    /// <summary>
    /// Creates table based on POCO attributes
    /// </summary>
    public void CreateTable()
    {
        var type = typeof(T);
        var props = type.GetProperties();
        var columns = new List<string>();

        foreach (var prop in props)
        {
            var columnDef = prop.Name + " " + SqliteType(prop.PropertyType);

            if (Attribute.IsDefined(prop, typeof(PrimaryKeyAttribute)))
                columnDef += " PRIMARY KEY";

            if (Attribute.IsDefined(prop, typeof(AutoIncrementAttribute)))
                columnDef += " AUTOINCREMENT";

            columns.Add(columnDef);
        }

        var sql = $"CREATE TABLE IF NOT EXISTS {type.Name} ({string.Join(",", columns)});";

        using var cmd = DbConnection.Connection.CreateCommand();
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

        using var cmd = DbConnection.Connection.CreateCommand();
        cmd.CommandText = sql;

        foreach (var prop in props)
            cmd.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(entity) ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Update entity based on primary key
    /// </summary>
    public void Update(T entity)
    {
        var type = typeof(T);
        var props = type.GetProperties();
        var pk = props.FirstOrDefault(p => Attribute.IsDefined(p, typeof(PrimaryKeyAttribute)));

        if (pk == null)
            throw new Exception("PrimaryKey attribute required for Update.");

        var setClauses = props
            .Where(p => !Attribute.IsDefined(p, typeof(PrimaryKeyAttribute)))
            .Select(p => $"{p.Name}=@{p.Name}");

        var sql = $"UPDATE {type.Name} SET {string.Join(",", setClauses)} WHERE {pk.Name}=@{pk.Name};";

        using var cmd = DbConnection.Connection.CreateCommand();
        cmd.CommandText = sql;

        foreach (var prop in props)
            cmd.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(entity) ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Delete entity based on primary key
    /// </summary>
    public void Delete(T entity)
    {
        var type = typeof(T);
        var props = type.GetProperties();
        var pk = props.FirstOrDefault(p => Attribute.IsDefined(p, typeof(PrimaryKeyAttribute)));

        if (pk == null)
            throw new Exception("PrimaryKey attribute required for Delete.");

        var sql = $"DELETE FROM {type.Name} WHERE {pk.Name}=@{pk.Name};";

        using var cmd = DbConnection.Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue($"@{pk.Name}", pk.GetValue(entity) ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Helper: Maps C# type to SQLite type
    /// </summary>
    private string SqliteType(Type type)
    {
        if (type == typeof(int) || type == typeof(long)) return "INTEGER";
        if (type == typeof(float) || type == typeof(double)) return "REAL";
        if (type == typeof(bool)) return "INTEGER"; // 0/1
        if (type == typeof(string)) return "TEXT";
        if (type == typeof(DateTime)) return "TEXT"; // store as ISO string
        return "BLOB";
    }
}