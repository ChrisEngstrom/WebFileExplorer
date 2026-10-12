using WebFileExplorer.Application.Configurations;
using WebFileExplorer.Application.Exceptions;
using WebFileExplorer.Infrastructure.FileSystem;
using WebFileExplorer.Infrastructure.IntegrationTests.Fixtures;

namespace WebFileExplorer.Infrastructure.IntegrationTests.FileSystem;

public sealed class PhysicalFileSystemTests : IDisposable
{
    private readonly TemporaryFileSystem _temporaryFileSystem = new ();

    [Fact]
    public void Constructor_Should_ThrowDirectoryNotFoundException_When_RootDoesNotExist()
    {
        // Arrange
        var configuration = new AppConfiguration
        {
            RootPath = Path.Combine(_temporaryFileSystem.RootPath, "missing")
        };

        // Act/Assert
        Assert.Throws<DirectoryNotFoundException>(() => new PhysicalFileSystem(configuration));
    }

    [Fact]
    public void Resolve_Should_ReturnConfiguredRoot_When_RelativePathIsEmpty()
    {
        // Arrange
        var fileSystem = _temporaryFileSystem.CreateFileSystem();

        // Act
        var result = fileSystem.Resolve(string.Empty);

        // Assert
        Assert.Equal(_temporaryFileSystem.RootPath, result);
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("/etc/passwd")]
    [InlineData("Photos/../sample.txt")]
    [InlineData("C:/Windows")]
    public void Resolve_Should_RejectUnsafePath_When_PathCouldLeaveRoot(string path)
    {
        // Arrange
        var fileSystem = _temporaryFileSystem.CreateFileSystem();

        // Act/Assert
        Assert.Throws<ArgumentException>(() => fileSystem.Resolve(path));
    }

    [Theory]
    [InlineData("escape")]
    [InlineData("escape/child.txt")]
    public void Resolve_Should_RejectLink_When_PathContainsSymbolicLink(string relativePath)
    {
        // Arrange
        Directory.CreateSymbolicLink(
            Path.Combine(_temporaryFileSystem.RootPath, "escape"),
            Path.GetDirectoryName(_temporaryFileSystem.RootPath)!);

        var fileSystem = _temporaryFileSystem.CreateFileSystem();

        // Act/Assert
        Assert.Throws<ArgumentException>(() => fileSystem.Resolve(relativePath));
    }

    [Fact]
    public void Relative_Should_ReturnSlashSeparatedPath_When_FileIsNested()
    {
        // Arrange
        var fileSystem = _temporaryFileSystem.CreateFileSystem();
        string path = Path.Combine(
            _temporaryFileSystem.RootPath,
            "Photos",
            "sample.txt");

        // Act
        var result = fileSystem.Relative(path);

        // Assert
        Assert.Equal("Photos/sample.txt", result);
    }

    [Fact]
    public void Items_Should_ReturnImmediateChildrenWithoutLinks_When_FolderHasMixedItems()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_temporaryFileSystem.RootPath, "Photos"));
        File.WriteAllText(Path.Combine(_temporaryFileSystem.RootPath, "root.txt"), "root");
        File.WriteAllText(
            Path.Combine(
                _temporaryFileSystem.RootPath,
                "Photos",
                "nested.txt"),
            "nested");
        File.CreateSymbolicLink(
            Path.Combine(_temporaryFileSystem.RootPath, "link.txt"),
            Path.Combine(_temporaryFileSystem.RootPath, "root.txt"));

        var fileSystem = _temporaryFileSystem.CreateFileSystem();

        // Act
        var result = fileSystem.Items(
                string.Empty,
                CancellationToken.None).Select(fileSystem.Relative).ToArray();

        // Assert
        Assert.Equal(["Photos", "root.txt"], result.OrderBy(path => path, StringComparer.Ordinal));
    }

    [Fact]
    public void Items_Should_ThrowDirectoryNotFoundException_When_FolderIsMissing()
    {
        // Arrange
        var fileSystem = _temporaryFileSystem.CreateFileSystem();

        // Act/Assert
        Assert.Throws<DirectoryNotFoundException>(() => fileSystem.Items(
                "missing",
                CancellationToken.None).ToArray());
    }

    [Fact]
    public void Items_Should_Succeed_When_DiscoveredItemsEqualLimit()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_temporaryFileSystem.RootPath, "one.txt"), "one");
        var configuration = _temporaryFileSystem.Configuration;
        configuration.MaxScannedItems = 1;

        var fileSystem = new PhysicalFileSystem(configuration);

        // Act
        var result = fileSystem.Items(string.Empty, CancellationToken.None).ToArray();

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public void Items_Should_CountSkippedLinks_When_DiscoveredItemsExceedLimit()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_temporaryFileSystem.RootPath, "one.txt"), "one");
        File.CreateSymbolicLink(
            Path.Combine(_temporaryFileSystem.RootPath, "link.txt"),
            Path.Combine(_temporaryFileSystem.RootPath, "one.txt"));
        var configuration = _temporaryFileSystem.Configuration;
        configuration.MaxScannedItems = 1;

        var fileSystem = new PhysicalFileSystem(configuration);

        // Act/Assert
        Assert.Throws<FileBrowserLimitExceededException>(
            () => fileSystem.Items(string.Empty, CancellationToken.None).ToArray());
    }

    [Fact]
    public void Items_Should_ThrowOperationCanceledException_When_TokenIsCancelled()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_temporaryFileSystem.RootPath, "one.txt"), "one");
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var fileSystem = _temporaryFileSystem.CreateFileSystem();

        // Act/Assert
        Assert.ThrowsAny<OperationCanceledException>(
            () => fileSystem.Items(string.Empty, cancellationTokenSource.Token).ToArray());
    }

    public void Dispose() => _temporaryFileSystem.Dispose();
}
