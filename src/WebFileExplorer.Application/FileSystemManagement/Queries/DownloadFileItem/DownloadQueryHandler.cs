using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.Errors;
using WebFileExplorer.Application.FileItems;
using WebFileExplorer.Application.Interfaces;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.Domain.FileItems;
using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Application.FileSystemManagement.Queries.DownloadFileItem;

internal sealed class DownloadQueryHandler
    : IQueryHandler<DownloadQuery, DownloadResponse>
{
    private readonly IFileItemRepository _fileItemRepository;
    private readonly IFileContentRepository _fileContentRepository;

    public DownloadQueryHandler(
        IFileItemRepository fileItemRepository,
        IFileContentRepository fileContentRepository)
    {
        _fileItemRepository = fileItemRepository;
        _fileContentRepository = fileContentRepository;
    }

    public async Task<Result<DownloadResponse>> Handle(DownloadQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        Result<DownloadResponse> result;
        try
        {
            var fileItemId = new FileItemId(request.Path);

            FileItem? fileItem = await _fileItemRepository.GetById(fileItemId);

            if (fileItem is null)
            {
                return Result.Failure<DownloadResponse>(FileSystemErrors.FileNotFound);
            }

            result = new DownloadResponse(_fileContentRepository.OpenRead(fileItem.Id), fileItem.Name);
        }
        catch (ArgumentException)
        {
            result = Result.Failure<DownloadResponse>(FileSystemErrors.InvalidPath);
        }
        catch (FileNotFoundException)
        {
            result = Result.Failure<DownloadResponse>(FileSystemErrors.FileNotFound);
        }
        catch (DirectoryNotFoundException)
        {
            result = Result.Failure<DownloadResponse>(FileSystemErrors.FileNotFound);
        }

        return result;
    }
}
