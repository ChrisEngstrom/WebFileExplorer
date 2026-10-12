using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WebFileExplorer.Application.Errors;
using WebFileExplorer.WebApp.IntegrationTests.Fixtures;

namespace WebFileExplorer.WebApp.IntegrationTests.Controllers.Files;

public sealed class DownloadTests : IDisposable
{
    private readonly FileExplorerWebApplicationFactory _factory = new ();
    private readonly HttpClient _client;

    public DownloadTests()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Download_Should_ReturnBytesAndAttachmentHeaders_When_FileExists()
    {
        // Arrange/Act
        using var response = await _client.GetAsync(
            new Uri("/api/files/download?path=Photos%2Fsample2.txt", UriKind.Relative),
            TestContext.Current.CancellationToken);
        byte[] downloaded = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new byte[58], downloaded);
        Assert.Equal("application/octet-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("sample2.txt", response.Content.Headers.ContentDisposition?.FileNameStar);
    }

    [Fact]
    public async Task Download_Should_ReturnTypedNotFound_When_FileIsMissing()
    {
        // Arrange/Act
        using var missing = await _client.GetAsync(
            new Uri("/api/files/download?path=gone.bin", UriKind.Relative),
            TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(
            await missing.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("File.NotFound", body.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            FileSystemErrors.FileNotFound.Description,
            body.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Download_Should_ReturnTypedInvalidPath_When_PathIsUnsafe()
    {
        // Arrange/Act
        using var response = await _client.GetAsync(
            new Uri("/api/files/download?path=Photos%2F..%2Fsecret.txt", UriKind.Relative),
            TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Path.Invalid", body.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            FileSystemErrors.InvalidPath.Description,
            body.RootElement.GetProperty("error").GetString());
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
