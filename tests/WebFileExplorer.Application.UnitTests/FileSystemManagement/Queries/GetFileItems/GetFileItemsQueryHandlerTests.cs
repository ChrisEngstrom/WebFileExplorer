using Moq;
using WebFileExplorer.Application.Exceptions;
using WebFileExplorer.Application.FileItems;
using WebFileExplorer.Application.FileSystemManagement.Queries.GetFileItems;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.Domain.FolderItems;
using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Application.UnitTests.FileSystemManagement.Queries.GetFileItems;

public class GetFileItemsQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnFileResponses_When_ServiceReturnsFiles()
    {
        // Arrange
        var mockFileItemService = new Mock<IFileItemService>();
        mockFileItemService.Setup(x => x.GetFileItemsFromFolder(new FolderItemId("Photos")))
            .ReturnsAsync(new List<FileItemResponse>()
            {
                new FileItemResponse
                {
                    Id = new ("Photos/photo.bin"),
                    Name = "photo.bin",
                    RelativePath = "Photos/photo.bin",
                    SizeBytes = 3
                }
            });

        var handler = new GetFileItemsQueryHandler(mockFileItemService.Object);

        // Act
        var result = await handler.Handle(
            new GetFileItemsQuery(new FolderItemId("Photos")),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Photos/photo.bin", Assert.Single(result.Value).RelativePath);
        mockFileItemService.Verify(
            x => x.GetFileItemsFromFolder(new FolderItemId("Photos")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidPath_When_ServiceRejectsPath()
    {
        // Arrange
        var mockFileItemService = new Mock<IFileItemService>();
        mockFileItemService.Setup(x => x.GetFileItemsFromFolder(new FolderItemId("Photos")))
            .ThrowsAsync(new ArgumentException("Invalid path"));

        // Act
        var result = await new GetFileItemsQueryHandler(mockFileItemService.Object)
            .Handle(new GetFileItemsQuery(new FolderItemId("Photos")), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Path.Invalid", result.Error.Code);
        mockFileItemService.Verify(
            x => x.GetFileItemsFromFolder(new FolderItemId("Photos")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_ServiceCannotFindFolder()
    {
        // Arrange
        var mockFileItemService = new Mock<IFileItemService>();
        mockFileItemService.Setup(x => x.GetFileItemsFromFolder(new FolderItemId("Photos")))
            .ThrowsAsync(new DirectoryNotFoundException());

        // Act
        var result = await new GetFileItemsQueryHandler(mockFileItemService.Object)
            .Handle(new GetFileItemsQuery(new FolderItemId("Photos")), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Folder.NotFound", result.Error.Code);
        mockFileItemService.Verify(
            x => x.GetFileItemsFromFolder(new FolderItemId("Photos")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnLimitExceeded_When_ServiceHitsScanLimit()
    {
        // Arrange
        var mockFileItemService = new Mock<IFileItemService>();
        mockFileItemService.Setup(x => x.GetFileItemsFromFolder(new FolderItemId("Photos")))
            .ThrowsAsync(new FileBrowserLimitExceededException("Too many items"));

        // Act
        var result = await new GetFileItemsQueryHandler(mockFileItemService.Object)
            .Handle(new GetFileItemsQuery(new FolderItemId("Photos")), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.LimitExceeded, result.Error.Type);
        Assert.Equal("FileSystem.LimitExceeded", result.Error.Code);
        mockFileItemService.Verify(
            x => x.GetFileItemsFromFolder(new FolderItemId("Photos")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowArgumentNullException_When_RequestIsNull()
    {
        // Arrange
        var mockFileItemService = new Mock<IFileItemService>();
        var handler = new GetFileItemsQueryHandler(mockFileItemService.Object);

        // Act/Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(null!, CancellationToken.None));
        mockFileItemService.VerifyNoOtherCalls();
    }
}
