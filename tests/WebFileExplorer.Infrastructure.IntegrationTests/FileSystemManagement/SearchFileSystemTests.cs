using Microsoft.Extensions.DependencyInjection;
using WebFileExplorer.Application;
using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.Configurations;
using WebFileExplorer.Application.FileSystemManagement.Queries.SearchFileSystem;
using WebFileExplorer.Application.Responses;

namespace WebFileExplorer.Infrastructure.IntegrationTests.FileSystemManagement;

public sealed class SearchFileSystemTests : IDisposable
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

    public SearchFileSystemTests()
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
    public async Task Handle_Should_ReturnRelativePaths_When_DescendantsMatch()
    {
        // Arrange
        using var provider = Provider;

        var searchQueryHandler = provider.GetRequiredService<IQueryHandler<SearchQuery, FileSystemItemsResponse>>();

        // Act
        FileSystemItemsResponse result = (await searchQueryHandler.Handle(
            new SearchQuery("Photos", "sam"),
            CancellationToken.None)).Value;

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Photos/Again/sample2.txt", result.Items[0].RelativePath);
        Assert.Equal(58, result.TotalSizeBytes);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
