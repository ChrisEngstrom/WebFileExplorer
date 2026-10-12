using WebFileExplorer.Application.Interfaces;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.Domain.FolderItems;

namespace WebFileExplorer.Infrastructure.FolderItems;

internal class FolderItemService : IFolderItemService
{
    private readonly IFolderItemRepository _folderItemRepository;

    public FolderItemService(IFolderItemRepository folderItemRepository)
    {
        _folderItemRepository = folderItemRepository;
    }

    public async Task<IEnumerable<FolderItemResponse>> GetFolderItemsFromRelativePath(string relativePath)
    {
        IEnumerable<FolderItem> folderItems = await _folderItemRepository.GetFolderItemsFromRelativePath(relativePath);

        // Build the responses from the Domain objects
        return folderItems.Select(folderItem => new FolderItemResponse
        {
            Id = folderItem.Id,
            Name = folderItem.Name,
            RelativePath = folderItem.RelativePath
        });
    }
}
