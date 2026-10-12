using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.Responses;

namespace WebFileExplorer.Application.FileSystemManagement.Queries.GetFolderItems;

public sealed record GetFolderItemsQuery(string RelativeFolderPath)
    : IQuery<IReadOnlyList<FolderItemResponse>>;
