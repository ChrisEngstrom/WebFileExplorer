using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.Responses;

namespace WebFileExplorer.Application.FileSystemManagement.Queries.DownloadFileItem;

public sealed record DownloadQuery(string Path)
    : IQuery<DownloadResponse>;
