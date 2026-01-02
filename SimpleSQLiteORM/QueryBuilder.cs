namespace SimpleSQLiteORM;

/// <summary>
/// Provides a fluent interface for building and executing SQLite queries on a specific table.
/// Supports WHERE clauses, ORDER BY, and LIMIT operations.
/// </summary>
/// <typeparam name="T">The entity type (must have parameterless constructor)</typeparam>
public class QueryBuilder<T>(Table<T> table) where T : new()
{
    private readonly Table<T> _table = table;
    private string _whereClause = string.Empty;
    private string _orderByClause = string.Empty;
    private int? _limit;

    /// <summary>
    /// Adds a WHERE clause to the query.
    /// </summary>
    /// <param name="clause">The WHERE condition (e.g., "Age > 18")</param>
    /// <returns>The query builder for method chaining</returns>
    public QueryBuilder<T> Where(string clause)
    {
        _whereClause = clause;
        return this;
    }

    /// <summary>
    /// Adds an ORDER BY clause to the query.
    /// </summary>
    /// <param name="column">The column name to sort by</param>
    /// <param name="ascending">True for ascending order, false for descending</param>
    /// <returns>The query builder for method chaining</returns>
    public QueryBuilder<T> OrderBy(string column, bool ascending = true)
    {
        _orderByClause = $"ORDER BY {column} {(ascending ? "ASC" : "DESC")}";
        return this;
    }

    /// <summary>
    /// Limits the number of results returned by the query.
    /// </summary>
    /// <param name="count">The maximum number of records to return</param>
    /// <returns>The query builder for method chaining</returns>
    public QueryBuilder<T> Limit(int count)
    {
        _limit = count;
        return this;
    }

    /// <summary>
    /// Executes the query and returns all matching records as a list.
    /// </summary>
    /// <returns>A list of entities matching the query criteria</returns>
    public List<T> ToList()
    {
        var results = new List<T>();
        var type = typeof(T);
        var props = type.GetProperties();

        var sql = $"SELECT * FROM {type.Name}";

        if (!string.IsNullOrEmpty(_whereClause))
            sql += $" WHERE {_whereClause}";

        if (!string.IsNullOrEmpty(_orderByClause))
            sql += $" {_orderByClause}";

        if (_limit.HasValue)
            sql += $" LIMIT {_limit.Value}";

        using var cmd = _table.DbConnection.Connection!.CreateCommand();
        cmd.CommandText = sql;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var obj = new T();
            foreach (var prop in props)
            {
                if (!reader.IsDBNull(reader.GetOrdinal(prop.Name)))
                {
                    var raw = reader[prop.Name];
                    var converted = ConvertForProperty(raw, prop.PropertyType);
                    prop.SetValue(obj, converted);
                }
            }
            results.Add(obj);
        }

        return results;
    }

    /// <summary>
    /// Executes the query and returns the first matching record or null if none found.
    /// </summary>
    /// <returns>The first matching entity or null</returns>
    public T? FirstOrDefault()
    {
        Limit(1);
        return ToList().FirstOrDefault();
    }

    private static object? ConvertForProperty(object value, Type targetType)
    {
        if (value is null or DBNull)
            return value;

        var valueType = value.GetType();

        if (targetType == valueType)
            return value;

        if (targetType.IsEnum)
            return Enum.ToObject(targetType, value);

        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch (Exception ex)
        {
            throw new InvalidCastException(
                $"Cannot convert value '{value}' ({valueType}) to {targetType}", ex);
        }
    }
}