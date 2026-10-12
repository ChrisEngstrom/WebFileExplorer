using Moq;
using WebFileExplorer.Application.Exceptions;
using WebFileExplorer.Application.FileSystemManagement.Commands.UploadFile;
using WebFileExplorer.Application.Interfaces;
using WebFileExplorer.Domain.FileItems;
using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Application.UnitTests.FileSystemManagement.Commands.UploadFile;

public class UploadCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_SaveStream_When_RequestIsValid()
    {
        // Arrange
        var mockFileContentRepository = new Mock<IFileContentRepository>();
        using var stream = new MemoryStream([7]);

        mockFileContentRepository.Setup(
            x => x.SaveNew(
                "Photos",
                "new.bin",
                stream,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileItem("Photos/new.bin", 1));

        // Act
        var result = await new UploadCommandHandler(mockFileContentRepository.Object)
            .Handle(
                new UploadCommand(
                    "Photos",
                    "new.bin",
                    stream),
                CancellationToken.None);

        // Assert
        mockFileContentRepository.Verify(
            x => x.SaveNew(
                "Photos",
                "new.bin",
                stream,
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_Should_ThrowArgumentNullException_When_RequestIsNull()
    {
        // Arrange
        var mockFileContentRepository = new Mock<IFileContentRepository>();
        var uploadCommandHandler = new UploadCommandHandler(mockFileContentRepository.Object);

        // Act/Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => uploadCommandHandler.Handle(null!, CancellationToken.None));
        mockFileContentRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_Should_ThrowArgumentNullException_When_StreamContentIsNull()
    {
        // Arrange
        var mockFileContentRepository = new Mock<IFileContentRepository>();

        // Simulates the repository's null-content guard.
#pragma warning disable S3928
        mockFileContentRepository.Setup(x =>
            x.SaveNew(
                "Photos",
                "new.bin",
                null!,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentNullException("content"));
#pragma warning restore S3928

        // Act/Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => new UploadCommandHandler(mockFileContentRepository.Object)
            .Handle(
                new UploadCommand(
                    "Photos",
                    "new.bin",
                    null!),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidPath_When_FileNameIsInvalid()
    {
        // Arrange
        var mockFileContentRepository = new Mock<IFileContentRepository>();
        using var stream = new MemoryStream([7]);

        mockFileContentRepository.Setup(x =>
            x.SaveNew(
                "Photos",
                "bad/name",
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("A file name cannot contain a path."));

        // Act
        Result result = await new UploadCommandHandler(mockFileContentRepository.Object)
            .Handle(
                new UploadCommand(
                    "Photos",
                    "bad/name",
                    stream),
                CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Path.Invalid", result.Error.Code);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_FolderIsMissing()
    {
        // Arrange
        var mockFileContentRepository = new Mock<IFileContentRepository>();
        using var stream = new MemoryStream([7]);

        mockFileContentRepository.Setup(
            x => x.SaveNew(
                "Gone",
                "new.bin",
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DirectoryNotFoundException());

        // Act
        var result = await new UploadCommandHandler(mockFileContentRepository.Object)
            .Handle(
                new UploadCommand(
                    "Gone",
                    "new.bin",
                    stream),
                CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Folder.NotFound", result.Error.Code);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyExists_When_FileAlreadyExists()
    {
        // Arrange
        var mockFileContentRepository = new Mock<IFileContentRepository>();
        using var stream = new MemoryStream([7]);

        mockFileContentRepository.Setup(x =>
            x.SaveNew(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileAlreadyExistsException());

        // Act
        var result = await new UploadCommandHandler(mockFileContentRepository.Object)
            .Handle(
                new UploadCommand(
                    "Photos",
                    "new.bin",
                    stream),
                CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("File.Exists", result.Error.Code);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Should_ReturnLimitExceeded_When_RepositoryRejectsSize()
    {
        // Arrange
        var mockFileContentRepository = new Mock<IFileContentRepository>();
        using var stream = new MemoryStream([7]);

        mockFileContentRepository.Setup(x =>
            x.SaveNew(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileBrowserLimitExceededException("upload too large"));

        // Act
        var result = await new UploadCommandHandler(mockFileContentRepository.Object)
            .Handle(
                new UploadCommand(
                    "Photos",
                    "new.bin",
                    stream),
                CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.LimitExceeded, result.Error.Type);
        Assert.Equal("FileSystem.LimitExceeded", result.Error.Code);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Should_ThrowIOException_When_FailureIsUnexpected()
    {
        // Arrange
        var mockFileContentRepository = new Mock<IFileContentRepository>();
        using var stream = new MemoryStream([7]);

        mockFileContentRepository.Setup(x =>
            x.SaveNew(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("disk failed"));

        // Act/Assert
        await Assert.ThrowsAsync<IOException>(() => new UploadCommandHandler(mockFileContentRepository.Object)
            .Handle(
                new UploadCommand(
                    "Photos",
                    "new.bin",
                    stream),
                CancellationToken.None));
    }
}
