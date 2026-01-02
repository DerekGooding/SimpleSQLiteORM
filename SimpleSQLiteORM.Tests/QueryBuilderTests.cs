namespace SimpleSQLiteORM.Tests;

[TestClass]
public class QueryBuilderTests
{
    private DbConnectionManager _dbConnectionManager = null!;
    private Table<TestModel> _table = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var connectionString = "Data Source=:memory:";
        _dbConnectionManager = new DbConnectionManager(connectionString);
        _table = new Table<TestModel>(_dbConnectionManager);
        _table.CreateTable();

        var models = new List<TestModel>
        {
            new TestModel { Name = "FilterTest1", Value = 10 },
            new TestModel { Name = "FilterTest2", Value = 20 },
            new TestModel { Name = "FilterTest3", Value = 30 },
            new TestModel { Name = "OrderB", Value = 1 },
            new TestModel { Name = "OrderA", Value = 2 },
            new TestModel { Name = "OrderC", Value = 3 }
        };
        _table.InsertMany(models);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _dbConnectionManager.Dispose();
    }

    [TestMethod]
    public void Where_ShouldFilterResults()
    {
        // Arrange
        var queryBuilder = new QueryBuilder<TestModel>(_table);

        // Act
        var results = queryBuilder.Where("Value > 15").ToList();

        // Assert
        Assert.HasCount(2, results);
        Assert.IsTrue(results.All(r => r.Value > 15));
    }

    [TestMethod]
    public void OrderBy_ShouldSortResults()
    {
        // Arrange
        var queryBuilder = new QueryBuilder<TestModel>(_table);

        // Act
        var results = queryBuilder.Where("Value < 5").OrderBy("Name").ToList();

        // Assert
        Assert.HasCount(3, results);
        Assert.AreEqual("OrderA", results[0].Name);
        Assert.AreEqual("OrderB", results[1].Name);
        Assert.AreEqual("OrderC", results[2].Name);
    }

    [TestMethod]
    public void Limit_ShouldLimitResults()
    {
        // Arrange
        var queryBuilder = new QueryBuilder<TestModel>(_table);

        // Act
        var results = queryBuilder.Limit(2).ToList();

        // Assert
        Assert.HasCount(2, results);
    }

    [TestMethod]
    public void FirstOrDefault_ShouldReturnFirstItem()
    {
        // Arrange
        var queryBuilder = new QueryBuilder<TestModel>(_table);

        // Act
        var result = queryBuilder.Where("Name = 'FilterTest1'").FirstOrDefault();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("FilterTest1", result.Name);
    }

    [TestMethod]
    public void FirstOrDefault_ShouldReturnNull_WhenNoItem()
    {
        // Arrange
        var queryBuilder = new QueryBuilder<TestModel>(_table);

        // Act
        var result = queryBuilder.Where("Name = 'NonExistent'").FirstOrDefault();

        // Assert
        Assert.IsNull(result);
    }
}
