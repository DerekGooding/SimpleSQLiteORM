namespace SimpleSQLiteORM;

/// <summary>
/// Provides a mapping of database names to their file system paths.
/// </summary>
public interface IPathProvider
{
    /// <summary>
    /// Gets the dictionary mapping database names to their corresponding FileInfo objects.
    /// </summary>
    Dictionary<string, FileInfo> Paths { get; }
}
