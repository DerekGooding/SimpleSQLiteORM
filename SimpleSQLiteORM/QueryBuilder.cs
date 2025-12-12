using SimpleSQLiteORM.Model;

namespace SimpleSQLiteORM;

public class QueryBuilder<T>(Table<T> table) where T : new()
{
    private readonly Table<T> _table = table;
    private string _whereClause = string.Empty;
    private string _orderByClause = string.Empty;
    private int? _limit;

    /// <summary>
    /// Adds a WHERE clause
    /// </summary>
    public QueryBuilder<T> Where(string clause)
    {
        _whereClause = clause;
        return this;
    }

    /// <summary>
    /// Adds ORDER BY clause
    /// </summary>
    public QueryBuilder<T> OrderBy(string column, bool ascending = true)
    {
        _orderByClause = $"ORDER BY {column} {(ascending ? "ASC" : "DESC")}";
        return this;
    }

    /// <summary>
    /// Limits the number of results
    /// </summary>
    public QueryBuilder<T> Limit(int count)
    {
        _limit = count;
        return this;
    }

    /// <summary>
    /// Executes the query and returns a list of T
    /// </summary>
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
                    prop.SetValue(obj, reader[prop.Name]);
            }
            results.Add(obj);
        }

        return results;
    }

    /// <summary>
    /// Gets the first record or default
    /// </summary>
    public T? FirstOrDefault()
    {
        Limit(1);
        return ToList().FirstOrDefault();
    }
}