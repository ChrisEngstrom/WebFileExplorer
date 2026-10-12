using WebFileExplorer.Domain.FolderItems;

namespace WebFileExplorer.Domain.UnitTests.FolderItems;

public class FolderItemIdTests
{
    [Fact]
    public void Constructor_Should_AcceptEmptyPath_When_FolderIsRoot()
    {
        // Arrange
        var rootFolderItemIdValue = string.Empty;

        // Act
        var folderItemId = new FolderItemId(rootFolderItemIdValue);

        // Assert
        Assert.Equal(rootFolderItemIdValue, folderItemId.Value);
    }

    [Fact]
    public void Constructor_Should_PreserveRelativePath_When_PathIsValid()
    {
        // Arrange
        var folderItemIdValue = "Photos/Again";

        // Act
        var folderItemId = new FolderItemId(folderItemIdValue);

        // Assert
        Assert.Equal(folderItemIdValue, folderItemId.Value);
    }
}
