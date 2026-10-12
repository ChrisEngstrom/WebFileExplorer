using WebFileExplorer.Application.FileItems;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.Domain.FileItems;
using WebFileExplorer.Domain.FolderItems;

namespace WebFileExplorer.Infrastructure.FileItems;

internal class FileItemService : IFileItemService
{
    private readonly IFileItemRepository _fileItemRepository;

    public FileItemService(IFileItemRepository fileItemRepository)
    {
        _fileItemRepository = fileItemRepository;
    }

    public async Task<IEnumerable<FileItemResponse>> GetFileItemsFromFolder(FolderItemId folderItemId)
    {
        IEnumerable<FileItem> fileItems = await _fileItemRepository.GetFileItemsFromFolder(folderItemId);

        // Build the responses from the Domain object
        return fileItems.Select(fileItem => new FileItemResponse
        {
            Id = fileItem.Id,
            Name = fileItem.Name,
            RelativePath = fileItem.RelativePath,
            SizeBytes = fileItem.SizeBytes
        });
    }
}
