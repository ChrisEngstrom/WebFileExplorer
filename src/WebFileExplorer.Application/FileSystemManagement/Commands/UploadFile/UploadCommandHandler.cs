using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.Errors;
using WebFileExplorer.Application.Exceptions;
using WebFileExplorer.Application.Interfaces;
using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Application.FileSystemManagement.Commands.UploadFile;

internal sealed class UploadCommandHandler : ICommandHandler<UploadCommand>
{
    private readonly IFileContentRepository _fileContentRepository;

    public UploadCommandHandler(IFileContentRepository fileContentRepository)
    {
        _fileContentRepository = fileContentRepository;
    }

    public async Task<Result> Handle(UploadCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Result result;
        try
        {
            await _fileContentRepository.SaveNew(
                request.RelativeFolderPath,
                request.FileName,
                request.Content,
                cancellationToken);

            result = Result.Success();
        }
        catch (ArgumentException exception) when (exception is not ArgumentNullException)
        {
            result = Result.Failure(FileSystemErrors.InvalidPath);
        }
        catch (DirectoryNotFoundException)
        {
            result = Result.Failure(FileSystemErrors.FolderNotFound);
        }
        catch (FileAlreadyExistsException)
        {
            result = Result.Failure(FileSystemErrors.FileExists);
        }
        catch (FileBrowserLimitExceededException)
        {
            result = Result.Failure(FileSystemErrors.LimitExceeded);
        }

        return result;
    }
}
