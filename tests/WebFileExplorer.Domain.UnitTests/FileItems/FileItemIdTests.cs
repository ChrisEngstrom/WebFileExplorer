using WebFileExplorer.Domain.FileItems;

namespace WebFileExplorer.Domain.UnitTests.FileItems;

public class FileItemIdTests
{
    [Fact]
    public void Constructor_Should_PreserveRelativePath_When_PathIsValid()
    {
        // Arrange
        var fileItemIdValue = "Photos/sample.txt";

        // Act
        var fileItemId = new FileItemId(fileItemIdValue);

        // Assert
        Assert.Equal(fileItemIdValue, fileItemId.Value);
    }
}
