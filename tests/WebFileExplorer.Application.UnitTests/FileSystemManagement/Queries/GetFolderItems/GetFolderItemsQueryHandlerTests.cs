using Moq;
using WebFileExplorer.Application.Exceptions;
using WebFileExplorer.Application.FileSystemManagement.Queries.GetFolderItems;
using WebFileExplorer.Application.Interfaces;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.Domain.FolderItems;
using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Application.UnitTests.FileSystemManagement.Queries.GetFolderItems;

public class GetFolderItemsQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnFolderResponses_When_ServiceReturnsFolders()
    {
        // Arrange
        var mockFolderItemService = new Mock<IFolderItemService>();
        mockFolderItemService.Setup(x => x.GetFolderItemsFromRelativePath("Photos"))
            .ReturnsAsync(new List<FolderItemResponse>()
            {
                new FolderItemResponse
                {
                    Id = new FolderItemId("Photos/Again"),
                    Name = "Again",
                    RelativePath = "Photos/Again"
                }
            });

        var handler = new GetFolderItemsQueryHandler(mockFolderItemService.Object);

        // Act
        var result = await handler.Handle(new GetFolderItemsQuery("Photos"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Photos/Again", Assert.Single(result.Value).RelativePath);
        mockFolderItemService.Verify(
            x => x.GetFolderItemsFromRelativePath("Photos"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidPath_When_ServiceRejectsPath()
    {
        // Arrange
        var mockFolderItemService = new Mock<IFolderItemService>();
        mockFolderItemService.Setup(x => x.GetFolderItemsFromRelativePath("Photos"))
            .ThrowsAsync(new ArgumentException("Invalid path"));

        // Act
        var result = await new GetFolderItemsQueryHandler(mockFolderItemService.Object)
            .Handle(new GetFolderItemsQuery("Photos"), CancellationToken.None);

        // Assert
        Assert.Equal("Path.Invalid", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.True(result.IsFailure);
        mockFolderItemService.Verify(
            x => x.GetFolderItemsFromRelativePath("Photos"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_ServiceCannotFindFolder()
    {
        // Arrange
        var mockFolderItemService = new Mock<IFolderItemService>();
        mockFolderItemService.Setup(x => x.GetFolderItemsFromRelativePath("Photos"))
            .ThrowsAsync(new DirectoryNotFoundException());

        // Act
        var result = await new GetFolderItemsQueryHandler(mockFolderItemService.Object)
            .Handle(new GetFolderItemsQuery("Photos"), CancellationToken.None);

        // Assert
        Assert.Equal("Folder.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.True(result.IsFailure);
        mockFolderItemService.Verify(
            x => x.GetFolderItemsFromRelativePath("Photos"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnLimitExceeded_When_ServiceHitsScanLimit()
    {
        // Arrange
        var mockFolderItemService = new Mock<IFolderItemService>();
        mockFolderItemService.Setup(x => x.GetFolderItemsFromRelativePath("Photos"))
            .ThrowsAsync(new FileBrowserLimitExceededException("Too many"));

        // Act
        var result = await new GetFolderItemsQueryHandler(mockFolderItemService.Object)
            .Handle(new GetFolderItemsQuery("Photos"), CancellationToken.None);

        // Assert
        Assert.Equal("FileSystem.LimitExceeded", result.Error.Code);
        Assert.Equal(ErrorType.LimitExceeded, result.Error.Type);
        Assert.True(result.IsFailure);
        mockFolderItemService.Verify(
            x => x.GetFolderItemsFromRelativePath("Photos"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowArgumentNullException_When_RequestIsNull()
    {
        // Arrange
        var mockFolderItemService = new Mock<IFolderItemService>();
        var handler = new GetFolderItemsQueryHandler(mockFolderItemService.Object);

        // Act/Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(null!, CancellationToken.None));
        mockFolderItemService.VerifyNoOtherCalls();
    }
}
