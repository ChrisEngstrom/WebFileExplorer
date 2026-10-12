using WebFileExplorer.Domain.Paths;

namespace WebFileExplorer.Domain.UnitTests.Paths;

public sealed class RelativePathRulesTests
{
    [Theory]
    [InlineData("Photos")]
    [InlineData("Photos/sample.txt")]
    [InlineData(" ")]
    public void Validate_Should_ReturnOriginalPath_When_SyntaxIsValid(string path)
    {
        // Act/Assert
        Assert.Equal(path, RelativePathRules.Validate(path));
    }

    [Fact]
    public void Validate_Should_AllowEmptyPath_When_RootIsAllowed()
    {
        // Arrange
        var rootPath = string.Empty;
        var isRoot = true;

        // Act/Assert
        Assert.Equal(rootPath, RelativePathRules.Validate(rootPath, isRoot));
    }

    [Fact]
    public void Validate_Should_ThrowArgumentException_When_EmptyPathIsNotRoot()
    {
        // Arrange
        var rootPath = string.Empty;
        var isRoot = false;

        // Act/Assert
        Assert.Throws<ArgumentException>(() => RelativePathRules.Validate(rootPath, isRoot));
    }

    [Theory]
    [InlineData("/Photos")]
    [InlineData("Photos\\sample.txt")]
    [InlineData("C:/Photos")]
    [InlineData("Photos\0sample.txt")]
    [InlineData("Photos//sample.txt")]
    [InlineData("Photos/./sample.txt")]
    [InlineData("Photos/../sample.txt")]
    [InlineData("Photos/")]
    public void Validate_Should_ThrowArgumentException_When_PathSyntaxIsInvalid(string path)
    {
        // Act/Assert
        Assert.Throws<ArgumentException>(() => RelativePathRules.Validate(path));
    }

    [Fact]
    public void Validate_Should_ThrowArgumentNullException_When_PathIsNull()
    {
        // Act/Assert
        Assert.Throws<ArgumentNullException>(() => RelativePathRules.Validate(null!));
    }
}
