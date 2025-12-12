namespace SimpleSQLiteORM;

/// <summary>
/// Defines the contract for database service operations supporting CRUD functionality
/// across multiple named SQLite databases.
/// </summary>
public interface IDatabaseService
{
    List<T> Read<T>(string target) where T : new();
    void Insert<T>(T item, string target) where T : new();
    void Insert<T>(List<T> items, string target) where T : new();
    void Update<T>(T item, string target) where T : new();
    void Update<T>(List<T> items, string target) where T : new();
    void Delete<T>(T item, string target) where T : new();
    void DropTable<T>(string target) where T : new();
}
