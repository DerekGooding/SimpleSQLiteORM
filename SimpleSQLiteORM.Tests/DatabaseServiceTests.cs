using Microsoft.Data.Sqlite;

namespace SimpleSQLiteORM.Tests;

[TestClass]
public class DatabaseServiceTests
{
    private const string _dbName = "test_db";
    private string _dbPath = null!;
    private TestDatabaseService _dbService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"{_dbName}_{Guid.NewGuid()}.db");
        var pathProvider = new TestPathProvider(_dbPath);
        _dbService = new TestDatabaseService(pathProvider);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        // This is to ensure the connection is closed before deleting the file.
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
    }

    [TestMethod]
    public void InsertAndRead_SingleItem_ShouldSucceed()
    {
        // Arrange
        var model = new TestModel { Name = "Test1", Value = 1.23 };
        _dbService.Insert(model, _dbName);

        // Act
        var results = _dbService.Read<TestModel>(_dbName);

        // Assert
        Assert.HasCount(1, results);
        Assert.AreEqual("Test1", results[0].Name);
        Assert.AreEqual(1.23, results[0].Value);
    }

    [TestMethod]
    public void InsertAndRead_MultipleItems_ShouldSucceed()
    {
        // Arrange
        var models = new List<TestModel>
        {
            new() { Name = "Test2", Value = 2.34 },
            new() { Name = "Test3", Value = 3.45 }
        };
        _dbService.Insert(models, _dbName);

        // Act
        var results = _dbService.Read<TestModel>(_dbName);

        // Assert
        Assert.HasCount(2, results);
    }

    [TestMethod]
    public void Update_SingleItem_ShouldSucceed()
    {
        // Arrange
        var model = new TestModel { Name = "Test4", Value = 4.56 };
        _dbService.Insert(model, _dbName);
        var items = _dbService.Read<TestModel>(_dbName);
        var itemToUpdate = items[0];
        itemToUpdate.Name = "UpdatedName";

        // Act
        _dbService.Update(itemToUpdate, _dbName);
        var updatedItems = _dbService.Read<TestModel>(_dbName);

        // Assert
        Assert.HasCount(1, updatedItems);
        Assert.AreEqual("UpdatedName", updatedItems[0].Name);
    }

    [TestMethod]
    public void Update_MultipleItems_ShouldSucceed()
    {
        // Arrange
        var models = new List<TestModel>
        {
            new() { Name = "UpdateTest1", Value = 1.1 },
            new() { Name = "UpdateTest2", Value = 2.2 }
        };
        _dbService.Insert(models, _dbName);
        var itemsToUpdate = _dbService.Read<TestModel>(_dbName);
        itemsToUpdate[0].Name = "Updated1";
        itemsToUpdate[1].Name = "Updated2";

        // Act
        _dbService.Update(itemsToUpdate, _dbName);
        var updatedItems = _dbService.Read<TestModel>(_dbName).OrderBy(i => i.Id).ToList();

        // Assert
        Assert.HasCount(2, updatedItems);
        Assert.AreEqual("Updated1", updatedItems[0].Name);
        Assert.AreEqual("Updated2", updatedItems[1].Name);
    }

    [TestMethod]
    public void Delete_SingleItem_ShouldSucceed()
    {
        // Arrange
        var model = new TestModel { Name = "Test5", Value = 5.67 };
        _dbService.Insert(model, _dbName);
        var items = _dbService.Read<TestModel>(_dbName);
        var itemToDelete = items[0];

        // Act
        _dbService.Delete(itemToDelete, _dbName);
        var remainingItems = _dbService.Read<TestModel>(_dbName);

        // Assert
        Assert.IsEmpty(remainingItems);
    }

    [TestMethod]
    public void DropTable_ShouldSucceed()
    {
        // Arrange
        var model = new TestModel { Name = "Test6", Value = 6.78 };
        _dbService.Insert(model, _dbName);

        // Act
        _dbService.DropTable<TestModel>(_dbName);

        // Assert
        var result = _dbService.Read<TestModel>(_dbName); // This should recreate the table without throwing an exception
        Assert.HasCount(0, result);
    }

    private class TestPathProvider : IPathProvider
    {
        public Dictionary<string, FileInfo> Paths { get; }

        public TestPathProvider(string dbPath) => Paths = new Dictionary<string, FileInfo>
        {
            { _dbName, new FileInfo(dbPath) }
        };
    }

    private class TestDatabaseService(IPathProvider pathProvider) : DatabaseServiceBase(pathProvider);
}
