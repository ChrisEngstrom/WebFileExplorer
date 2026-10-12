using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.Errors;
using WebFileExplorer.Application.Exceptions;
using WebFileExplorer.Application.FileItems;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Application.FileSystemManagement.Queries.GetFileItems;

internal sealed class GetFileItemsQueryHandler
    : IQueryHandler<GetFileItemsQuery, IReadOnlyList<FileItemResponse>>
{
    private readonly IFileItemService _fileItemService;

    public GetFileItemsQueryHandler(IFileItemService fileItemService)
    {
        _fileItemService = fileItemService;
    }

    public async Task<Result<IReadOnlyList<FileItemResponse>>> Handle(GetFileItemsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Result<IReadOnlyList<FileItemResponse>> result;
        try
        {
            IEnumerable<FileItemResponse> fileItems = await _fileItemService.GetFileItemsFromFolder(request.folderItemId);

            result = fileItems.ToList();
        }
        catch (ArgumentException)
        {
            result = Result.Failure<IReadOnlyList<FileItemResponse>>(FileSystemErrors.InvalidPath);
        }
        catch (DirectoryNotFoundException)
        {
            result = Result.Failure<IReadOnlyList<FileItemResponse>>(FileSystemErrors.FolderNotFound);
        }
        catch (FileBrowserLimitExceededException)
        {
            result = Result.Failure<IReadOnlyList<FileItemResponse>>(FileSystemErrors.LimitExceeded);
        }

        return result;
    }
}
