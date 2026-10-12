namespace WebFileExplorer.Application.Responses;

public sealed record FileSystemItemsResponse(
    string Path,
    IReadOnlyList<FileSystemItemResponse> Items,
    int FolderCount,
    int FileCount,
    long TotalSizeBytes);
