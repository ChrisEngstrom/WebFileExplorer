using WebFileExplorer.Application.Interfaces;
using WebFileExplorer.Domain.FolderItems;
using WebFileExplorer.Infrastructure.FileSystem;

namespace WebFileExplorer.Infrastructure.FolderItems;

internal class FolderItemRepository : IFolderItemRepository
{
    private readonly PhysicalFileSystem _fileSystem;

    public FolderItemRepository(PhysicalFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public Task<IEnumerable<FolderItem>> GetFolderItemsFromRelativePath(string relativePath)
    {
        // Load the child directories into memory
        IEnumerable<FolderItem> folderItems = _fileSystem.Items(relativePath, CancellationToken.None)
            .Where(Directory.Exists)
            .Select(path => new FolderItem(_fileSystem.Relative(path)))
            .OrderBy(folderItem => folderItem.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult(folderItems);
    }

    public Task<FolderItem?> GetById(FolderItemId folderItemId)
    {
        string path = _fileSystem.Resolve(folderItemId.Value);

        FolderItem? folderItem = Directory.Exists(path)
            ? new FolderItem(folderItemId.Value)
            : null;

        return Task.FromResult(folderItem);
    }
}
