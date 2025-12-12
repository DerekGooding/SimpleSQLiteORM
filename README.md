# SimpleSQLiteORM

A lightweight, easy-to-use Object-Relational Mapping (ORM) library for SQLite in .NET. SimpleSQLiteORM provides a simple and intuitive API for performing CRUD operations without writing SQL, while maintaining the flexibility to work with multiple databases.

## Features

- ✨ **Simple API** - Perform CRUD operations with minimal code
- 🔄 **Automatic Table Management** - Tables are created automatically based on your entity classes
- 🏷️ **Attribute-Based Mapping** - Use attributes to define primary keys, auto-increment fields, and more
- 📦 **Bulk Operations** - Efficient batch insert and update operations with transactions
- 🔍 **Fluent Query Builder** - Build queries with a clean, chainable syntax
- 🗂️ **Multiple Database Support** - Manage multiple SQLite databases in a single application
- 💪 **Type-Safe** - Strongly-typed operations using C# generics
- 🎯 **Zero Configuration** - No complex setup or configuration files required

## Installation

```bash
# Add the package to your project (replace with actual package name when published)
dotnet add package SimpleSQLiteORM
```

## Quick Start

### 1. Define Your Entity

```csharp
using SimpleSQLiteORM.Attributes;

public class User
{
    [PrimaryKey]
    [AutoIncrement]
    public int Id { get; set; }
    
    public string Username { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    
    [Ignore] // This property won't be stored in the database
    public string TempToken { get; set; }
}
```

### 2. Setup Database Service

```csharp
using SimpleSQLiteORM;

// Implement IPathProvider to specify your database locations
public class MyPathProvider : IPathProvider
{
    public Dictionary<string, FileInfo> Paths { get; } = new()
    {
        ["UserDb"] = new FileInfo("users.db"),
        ["ProductDb"] = new FileInfo("products.db")
    };
}

// Create the database service
var pathProvider = new MyPathProvider();
var dbService = new DatabaseService(pathProvider);
```

### 3. Perform CRUD Operations

```csharp
// INSERT
var user = new User 
{ 
    Username = "john_doe", 
    Email = "john@example.com",
    CreatedAt = DateTime.Now
};
dbService.Insert(user, "UserDb");

// INSERT MANY (uses transactions for better performance)
var users = new List<User>
{
    new User { Username = "jane_doe", Email = "jane@example.com", CreatedAt = DateTime.Now },
    new User { Username = "bob_smith", Email = "bob@example.com", CreatedAt = DateTime.Now }
};
dbService.Insert(users, "UserDb");

// READ ALL
var allUsers = dbService.Read<User>("UserDb");

// UPDATE
user.Email = "newemail@example.com";
dbService.Update(user, "UserDb");

// UPDATE MANY
dbService.Update(users, "UserDb");

// DELETE
dbService.Delete(user, "UserDb");

// DROP TABLE (destructive operation - use with caution!)
dbService.DropTable<User>("UserDb");
```

## Advanced Usage

### Query Builder

Use the `QueryBuilder` for more complex queries with WHERE, ORDER BY, and LIMIT clauses:

```csharp
// Note: You need direct access to the Table<T> instance for QueryBuilder
using var db = new DbConnectionManager("users.db");
var table = new Table<User>(db);
table.CreateTable();

// Query with WHERE clause
var adults = new QueryBuilder<User>(table)
    .Where("Age >= 18")
    .ToList();

// Query with ORDER BY
var sortedUsers = new QueryBuilder<User>(table)
    .OrderBy("Username", ascending: true)
    .ToList();

// Query with LIMIT
var topUsers = new QueryBuilder<User>(table)
    .OrderBy("CreatedAt", ascending: false)
    .Limit(10)
    .ToList();

// Get single result
var user = new QueryBuilder<User>(table)
    .Where("Username = 'john_doe'")
    .FirstOrDefault();

// Combine multiple clauses
var results = new QueryBuilder<User>(table)
    .Where("Email LIKE '%@example.com'")
    .OrderBy("CreatedAt", ascending: false)
    .Limit(5)
    .ToList();
```

### Attributes Reference

| Attribute | Target | Description |
|-----------|--------|-------------|
| `[PrimaryKey]` | Property | Marks the property as the table's primary key (required for Update/Delete) |
| `[AutoIncrement]` | Property | Marks the property as auto-incrementing (excluded from INSERT operations) |
| `[Ignore]` | Property | Excludes the property from database operations |
| `[NotNull]` | Property | Documents that the column should not accept null values |
| `[Table("name")]` | Class | Specifies a custom table name (defaults to class name) |

### Type Mapping

SimpleSQLiteORM automatically maps .NET types to SQLite types:

| .NET Type | SQLite Type |
|-----------|-------------|
| `int`, `long` | INTEGER |
| `float`, `double` | REAL |
| `bool` | INTEGER (0/1) |
| `string` | TEXT |
| `DateTime` | TEXT (ISO format) |
| Other | BLOB |

### Multiple Database Example

```csharp
public class ApplicationPathProvider : IPathProvider
{
    public Dictionary<string, FileInfo> Paths { get; } = new()
    {
        ["Users"] = new FileInfo("data/users.db"),
        ["Products"] = new FileInfo("data/products.db"),
        ["Orders"] = new FileInfo("data/orders.db"),
        ["Analytics"] = new FileInfo("analytics/metrics.db")
    };
}

var dbService = new DatabaseService(new ApplicationPathProvider());

// Work with different databases
dbService.Insert(user, "Users");
dbService.Insert(product, "Products");
dbService.Insert(order, "Orders");
```

## Best Practices

### 1. Always Define a Primary Key

```csharp
public class Product
{
    [PrimaryKey]
    [AutoIncrement]
    public int Id { get; set; }  // Required for Update/Delete operations
    
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

### 2. Use Bulk Operations for Better Performance

```csharp
// Good - Single transaction
dbService.Insert(listOfUsers, "UserDb");

// Less efficient - Multiple transactions
foreach (var user in listOfUsers)
{
    dbService.Insert(user, "UserDb");
}
```

### 3. Handle Exceptions

```csharp
try
{
    dbService.Insert(user, "UserDb");
}
catch (ArgumentException ex)
{
    // Database name not found in path provider
    Console.WriteLine($"Database error: {ex.Message}");
}
catch (Exception ex)
{
    // Other database errors
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

### 4. Organize Database Paths

```csharp
public class PathProvider : IPathProvider
{
    private readonly string _baseDirectory;
    
    public PathProvider(string baseDirectory = "data")
    {
        _baseDirectory = baseDirectory;
        Directory.CreateDirectory(baseDirectory);
    }
    
    public Dictionary<string, FileInfo> Paths { get; } = new()
    {
        ["Main"] = new FileInfo(Path.Combine(_baseDirectory, "main.db")),
        ["Cache"] = new FileInfo(Path.Combine(_baseDirectory, "cache.db"))
    };
}
```

## Limitations

- **No Relationships** - This ORM does not handle foreign keys or relationships between tables
- **Basic Queries** - Complex joins and subqueries are not supported through the API
- **Thread Safety** - Concurrent operations on the same database may require additional synchronization
- **No Migrations** - Schema changes must be handled manually (consider `DropTable` and recreate for major changes)
- **Limited Attribute Support** - `NotNull` attribute is not currently enforced during table creation

## Examples

### Complete Application Example

```csharp
using SimpleSQLiteORM;
using SimpleSQLiteORM.Attributes;

// Define entity
public class Task
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    public string Title { get; set; }
    public string Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime DueDate { get; set; }
}

// Setup
public class TaskPathProvider : IPathProvider
{
    public Dictionary<string, FileInfo> Paths { get; } = new()
    {
        ["Tasks"] = new FileInfo("tasks.db")
    };
}

// Usage
class Program
{
    static void Main()
    {
        var db = new DatabaseService(new TaskPathProvider());
        
        // Create tasks
        var task1 = new Task
        {
            Title = "Complete project",
            Description = "Finish the SimpleSQLiteORM documentation",
            IsCompleted = false,
            DueDate = DateTime.Now.AddDays(7)
        };
        
        db.Insert(task1, "Tasks");
        
        // Read all tasks
        var allTasks = db.Read<Task>("Tasks");
        
        foreach (var task in allTasks)
        {
            Console.WriteLine($"{task.Id}: {task.Title} - Due: {task.DueDate:yyyy-MM-dd}");
        }
        
        // Update task
        task1.IsCompleted = true;
        db.Update(task1, "Tasks");
        
        // Delete task
        db.Delete(task1, "Tasks");
    }
}
```

**Note**: This is a simple ORM designed for straightforward use cases. For complex applications with advanced requirements, consider using a more feature-rich ORM like Entity Framework Core or Dapper.