namespace SimpleSQLiteORM.Attributes;

/// <summary>
/// Marks a property to be excluded from database operations.
/// Properties with this attribute will not be mapped to table columns.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class IgnoreAttribute : Attribute;