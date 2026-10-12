using WebFileExplorer.Domain.FileItems;

namespace WebFileExplorer.Application.Interfaces;

/// <summary>
/// Interface for a file content repository that enables streaming upload/download use cases
/// </summary>
public interface IFileContentRepository
{
    Task<FileItem> SaveNew(
        string directory,
        string fileName,
        Stream content,
        CancellationToken cancellationToken);

    Stream OpenRead(FileItemId fileItemId);
}
