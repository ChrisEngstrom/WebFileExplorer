using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.Configurations;
using WebFileExplorer.Application.Errors;
using WebFileExplorer.Application.FileSystemManagement.Queries.BrowseFileSystem;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.Domain.Paths;
using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Application.FileSystemManagement.Queries.SearchFileSystem;

internal sealed class SearchQueryHandler
    : IQueryHandler<SearchQuery, FileSystemItemsResponse>
{
    private const int MAX_SEARCH_LENGTH = 200;

    private readonly IQueryHandler<BrowseQuery, FileSystemItemsResponse> _browse;
    private readonly AppConfiguration _configuration;

    public SearchQueryHandler(IQueryHandler<BrowseQuery, FileSystemItemsResponse> browse, AppConfiguration configuration)
    {
        _browse = browse;
        _configuration = configuration;
    }

    public async Task<Result<FileSystemItemsResponse>> Handle(SearchQuery request, CancellationToken cancellationToken)
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

        if (request.Search is { Length: > MAX_SEARCH_LENGTH })
        {
            return Result.Failure<FileSystemItemsResponse>(FileSystemErrors.SearchTooLong);
        }

        // A blank search is an ordinary one-level browse.
        if (string.IsNullOrWhiteSpace(request.Search))
        {
            return await _browse.Handle(new BrowseQuery(request.Path), cancellationToken);
        }

        string searchTerm = request.Search.Trim();
        var fileSystemItems = new List<FileSystemItemResponse>();

        // List of root-relative folders that still need to be processed; stack used to avoid recursive calls
        var pendingFolders = new Stack<string>();
        pendingFolders.Push(request.Path);

        int scanned = 0;
        long totalSizeBytes = 0;
        while (pendingFolders.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string folderPath = pendingFolders.Pop();
            Result<FileSystemItemsResponse> folderResult =
                await _browse.Handle(new BrowseQuery(folderPath), cancellationToken);

            if (folderResult.IsFailure)
            {
                return Result.Failure<FileSystemItemsResponse>(folderResult.Error);
            }

            cancellationToken.ThrowIfCancellationRequested();

            foreach (FileSystemItemResponse item in folderResult.Value.Items)
            {
                // Count inspected items, not just matches, to bound the entire search.
                if (++scanned > _configuration.MaxScannedItems)
                {
                    return Result.Failure<FileSystemItemsResponse>(FileSystemErrors.LimitExceeded);
                }

                if (item.IsDirectory)
                {
                    // Add child folders to be processed to find matches
                    pendingFolders.Push(item.RelativePath);
                }

                if (!item.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                fileSystemItems.Add(item);

                // Update the current folder's immediate total byte size, not including recursive folder size
                if (!item.IsDirectory)
                {
                    totalSizeBytes = checked(totalSizeBytes + item.SizeBytes!.Value);
                }
            }
        }

        // Sort to show folders first, then files
        //  Each group is then sorted by relative path
        //  Display order should not depend on the stack's depth-first visitation order
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