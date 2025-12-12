namespace SimpleSQLiteORM.Attributes;

/// <summary>
/// Marks a property as NOT NULL in the database schema.
/// Note: Currently not enforced in the CreateTable implementation.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NotNullAttribute : Attribute;