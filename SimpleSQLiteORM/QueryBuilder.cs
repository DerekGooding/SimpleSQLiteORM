using SimpleSQLiteORM.Model;

namespace SimpleSQLiteORM;

public class QueryBuilder<T> where T : new()
{
    private readonly Table<T> _table;
    private string _whereClause = "";

    public QueryBuilder(Table<T> table)
    {
        _table = table;
    }

    public QueryBuilder<T> Where(string clause)
    {
        _whereClause = clause;
        return this;
    }

    public List<T> ToList()
    {
        var results = new List<T>();
        var type = typeof(T);
        var props = type.GetProperties();

        var sql = $"SELECT * FROM {type.Name}";
        if (!string.IsNullOrEmpty(_whereClause))
            sql += $" WHERE {_whereClause}";

        using var cmd = _table.DbConnection.Connection.CreateCommand();
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
}