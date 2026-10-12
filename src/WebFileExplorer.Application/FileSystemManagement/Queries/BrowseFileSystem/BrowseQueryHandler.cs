using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.Configurations;
using WebFileExplorer.Application.Errors;
using WebFileExplorer.Application.FileSystemManagement.Queries.GetFileItems;
using WebFileExplorer.Application.FileSystemManagement.Queries.GetFolderItems;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.Domain.FolderItems;
using WebFileExplorer.Domain.Paths;
using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Application.FileSystemManagement.Queries.BrowseFileSystem;

internal sealed class BrowseQueryHandler
    : IQueryHandler<BrowseQuery, FileSystemItemsResponse>
{
    private readonly IQueryHandler<GetFolderItemsQuery, IReadOnlyList<FolderItemResponse>> _folderItems;
    private readonly IQueryHandler<GetFileItemsQuery, IReadOnlyList<FileItemResponse>> _fileItems;
    private readonly AppConfiguration _configuration;

    public BrowseQueryHandler(
        IQueryHandler<GetFolderItemsQuery, IReadOnlyList<FolderItemResponse>> folderItems,
        IQueryHandler<GetFileItemsQuery, IReadOnlyList<FileItemResponse>> fileItems,
        AppConfiguration configuration)
    {
        _folderItems = folderItems;
        _fileItems = fileItems;
        _configuration = configuration;
    }

    public async Task<Result<FileSystemItemsResponse>> Handle(BrowseQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            RelativePathRules.Validate(request.Path, allowRoot: true);
        }
        catch (ArgumentException)
        {
            return Result.Failure<FileSystemItemsResponse>(FileSystemErrors.InvalidPath);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Get the immediate child folders from the requested path, doesn't traverse down
        Result<IReadOnlyList<FolderItemResponse>> folderResult =
            await _folderItems.Handle(new GetFolderItemsQuery(request.Path), cancellationToken);

        if (folderResult.IsFailure)
        {
            return Result.Failure<FileSystemItemsResponse>(folderResult.Error);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Get the files from the requested path
        Result<IReadOnlyList<FileItemResponse>> fileResult =
            await _fileItems.Handle(new GetFileItemsQuery(new FolderItemId(request.Path)), cancellationToken);

        if (fileResult.IsFailure)
        {
            return Result.Failure<FileSystemItemsResponse>(fileResult.Error);
        }

        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<FolderItemResponse> folders = folderResult.Value;
        IReadOnlyList<FileItemResponse> files = fileResult.Value;
        var fileSystemItems = new List<FileSystemItemResponse>();
        int scanned = 0;
        long totalSizeBytes = 0;

        // Add each folder to the response
        foreach (FolderItemResponse folder in folders)
        {
            if (++scanned > _configuration.MaxScannedItems)
            {
                return Result.Failure<FileSystemItemsResponse>(FileSystemErrors.LimitExceeded);
            }

            fileSystemItems.Add(new FileSystemItemResponse(folder.Name, folder.RelativePath, true, null));
        }

        // Add each file to the response
        foreach (FileItemResponse file in files)
        {
            if (++scanned > _configuration.MaxScannedItems)
            {
                return Result.Failure<FileSystemItemsResponse>(FileSystemErrors.LimitExceeded);
            }

            totalSizeBytes = checked(totalSizeBytes + file.SizeBytes);
            fileSystemItems.Add(new FileSystemItemResponse(file.Name, file.RelativePath, false, file.SizeBytes));
        }

        // Sort to show folders first, then files
        //  Each group is then sorted by relative path
        fileSystemItems.Sort((left, right) =>
        {
            // Reversed comparison puts directories before files
            int folderOrder = right.IsDirectory.CompareTo(left.IsDirectory);

            // If they are not the same type, put the folder first, else alphabetical
            return folderOrder != 0
                ? folderOrder
                : StringComparer.OrdinalIgnoreCase.Compare(left.RelativePath, right.RelativePath);
        });

        return new FileSystemItemsResponse(
            request.Path,
            fileSystemItems,
            fileSystemItems.Count(item => item.IsDirectory),
            fileSystemItems.Count(item => !item.IsDirectory),
            totalSizeBytes);
    }
}
