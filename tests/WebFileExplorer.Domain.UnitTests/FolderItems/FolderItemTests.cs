using FluentAssertions;
using WebFileExplorer.Domain.FolderItems;

namespace WebFileExplorer.Domain.UnitTests.FolderItems;

public class FolderItemTests
{
    [Fact]
    public void Constructor_Should_SetPropertyValues()
    {
        // Arrange
        var folderRelativePath = "root/TestFolder";
        var folderName = "TestFolder";

        // Act
        var folderItem = new FolderItem(folderRelativePath);

        // Assert
        folderItem.Id.Should().Be(new FolderItemId(folderRelativePath));
        folderItem.Name.Should().Be(folderName);
        folderItem.RelativePath.Should().Be(folderRelativePath);
    }

    [Fact]
    public void Constructor_Should_LeaveNameEmpty_When_FolderIsRoot()
    {
        // Arrange
        var rootFolderPath = string.Empty;

        // Act/Assert
        Assert.Equal(rootFolderPath, new FolderItem(rootFolderPath).Name);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("Trips/   ")]
    public void Constructor_Should_ThrowArgumentException_When_FinalNameIsWhiteSpace(string path)
    {
        // Act/Assert
        Assert.Throws<ArgumentException>(() => new FolderItem(path));
    }

    [Fact]
    public void Constructor_Should_GiveSameIdentity_When_FolderPathsMatch()
    {
        // Arrange/Act
        var first = new FolderItem("Photos");
        var second = new FolderItem("Photos");

        // Assert
        Assert.Equal(first.Id, second.Id);
    }
}
