namespace WebFileExplorer.Domain.FileItems;

/// <summary>
/// Model that represents a file in the file system
/// </summary>
public sealed class FileItem
{
    public FileItem(string relativePath, long sizeBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeBytes);

        Id = new FileItemId(relativePath);
        SizeBytes = sizeBytes;

        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentException("File name cannot be blank.", nameof(relativePath));
        }
    }

    /// <summary>
    /// Gets the Id of the file
    /// </summary>
    public FileItemId Id { get; }

    /// <summary>
    /// Gets the name of the file
    /// </summary>
    public string Name => Path.GetFileName(Id.Value);

    /// <summary>
    /// Gets the relative path of the file to the root of the file system
    /// </summary>
    public string RelativePath => Id.Value;

    /// <summary>
    /// Gets the file's size in bytes
    /// </summary>
    public long SizeBytes { get; }
}
