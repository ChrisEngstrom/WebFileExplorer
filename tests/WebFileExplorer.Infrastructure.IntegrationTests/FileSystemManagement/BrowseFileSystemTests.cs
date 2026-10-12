using Microsoft.Extensions.DependencyInjection;
using WebFileExplorer.Application;
using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.Configurations;
using WebFileExplorer.Application.FileSystemManagement.Queries.BrowseFileSystem;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Infrastructure.IntegrationTests.FileSystemManagement;

public sealed class BrowseFileSystemTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "explorer-reference-" + Guid.NewGuid().ToString("N"));

    private ServiceProvider Provider => new ServiceCollection()
        .AddLogging()
        .AddSingleton(new AppConfiguration { RootPath = _root })
        .AddApplication()
        .AddInfrastructure()
        .BuildServiceProvider();

    public BrowseFileSystemTests()
    {
        Directory.CreateDirectory(Path.Combine(
            _root,
            "Photos",
            "Again"));

        File.WriteAllBytes(
            Path.Combine(
                _root,
                "sample.txt"),
            new byte[13]);

        File.WriteAllBytes(
            Path.Combine(
                _root,
                "Photos",
                "Again",
                "sample2.txt"),
            new byte[58]);
    }

    [Fact]
    public async Task Handle_Should_ReturnImmediateChildrenAndVisibleFileTotal_When_BrowsingRoot()
    {
        // Arrange
        using var provider = Provider;

        var browseQueryHandler = provider.GetRequiredService<IQueryHandler<BrowseQuery, FileSystemItemsResponse>>();

        // Act
        FileSystemItemsResponse result = (await browseQueryHandler.Handle(
            new BrowseQuery(string.Empty),
            CancellationToken.None)).Value;

        // Assert
        Assert.Equal(1, result.FolderCount);
        Assert.Equal(1, result.FileCount);
        Assert.Equal(13, result.TotalSizeBytes);
        Assert.Contains(result.Items, e => e.RelativePath == "Photos" && e.IsDirectory);
        Assert.DoesNotContain(result.Items, e => e.RelativePath == "Photos/Again/sample2.txt");
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("/etc/passwd")]
    [InlineData("Photos/../sample.txt")]
    [InlineData("C:/Windows")]
    public async Task Handle_Should_RejectInvalidPath_When_BrowsePathIsUnsafe(string path)
    {
        // Arrange
        using var provider = Provider;

        var browseQueryHandler = provider.GetRequiredService<IQueryHandler<BrowseQuery, FileSystemItemsResponse>>();

        // Act
        var result = await browseQueryHandler.Handle(
            new BrowseQuery(path),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Handle_Should_RejectPath_When_BrowseSymlinkLeavesRoot()
    {
        // Arrange
        string link = Path.Combine(_root, "escape");
        Directory.CreateSymbolicLink(link, Path.GetDirectoryName(_root)!);
        using var provider = Provider;

        var browseQueryHandler = provider.GetRequiredService<IQueryHandler<BrowseQuery, FileSystemItemsResponse>>();

        // Act
        var result = await browseQueryHandler.Handle(
            new BrowseQuery("escape"),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
