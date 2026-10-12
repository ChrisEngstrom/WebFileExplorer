using FluentAssertions;
using WebFileExplorer.Domain.FileItems;

namespace WebFileExplorer.Domain.UnitTests.FileItems;

public class FileItemTests
{
    [Fact]
    public void Constructor_Should_SetPropertyValues()
    {
        // Arrange
        var fileRelativePath = "TestFolder/testFile.txt";
        var fileName = "testFile.txt";
        var fileSize = 2;

        // Act
        var fileItem = new FileItem(fileRelativePath, fileSize);

        // Assert
        fileItem.Id.Should().Be(new FileItemId(fileRelativePath));
        fileItem.Name.Should().Be(fileName);
        fileItem.RelativePath.Should().Be(fileRelativePath);
        fileItem.SizeBytes.Should().Be(fileSize);
    }

    [Fact]
    public void Constructor_Should_GiveSameIdentity_When_FilePathsMatch()
    {
        // Arrange/Act
        var first = new FileItem("Photos/sample.txt", 7);
        var second = new FileItem("Photos/sample.txt", 14);

        // Assert
        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentOutOfRangeException_When_FileSizeIsNegative()
    {
        // Arrange
        var fileRelativePath = "Photos/sample.txt";
        var fileSize = -7;

        // Act/Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new FileItem(fileRelativePath, fileSize));
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_When_FileNameIsNull()
    {
        // Arrange
        string? fileRelativePath = null;
        var fileSize = 7;

        // Act/Assert
        Assert.Throws<ArgumentNullException>(() => new FileItem(fileRelativePath!, fileSize));
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentException_When_FileNameIsWhiteSpace()
    {
        // Arrange
        var fileRelativePath = "Photos/   ";
        var fileSize = 7;

        // Act/Assert
        Assert.Throws<ArgumentException>(() => new FileItem(fileRelativePath, fileSize));
    }
}
