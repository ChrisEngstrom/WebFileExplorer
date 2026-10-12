using WebFileExplorer.Domain.FolderItems;

namespace WebFileExplorer.Application.Interfaces;

public interface IFolderItemRepository
{
    Task<IEnumerable<FolderItem>> GetFolderItemsFromRelativePath(string relativePath);

    Task<FolderItem?> GetById(FolderItemId folderItemId);
}
