using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.Errors;
using WebFileExplorer.Application.FileSystemManagement.Commands.UploadFile;
using WebFileExplorer.WebApp.IntegrationTests.Fixtures;

namespace WebFileExplorer.WebApp.IntegrationTests.Controllers.Files;

public sealed class UploadTests : IDisposable
{
    private readonly FileExplorerWebApplicationFactory _factory = new ();
    private readonly HttpClient _client;

    public UploadTests()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public void UploadRegistration_Should_ResolveApplicationHandler_When_WebAppStarts()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UploadCommand>>();

        // Assert
        Assert.Equal("UploadCommandHandler", handler.GetType().Name);
    }

    [Fact]
    public async Task Upload_Should_AllowDownload_When_NewFileIsCommitted()
    {
        // Arrange
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent([7, 8, 9]);

        form.Add(
            fileContent,
            "file",
            "new.bin");

        // Act
        using var upload = await _client.PostAsync(
            new Uri("/api/files/upload?path=Photos", UriKind.Relative),
            form,
            TestContext.Current.CancellationToken);
        byte[] downloaded = await _client.GetByteArrayAsync(
            new Uri("/api/files/download?path=Photos%2Fnew.bin", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        Assert.Equal(
            "/api/files/download?path=Photos%2Fnew.bin",
            upload.Headers.Location?.OriginalString);
        Assert.Equal([7, 8, 9], downloaded);
    }

    [Fact]
    public async Task Upload_Should_ReturnConflictAndPreserveBytes_When_FileAlreadyExists()
    {
        // Arrange
        byte[] existingBytes = [7, 8, 9];
        string destination = Path.Combine(
            _factory.RootPath,
            "Photos",
            "existing.bin");
        await File.WriteAllBytesAsync(destination, existingBytes, TestContext.Current.CancellationToken);
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent([1]);

        form.Add(
            fileContent,
            "file",
            "existing.bin");

        // Act
        using var duplicate = await _client.PostAsync(
            new Uri("/api/files/upload?path=Photos", UriKind.Relative),
            form,
            TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(
            await duplicate.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("File.Exists", body.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            FileSystemErrors.FileExists.Description,
            body.RootElement.GetProperty("error").GetString());
        Assert.Equal(
            existingBytes,
            await File.ReadAllBytesAsync(destination, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("/api/files/upload")]
    [InlineData("/api/files/upload?path=")]
    public async Task Upload_Should_ReturnRelativeDownloadLocation_When_TargetIsRoot(string url)
    {
        // Arrange
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent([1]);

        form.Add(
            fileContent,
            "file",
            "root-new.bin");

        // Act
        using var upload = await _client.PostAsync(
            new Uri(url, UriKind.Relative),
            form,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        Assert.Equal("/api/files/download?path=root-new.bin", upload.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Upload_Should_ReturnTypedLimitExceededAndRemoveFile_When_ContentExceedsLimit()
    {
        // Arrange
        using var factory = new FileExplorerWebApplicationFactory(maxUploadBytes: 2);
        using var client = factory.CreateClient();
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent([1, 2, 3]);

        form.Add(
            fileContent,
            "file",
            "too-large.bin");

        // Act
        using var response = await client.PostAsync(
            new Uri("/api/files/upload?path=Photos", UriKind.Relative),
            form,
            TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("FileSystem.LimitExceeded", body.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            FileSystemErrors.LimitExceeded.Description,
            body.RootElement.GetProperty("error").GetString());
        Assert.Equal(2, body.RootElement.GetProperty("maxUploadBytes").GetInt64());
        Assert.False(body.RootElement.GetProperty("requestTooLarge").GetBoolean());
        Assert.False(File.Exists(Path.Combine(
            factory.RootPath,
            "Photos",
            "too-large.bin")));
    }

    [Fact]
    public async Task Upload_Should_ReturnTypedLimitExceeded_When_RequestExceedsMultipartAllowance()
    {
        // Arrange
        using var factory = new FileExplorerWebApplicationFactory(maxUploadBytes: 2);
        using var client = factory.CreateClient();
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(new byte[(1024 * 1024) + 3]);

        form.Add(
            fileContent,
            "file",
            "much-too-large.bin");

        // TestServer does not enforce Kestrel's request limit. Explicit length exercises the pre-binding guard.
        form.Headers.ContentLength = (await form.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)).Length;

        // Act
        using var response = await client.PostAsync(
            new Uri("/api/files/upload?path=Photos", UriKind.Relative),
            form,
            TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("FileSystem.LimitExceeded", body.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            FileSystemErrors.LimitExceeded.Description,
            body.RootElement.GetProperty("error").GetString());
        Assert.Equal(2, body.RootElement.GetProperty("maxUploadBytes").GetInt64());
        Assert.True(body.RootElement.GetProperty("requestTooLarge").GetBoolean());
        Assert.False(File.Exists(Path.Combine(
            factory.RootPath,
            "Photos",
            "much-too-large.bin")));
    }

    [Fact]
    public async Task Upload_Should_ReturnBadRequest_When_FileIsMissingFromForm()
    {
        // Arrange
        using var form = new MultipartFormDataContent();
        using var fileContent = new StringContent("Photos");

        form.Add(fileContent, "unused");

        // Act
        using var response = await _client.PostAsync(
            new Uri("/api/files/upload?path=Photos", UriKind.Relative),
            form,
            TestContext.Current.CancellationToken);

        // Assert
        // ApiController may reject the missing form field before the action runs.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single(Directory.EnumerateFiles(Path.Combine(_factory.RootPath, "Photos")));
    }

    [Fact]
    public async Task Upload_Should_ReturnTypedInvalidPath_When_DestinationIsUnsafe()
    {
        // Arrange
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent([1]);

        form.Add(
            fileContent,
            "file",
            "new.bin");

        // Act
        using var response = await _client.PostAsync(
            new Uri("/api/files/upload?path=..%2Foutside", UriKind.Relative),
            form,
            TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Path.Invalid", body.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            FileSystemErrors.InvalidPath.Description,
            body.RootElement.GetProperty("error").GetString());
        Assert.False(File.Exists(Path.Combine(_factory.RootPath, "new.bin")));
    }

    [Fact]
    public async Task Upload_Should_ReturnTypedNotFound_When_DestinationFolderIsMissing()
    {
        // Arrange
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent([1]);

        form.Add(
            fileContent,
            "file",
            "new.bin");

        // Act
        using var response = await _client.PostAsync(
            new Uri("/api/files/upload?path=Gone", UriKind.Relative),
            form,
            TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Folder.NotFound", body.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            FileSystemErrors.FolderNotFound.Description,
            body.RootElement.GetProperty("error").GetString());
        Assert.False(Directory.Exists(Path.Combine(_factory.RootPath, "Gone")));
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
