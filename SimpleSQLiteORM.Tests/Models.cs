using SimpleSQLiteORM.Attributes;

namespace SimpleSQLiteORM.Tests;

[Table("TestModels")]
public class TestModel
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; } = string.Empty;

    public double Value { get; set; }

    [SimpleSQLiteORM.Attributes.Ignore]
    public string IgnoredProperty { get; set; } = string.Empty;
}
