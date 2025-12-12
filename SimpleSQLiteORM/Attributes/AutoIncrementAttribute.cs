namespace SimpleSQLiteORM.Attributes;

/// <summary>
/// Marks a property as an auto-incrementing column in the database.
/// Typically used with primary key integer columns.
/// Properties with this attribute are excluded from INSERT operations.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class AutoIncrementAttribute : Attribute;
