using Moq;
using WebFileExplorer.Application.Interfaces;
using WebFileExplorer.Domain.FolderItems;
using WebFileExplorer.Infrastructure.FolderItems;

namespace WebFileExplorer.Infrastructure.UnitTests.FolderItems;

public class FolderItemServiceTests
{
    [Fact]
    public async Task GetFolderItemsFromRelativePath_Should_ProjectEntityProperties_When_RepositoryReturnsItems()
    {
        // Arrange
        var mockFolderItemRepository = new Mock<IFolderItemRepository>();
        var folderItem = new FolderItem("Photos/Again");
        mockFolderItemRepository.Setup(
            x => x.GetFolderItemsFromRelativePath(
                "Photos"))
            .ReturnsAsync(new List<FolderItem> { folderItem });

        var folderItemService = new FolderItemService(mockFolderItemRepository.Object);

        // Act
        var result = (await folderItemService.GetFolderItemsFromRelativePath("Photos")).ToArray();

        // Assert
        var response = Assert.Single(result);
        Assert.Equal(folderItem.Id, response.Id);
        Assert.Equal("Again", response.Name);
        Assert.Equal("Photos/Again", response.RelativePath);
        mockFolderItemRepository.Verify(
            x => x.GetFolderItemsFromRelativePath("Photos"),
            Times.Once);
    }

    [Fact]
    public async Task GetFolderItemsFromRelativePath_Should_ReturnEmptyItems_When_RepositoryReturnsNoItems()
    {
        // Arrange
        var mockFolderItemRepository = new Mock<IFolderItemRepository>();
        mockFolderItemRepository.Setup(
            x => x.GetFolderItemsFromRelativePath(
                "Photos"))
            .ReturnsAsync(new List<FolderItem>());

        var folderItemService = new FolderItemService(mockFolderItemRepository.Object);

        // Act
        var result = await folderItemService.GetFolderItemsFromRelativePath("Photos");

        // Assert
        Assert.Empty(result);
        mockFolderItemRepository.Verify(
            x => x.GetFolderItemsFromRelativePath("Photos"),
            Times.Once);
    }

    [Fact]
    public async Task GetFolderItemsFromRelativePath_Should_PropagateException_When_RepositoryFails()
    {
        // Arrange
        var exception = new IOException("Repository failure.");
        var mockFolderItemRepository = new Mock<IFolderItemRepository>();
        mockFolderItemRepository.Setup(
            x => x.GetFolderItemsFromRelativePath(
                "Photos"))
            .ThrowsAsync(exception);

        var folderItemService = new FolderItemService(mockFolderItemRepository.Object);

        // Act/Assert
        var actual = await Assert.ThrowsAsync<IOException>(
            () => folderItemService.GetFolderItemsFromRelativePath("Photos"));
        Assert.Same(exception, actual);
    }
}
