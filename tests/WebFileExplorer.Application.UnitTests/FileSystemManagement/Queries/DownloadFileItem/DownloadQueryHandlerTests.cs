using Moq;
using WebFileExplorer.Application.FileItems;
using WebFileExplorer.Application.FileSystemManagement.Queries.DownloadFileItem;
using WebFileExplorer.Application.Interfaces;
using WebFileExplorer.Domain.FileItems;
using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Application.UnitTests.FileSystemManagement.Queries.DownloadFileItem;

public class DownloadQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnContent_When_FileExists()
    {
        // Arrange
        using var output = new MemoryStream();
        var mockFileContentRepository = new Mock<IFileContentRepository>();
        var mockFileItemRepository = new Mock<IFileItemRepository>();

        mockFileItemRepository.Setup(x => x.GetById(new FileItemId("Photos/photo.bin")))
            .ReturnsAsync(new FileItem("Photos/photo.bin", 3));
        mockFileContentRepository.Setup(x => x.OpenRead(new FileItemId("Photos/photo.bin")))
            .Returns(new MemoryStream([1, 2, 3]));

        // Act
        var result = await new DownloadQueryHandler(
                mockFileItemRepository.Object,
                mockFileContentRepository.Object)
            .Handle(new DownloadQuery("Photos/photo.bin"), CancellationToken.None);

        await result.Value.Content.CopyToAsync(output, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("photo.bin", result.Value.Name);
        Assert.Equal([1, 2, 3], output.ToArray());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_FileIsMissing()
    {
        // Arrange
        var mockFileItemRepository = new Mock<IFileItemRepository>();
        var mockFileContentRepository = new Mock<IFileContentRepository>();
        var downloadQueryHandler = new DownloadQueryHandler(mockFileItemRepository.Object, mockFileContentRepository.Object);

        // Act
        var result = await downloadQueryHandler.Handle(new DownloadQuery("gone.bin"), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("File.NotFound", result.Error.Code);
        mockFileItemRepository.Verify(
            x => x.GetById(new FileItemId("gone.bin")),
            Times.Once);
        mockFileContentRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidPath_When_PathIsUnsafe()
    {
        // Arrange
        var mockFileItemRepository = new Mock<IFileItemRepository>();
        var mockFileContentRepository = new Mock<IFileContentRepository>();
        var downloadQueryHandler = new DownloadQueryHandler(mockFileItemRepository.Object, mockFileContentRepository.Object);

        // Act
        var result = await downloadQueryHandler.Handle(new DownloadQuery("Photos/../secret.txt"), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Path.Invalid", result.Error.Code);
        mockFileItemRepository.VerifyNoOtherCalls();
        mockFileContentRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_ContentDisappearsAfterInitialLookup()
    {
        // Arrange
        var mockFileItemRepository = new Mock<IFileItemRepository>();
        mockFileItemRepository.Setup(x => x.GetById(new FileItemId("Photos/photo.bin")))
            .ReturnsAsync(new FileItem("Photos/photo.bin", 3));

        foreach (Exception exception in new List<Exception>()
            {
                new FileNotFoundException(),
                new DirectoryNotFoundException()
            })
        {
            var mockFileContentRepository = new Mock<IFileContentRepository>();
            mockFileContentRepository.Setup(x => x.OpenRead(new FileItemId("Photos/photo.bin")))
                .Throws(exception);

            // Act
            var result = await new DownloadQueryHandler(
                mockFileItemRepository.Object,
                mockFileContentRepository.Object)
                .Handle(new DownloadQuery("Photos/photo.bin"), CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.NotFound, result.Error.Type);
            Assert.Equal("File.NotFound", result.Error.Code);
        }
    }
}
