using WebFileExplorer.Application.Responses;
using WebFileExplorer.Domain.FolderItems;

namespace WebFileExplorer.Application.FileItems;

public interface IFileItemService
{
    public Task<IEnumerable<FileItemResponse>> GetFileItemsFromFolder(FolderItemId folderItemId);
}
