using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.Domain.FolderItems;

namespace WebFileExplorer.Application.FileSystemManagement.Queries.GetFileItems;

public sealed record GetFileItemsQuery(FolderItemId folderItemId)
    : IQuery<IReadOnlyList<FileItemResponse>>;
