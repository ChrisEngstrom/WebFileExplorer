using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.Errors;
using WebFileExplorer.Application.Exceptions;
using WebFileExplorer.Application.Interfaces;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Application.FileSystemManagement.Queries.GetFolderItems;

internal sealed class GetFolderItemsQueryHandler
    : IQueryHandler<GetFolderItemsQuery, IReadOnlyList<FolderItemResponse>>
{
    private readonly IFolderItemService _folderItemService;

    public GetFolderItemsQueryHandler(IFolderItemService folderItemService)
    {
        _folderItemService = folderItemService;
    }

    public async Task<Result<IReadOnlyList<FolderItemResponse>>> Handle(GetFolderItemsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Result<IReadOnlyList<FolderItemResponse>> result;
        try
        {
            IEnumerable<FolderItemResponse> folderItems = await _folderItemService.GetFolderItemsFromRelativePath(request.RelativeFolderPath);

            result = folderItems.ToList();
        }
        catch (ArgumentException)
        {
            result = Result.Failure<IReadOnlyList<FolderItemResponse>>(FileSystemErrors.InvalidPath);
        }
        catch (DirectoryNotFoundException)
        {
            result = Result.Failure<IReadOnlyList<FolderItemResponse>>(FileSystemErrors.FolderNotFound);
        }
        catch (FileBrowserLimitExceededException)
        {
            result = Result.Failure<IReadOnlyList<FolderItemResponse>>(FileSystemErrors.LimitExceeded);
        }

        return result;
    }
}
