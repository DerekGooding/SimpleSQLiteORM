using Microsoft.Data.Sqlite;

namespace SimpleSQLiteORM.Tests;

[TestClass]
public class DatabaseServiceTests
{
    private const string DbName = "test_db";
    private string _dbPath = null!;
    private IDatabaseService _dbService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"{DbName}.db");
        var pathProvider = new TestPathProvider(_dbPath);
        _dbService = new DatabaseService(pathProvider);
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
        _dbService.Insert(model, DbName);

        // Act
        var results = _dbService.Read<TestModel>(DbName);

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
            new TestModel { Name = "Test2", Value = 2.34 },
            new TestModel { Name = "Test3", Value = 3.45 }
        };
        _dbService.Insert(models, DbName);

        // Act
        var results = _dbService.Read<TestModel>(DbName);

        // Assert
        Assert.HasCount(2, results);
    }

    [TestMethod]
    public void Update_SingleItem_ShouldSucceed()
    {
        // Arrange
        var model = new TestModel { Name = "Test4", Value = 4.56 };
        _dbService.Insert(model, DbName);
        var items = _dbService.Read<TestModel>(DbName);
        var itemToUpdate = items[0];
        itemToUpdate.Name = "UpdatedName";

        // Act
        _dbService.Update(itemToUpdate, DbName);
        var updatedItems = _dbService.Read<TestModel>(DbName);

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
            new TestModel { Name = "UpdateTest1", Value = 1.1 },
            new TestModel { Name = "UpdateTest2", Value = 2.2 }
        };
        _dbService.Insert(models, DbName);
        var itemsToUpdate = _dbService.Read<TestModel>(DbName);
        itemsToUpdate[0].Name = "Updated1";
        itemsToUpdate[1].Name = "Updated2";

        // Act
        _dbService.Update(itemsToUpdate, DbName);
        var updatedItems = _dbService.Read<TestModel>(DbName).OrderBy(i => i.Id).ToList();

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
        _dbService.Insert(model, DbName);
        var items = _dbService.Read<TestModel>(DbName);
        var itemToDelete = items[0];

        // Act
        _dbService.Delete(itemToDelete, DbName);
        var remainingItems = _dbService.Read<TestModel>(DbName);

        // Assert
        Assert.IsEmpty(remainingItems);
    }

    [TestMethod]
    public void DropTable_ShouldSucceed()
    {
        // Arrange
        var model = new TestModel { Name = "Test6", Value = 6.78 };
        _dbService.Insert(model, DbName);

        // Act
        _dbService.DropTable<TestModel>(DbName);

        // Assert
        Assert.Throws<SqliteException>(() => _dbService.Read<TestModel>(DbName));
    }

    private class TestPathProvider : IPathProvider
    {
        public Dictionary<string, FileInfo> Paths { get; }

        public TestPathProvider(string dbPath)
        {
            Paths = new Dictionary<string, FileInfo>
            {
                { DbName, new FileInfo(dbPath) }
            };
        }
    }
}
