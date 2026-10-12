namespace WebFileExplorer.Application.Responses;

public sealed record FileSystemItemResponse(
    string Name,
    string RelativePath,
    bool IsDirectory,
    long? SizeBytes);
