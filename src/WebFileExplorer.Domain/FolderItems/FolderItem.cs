namespace WebFileExplorer.Domain.FolderItems;

/// <summary>
/// Model that represents a folder in the file system.
/// </summary>
public sealed class FolderItem
{
    public FolderItem(string relativePath)
    {
        Id = new FolderItemId(relativePath);

        if (Id.Value.Length > 0 &&
            string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentException("Folder name cannot be blank.", nameof(relativePath));
        }
    }

    /// <summary>
    /// Gets the Id of the folder
    /// </summary>
    public FolderItemId Id { get; }

    /// <summary>
    /// Gets the name of the folder
    /// </summary>
    public string Name => Path.GetFileName(Id.Value);

    /// <summary>
    /// Gets the relative path of the folder to the root of the file system
    /// </summary>
    public string RelativePath => Id.Value;
}