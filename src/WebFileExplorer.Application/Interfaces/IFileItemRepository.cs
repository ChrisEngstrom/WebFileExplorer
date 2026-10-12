using WebFileExplorer.Domain.FileItems;
using WebFileExplorer.Domain.FolderItems;

namespace WebFileExplorer.Application.FileItems;

public interface IFileItemRepository
{
    Task<IEnumerable<FileItem>> GetFileItemsFromFolder(FolderItemId folderItemId);

    Task<FileItem?> GetById(FileItemId fileItemId);
}
