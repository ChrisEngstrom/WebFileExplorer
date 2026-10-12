using WebFileExplorer.Domain.FileItems;

namespace WebFileExplorer.Application.Responses;

public sealed class FileItemResponse
{
    required public FileItemId Id { get; init; }

    required public string Name { get; init; }

    required public string RelativePath { get; init; }

    public long SizeBytes { get; init; }
}
