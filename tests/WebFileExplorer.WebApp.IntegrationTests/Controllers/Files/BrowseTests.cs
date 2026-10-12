using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WebFileExplorer.Application.Errors;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.WebApp.IntegrationTests.Fixtures;

namespace WebFileExplorer.WebApp.IntegrationTests.Controllers.Files;

public sealed class BrowseTests : IDisposable
{
    private readonly FileExplorerWebApplicationFactory _factory = new ();
    private readonly HttpClient _client;

    public BrowseTests()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Theory]
    [InlineData("/api/files")]
    [InlineData("/api/files?path=")]
    public async Task Get_Should_ReturnImmediateRootChildren_When_PathIsOmittedOrEmpty(string url)
    {
        // Arrange/Act
        var root = await _client.GetFromJsonAsync<FileSystemItemsResponse>(
            url,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(root);
        Assert.Equal(string.Empty, root.Path);
        Assert.Equal(1, root.FolderCount);
        Assert.Equal(1, root.FileCount);
        Assert.Equal(13, root.TotalSizeBytes);
        Assert.Equal(2, root.Items.Count);
        Assert.Equal("Photos", Assert.Single(root.Items, item => item.IsDirectory).Name);
        Assert.Contains(root.Items, item => !item.IsDirectory && item.RelativePath == "sample.txt");
        Assert.DoesNotContain(root.Items, item => item.RelativePath == "Photos/sample2.txt");
    }

    [Fact]
    public async Task Get_Should_ReturnImmediateChildren_When_BrowsingNamedFolder()
    {
        // Arrange/Act
        var photos = await _client.GetFromJsonAsync<FileSystemItemsResponse>(
            "/api/files?path=Photos",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(photos);
        Assert.Equal("Photos", photos.Path);
        Assert.Equal("Photos/sample2.txt", Assert.Single(photos.Items).RelativePath);
        Assert.Equal(58, photos.TotalSizeBytes);
    }

    [Fact]
    public async Task Get_Should_ReturnItemsProperty_When_ListingIsSerialized()
    {
        // Arrange/Act
        using var json = JsonDocument.Parse(await _client.GetStringAsync(
            new Uri("/api/files", UriKind.Relative),
            TestContext.Current.CancellationToken));

        // Assert
        Assert.True(json.RootElement.TryGetProperty("items", out _));
        Assert.False(json.RootElement.TryGetProperty("entries", out _));
    }

    [Fact]
    public async Task Get_Should_RejectTraversalWithoutLeakingRoot_When_PathIsUnsafe()
    {
        // Arrange/Act
        using var response = await _client.GetAsync(
            new Uri("/api/files?path=..%2F..%2Fetc", UriKind.Relative),
            TestContext.Current.CancellationToken);
        string text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(text);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Path.Invalid", body.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            FileSystemErrors.InvalidPath.Description,
            body.RootElement.GetProperty("error").GetString());
        Assert.DoesNotContain(_factory.RootPath, text);
    }

    [Fact]
    public async Task Get_Should_ReturnTypedNotFound_When_FolderIsMissing()
    {
        // Arrange/Act
        using var response = await _client.GetAsync(
            new Uri("/api/files?path=Photos%2FGone%2FChild", UriKind.Relative),
            TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Folder.NotFound", body.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            FileSystemErrors.FolderNotFound.Description,
            body.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Get_Should_ReturnTypedLimitExceeded_When_ImmediateScanExceedsLimit()
    {
        // Arrange
        using var factory = new FileExplorerWebApplicationFactory(maxScannedItems: 1);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/files", UriKind.Relative),
            TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("FileSystem.LimitExceeded", body.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            FileSystemErrors.LimitExceeded.Description,
            body.RootElement.GetProperty("error").GetString());
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
