namespace SimpleSQLiteORM.Attributes;

/// <summary>
/// Marks a property as the primary key for the table.
/// Required for Update and Delete operations to identify records.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PrimaryKeyAttribute : Attribute;
