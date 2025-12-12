namespace SimpleSQLiteORM.Attributes;

/// <summary>
/// Specifies a custom table name for a class.
/// If not present, the class name is used as the table name.
/// </summary>
/// <param name="name">The custom table name to use in the database</param>
[AttributeUsage(AttributeTargets.Class)]
public class TableAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
