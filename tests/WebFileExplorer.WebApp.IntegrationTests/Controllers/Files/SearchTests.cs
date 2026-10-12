using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WebFileExplorer.Application.Errors;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.WebApp.IntegrationTests.Fixtures;

namespace WebFileExplorer.WebApp.IntegrationTests.Controllers.Files;

public sealed class SearchTests : IDisposable
{
    private readonly FileExplorerWebApplicationFactory _factory = new ();
    private readonly HttpClient _client;

    public SearchTests()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Get_Should_ReturnRelativeMatchingPaths_When_SearchingNamedFolder()
    {
        // Arrange/Act
        var searched = await _client.GetFromJsonAsync<FileSystemItemsResponse>(
            "/api/files?path=Photos&q=sam",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(searched);
        var item = Assert.Single(searched.Items);
        Assert.Equal("Photos/sample2.txt", item.RelativePath);
        Assert.Equal("sample2.txt", item.Name);
        Assert.Equal(58, searched.TotalSizeBytes);
    }

    [Fact]
    public async Task Get_Should_ReturnEmptyTotals_When_NoNamesMatch()
    {
        // Arrange/Act
        var noMatches = await _client.GetFromJsonAsync<FileSystemItemsResponse>(
            "/api/files?q=absent",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(noMatches);
        Assert.Empty(noMatches.Items);
        Assert.Equal(0, noMatches.TotalSizeBytes);
        Assert.Equal(0, noMatches.FileCount);
        Assert.Equal(0, noMatches.FolderCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("%20")]
    public async Task Get_Should_ReturnOnlyImmediateChildren_When_QueryIsBlank(string query)
    {
        // Arrange/Act
        var result = await _client.GetFromJsonAsync<FileSystemItemsResponse>(
            "/api/files?q=" + query,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(13, result.TotalSizeBytes);
        Assert.DoesNotContain(result.Items, item => item.RelativePath == "Photos/sample2.txt");
    }

    [Fact]
    public async Task Get_Should_ReturnTypedSearchTooLong_When_QueryExceedsLimit()
    {
        // Arrange/Act
        using var invalid = await _client.GetAsync(
            new Uri("/api/files?q=" + new string('x', 201), UriKind.Relative),
            TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(
            await invalid.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("Browse.SearchTooLong", body.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            FileSystemErrors.SearchTooLong.Description,
            body.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Get_Should_ReturnTypedLimitExceeded_When_PerFolderScanExceedsLimit()
    {
        // Arrange
        using var factory = new FileExplorerWebApplicationFactory(maxScannedItems: 1);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/files?q=sample", UriKind.Relative),
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

    [Fact]
    public async Task Get_Should_ReturnTypedLimitExceeded_When_AggregateScanExceedsLimit()
    {
        // Arrange
        // Root has two rows, Photos has one. Both listings fit, but the combined scan does not.
        using var factory = new FileExplorerWebApplicationFactory(maxScannedItems: 2);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/files?q=absent", UriKind.Relative),
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
