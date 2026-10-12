using WebFileExplorer.Application.Responses;

namespace WebFileExplorer.Application.Interfaces;

public interface IFolderItemService
{
    public Task<IEnumerable<FolderItemResponse>> GetFolderItemsFromRelativePath(string relativePath);
}
