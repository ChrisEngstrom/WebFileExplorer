using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.Responses;

namespace WebFileExplorer.Application.FileSystemManagement.Queries.SearchFileSystem;

public sealed record SearchQuery(string Path, string Search)
    : IQuery<FileSystemItemsResponse>;
