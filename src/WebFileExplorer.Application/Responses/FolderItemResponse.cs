using WebFileExplorer.Domain.FolderItems;

namespace WebFileExplorer.Application.Responses;

public sealed class FolderItemResponse
{
    required public FolderItemId Id { get; init; }

    required public string Name { get; init; }

    required public string RelativePath { get; init; }
}
