using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.Responses;

namespace WebFileExplorer.Application.FileSystemManagement.Queries.BrowseFileSystem;

public sealed record BrowseQuery(string Path)
    : IQuery<FileSystemItemsResponse>;
