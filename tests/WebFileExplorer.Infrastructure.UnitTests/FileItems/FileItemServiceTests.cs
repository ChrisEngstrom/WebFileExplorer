using Moq;
using WebFileExplorer.Application.FileItems;
using WebFileExplorer.Domain.FileItems;
using WebFileExplorer.Domain.FolderItems;
using WebFileExplorer.Infrastructure.FileItems;

namespace WebFileExplorer.Infrastructure.UnitTests.FileItems;

public class FileItemServiceTests
{
    [Fact]
    public async Task GetFileItemsFromFolder_Should_ProjectEntityProperties_When_RepositoryReturnsItems()
    {
        // Arrange
        var mockFileItemRepository = new Mock<IFileItemRepository>();
        var fileItem = new FileItem("Photos/photo.bin", 3);
        mockFileItemRepository.Setup(
            x => x.GetFileItemsFromFolder(
                new FolderItemId("Photos")))
            .ReturnsAsync(new List<FileItem> { fileItem });

        var fileItemService = new FileItemService(mockFileItemRepository.Object);

        // Act
        var result = (await fileItemService.GetFileItemsFromFolder(new FolderItemId("Photos"))).ToArray();

        // Assert
        var response = Assert.Single(result);
        Assert.Equal(fileItem.Id, response.Id);
        Assert.Equal("photo.bin", response.Name);
        Assert.Equal("Photos/photo.bin", response.RelativePath);
        Assert.Equal(3, response.SizeBytes);
        mockFileItemRepository.Verify(
            x => x.GetFileItemsFromFolder(new FolderItemId("Photos")),
            Times.Once);
    }

    [Fact]
    public async Task GetFileItemsFromFolder_Should_ReturnEmptyItems_When_RepositoryReturnsNoItems()
    {
        // Arrange
        var mockFileItemRepository = new Mock<IFileItemRepository>();
        mockFileItemRepository.Setup(
            x => x.GetFileItemsFromFolder(
                new FolderItemId("Photos")))
            .ReturnsAsync(new List<FileItem>());

        var fileItemService = new FileItemService(mockFileItemRepository.Object);

        // Act
        var result = await fileItemService.GetFileItemsFromFolder(new FolderItemId("Photos"));

        // Assert
        Assert.Empty(result);
        mockFileItemRepository.Verify(
            x => x.GetFileItemsFromFolder(new FolderItemId("Photos")),
            Times.Once);
    }

    [Fact]
    public async Task GetFileItemsFromFolder_Should_PropagateException_When_RepositoryFails()
    {
        // Arrange
        var exception = new IOException("Repository failure.");
        var mockFileItemRepository = new Mock<IFileItemRepository>();
        mockFileItemRepository.Setup(
            x => x.GetFileItemsFromFolder(
                new FolderItemId("Photos")))
            .ThrowsAsync(exception);

        var fileItemService = new FileItemService(mockFileItemRepository.Object);

        // Act/Assert
        var actual = await Assert.ThrowsAsync<IOException>(
            () => fileItemService.GetFileItemsFromFolder(new FolderItemId("Photos")));
        Assert.Same(exception, actual);
    }
}
