using WebFileExplorer.Application.Exceptions;
using WebFileExplorer.Domain.FileItems;
using WebFileExplorer.Domain.FolderItems;
using WebFileExplorer.Infrastructure.FileItems;
using WebFileExplorer.Infrastructure.IntegrationTests.Fakes.Streams;
using WebFileExplorer.Infrastructure.IntegrationTests.Fixtures;

namespace WebFileExplorer.Infrastructure.IntegrationTests.FileItems;

public sealed class FileItemRepositoryTests : IDisposable
{
    private readonly TemporaryFileSystem _temporaryFileSystem = new ();

    [Fact]
    public async Task GetFileItemsFromFolder_Should_ReturnSortedImmediateMetadata_When_ItemsAreMixed()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_temporaryFileSystem.RootPath, "Photos"));
        await File.WriteAllBytesAsync(
            Path.Combine(
                _temporaryFileSystem.RootPath,
                "z.txt"),
            new byte[13],
            TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(
            Path.Combine(
                _temporaryFileSystem.RootPath,
                "A.txt"),
            new byte[3],
            TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(
            Path.Combine(
                _temporaryFileSystem.RootPath,
                "Photos",
                "nested.txt"),
            new byte[58],
            TestContext.Current.CancellationToken);

        var fileItemRepository = NewRepository();

        // Act
        var result = (await fileItemRepository.GetFileItemsFromFolder(new FolderItemId(string.Empty))).ToArray();

        // Assert
        Assert.Equal(["A.txt", "z.txt"], result.Select(file => file.Name));
        Assert.Equal(["A.txt", "z.txt"], result.Select(file => file.RelativePath));
        Assert.Equal([3L, 13L], result.Select(file => file.SizeBytes));
        Assert.Equal(["A.txt", "z.txt"], result.Select(file => file.Id.Value));
    }

    [Fact]
    public async Task GetFileItemsFromFolder_Should_ReturnEmptyItems_When_FolderIsEmpty()
    {
        // Arrange
        var repository = NewRepository();

        // Act
        var result = await repository.GetFileItemsFromFolder(new FolderItemId(string.Empty));

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetById_Should_ReturnMetadata_When_FileExists()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_temporaryFileSystem.RootPath, "Photos"));
        await File.WriteAllBytesAsync(
            Path.Combine(
                _temporaryFileSystem.RootPath,
                "Photos",
                "sample.txt"),
            new byte[13],
            TestContext.Current.CancellationToken);

        var fileItemRepository = NewRepository();

        // Act
        var result = await fileItemRepository.GetById(new FileItemId("Photos/sample.txt"));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Photos/sample.txt", result.Id.Value);
        Assert.Equal("Photos/sample.txt", result.RelativePath);
        Assert.Equal("sample.txt", result.Name);
        Assert.Equal(13, result.SizeBytes);
    }

    [Fact]
    public async Task GetById_Should_ReturnNull_When_FileIsMissing()
    {
        // Arrange
        var fileItemRepository = NewRepository();

        // Act
        var result = await fileItemRepository.GetById(new FileItemId("missing.txt"));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task OpenRead_Should_ReturnExactBytes_When_FileExists()
    {
        // Arrange
        byte[] bytes = [0, 1, 2, 3];
        await File.WriteAllBytesAsync(
            Path.Combine(_temporaryFileSystem.RootPath, "sample.bin"),
            bytes,
            TestContext.Current.CancellationToken);
        using var output = new MemoryStream();

        var fileItemRepository = NewRepository();

        // Act
        await using var content = fileItemRepository.OpenRead(new FileItemId("sample.bin"));
        await content.CopyToAsync(output, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(bytes, output.ToArray());
    }

    [Fact]
    public void OpenRead_Should_ThrowFileNotFoundException_When_FileIsMissing()
    {
        // Arrange
        var fileItemRepository = NewRepository();

        // Act/Assert
        Assert.Throws<FileNotFoundException>(() => fileItemRepository.OpenRead(new FileItemId("missing.txt")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Photos")]
    public async Task SaveNew_Should_CommitBytesAndRelativeIdentity_When_ContentEqualsLimit(string directory)
    {
        ArgumentNullException.ThrowIfNull(directory);

        // Arrange
        Directory.CreateDirectory(Path.Combine(_temporaryFileSystem.RootPath, "Photos"));
        byte[] bytes = [7, 8, 9, 10];
        using var content = new MemoryStream(bytes);
        string expectedPath = directory.Length == 0 ? "new.bin" : "Photos/new.bin";

        var fileItemRepository = NewRepository(maxUploadBytes: bytes.Length);

        // Act
        var result = await fileItemRepository.SaveNew(
            directory,
            "new.bin",
            content,
            CancellationToken.None);

        // Assert
        Assert.Equal(expectedPath, result.Id.Value);
        Assert.Equal(expectedPath, result.RelativePath);
        Assert.Equal("new.bin", result.Name);
        Assert.Equal(bytes.Length, result.SizeBytes);
        Assert.Equal(
            bytes,
            await File.ReadAllBytesAsync(
                Path.Combine(
                    _temporaryFileSystem.RootPath,
                    directory,
                    "new.bin"),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveNew_Should_PreserveExistingBytes_When_FileAlreadyExists()
    {
        // Arrange
        byte[] existingBytes = [7, 8, 9];
        string destination = Path.Combine(_temporaryFileSystem.RootPath, "existing.bin");
        await File.WriteAllBytesAsync(
            destination,
            existingBytes,
            TestContext.Current.CancellationToken);
        using var content = new MemoryStream([1]);

        var fileItemRepository = NewRepository();

        // Act/Assert
        await Assert.ThrowsAsync<FileAlreadyExistsException>(() => fileItemRepository.SaveNew(
            string.Empty,
            "existing.bin",
            content,
            CancellationToken.None));
        Assert.Equal(
            existingBytes,
            await File.ReadAllBytesAsync(destination, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("bad/name")]
    [InlineData("../outside")]
    public async Task SaveNew_Should_RejectNameWithoutCreatingFile_When_NameIsInvalid(string fileName)
    {
        // Arrange
        using var content = new MemoryStream([1]);

        var fileItemRepository = NewRepository();

        // Act/Assert
        await Assert.ThrowsAsync<ArgumentException>(() => fileItemRepository.SaveNew(
            string.Empty,
            fileName,
            content,
            CancellationToken.None));
        Assert.Empty(Directory.EnumerateFileSystemEntries(_temporaryFileSystem.RootPath));
    }

    [Fact]
    public async Task SaveNew_Should_ThrowArgumentNullException_When_ContentIsNull()
    {
        // Arrange
        var fileItemRepository = NewRepository();

        // Act/Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => fileItemRepository.SaveNew(
            string.Empty,
            "new.bin",
            null!,
            CancellationToken.None));
        Assert.Empty(Directory.EnumerateFileSystemEntries(_temporaryFileSystem.RootPath));
    }

    [Fact]
    public async Task SaveNew_Should_ThrowDirectoryNotFoundException_When_FolderIsMissing()
    {
        // Arrange
        using var content = new MemoryStream([1]);

        var repository = NewRepository();

        // Act/Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => repository.SaveNew(
            "missing",
            "new.bin",
            content,
            CancellationToken.None));
        Assert.Empty(Directory.EnumerateFileSystemEntries(_temporaryFileSystem.RootPath));
    }

    [Fact]
    public async Task SaveNew_Should_RemovePartialFile_When_ContentExceedsLimit()
    {
        // Arrange
        using var content = new MemoryStream(new byte[5]);

        var fileItemRepository = NewRepository(maxUploadBytes: 4);

        // Act/Assert
        await Assert.ThrowsAsync<FileBrowserLimitExceededException>(() => fileItemRepository.SaveNew(
            string.Empty,
            "oversized.bin",
            content,
            CancellationToken.None));
        Assert.False(File.Exists(Path.Combine(_temporaryFileSystem.RootPath, "oversized.bin")));
    }

    [Fact]
    public async Task SaveNew_Should_RemoveWrittenPartialFile_When_SourceStreamFails()
    {
        // Arrange
        using var fakeContent = new FailingReadStream();

        var fileItemRepository = NewRepository();

        // Act/Assert
        await Assert.ThrowsAsync<IOException>(() => fileItemRepository.SaveNew(
            string.Empty,
            "failed.bin",
            fakeContent,
            CancellationToken.None));
        Assert.False(File.Exists(Path.Combine(_temporaryFileSystem.RootPath, "failed.bin")));
    }

    [Fact]
    public async Task SaveNew_Should_RemovePartialFile_When_TokenIsCancelled()
    {
        // Arrange
        using var content = new MemoryStream([1]);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        var fileItemRepository = NewRepository();

        // Act/Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fileItemRepository.SaveNew(
            string.Empty,
            "cancelled.bin",
            content,
            cancellationTokenSource.Token));
        Assert.False(File.Exists(Path.Combine(_temporaryFileSystem.RootPath, "cancelled.bin")));
    }

    public void Dispose() => _temporaryFileSystem.Dispose();

    private FileItemRepository NewRepository(long maxUploadBytes = 4)
    {
        var configuration = _temporaryFileSystem.Configuration;
        configuration.MaxUploadBytes = maxUploadBytes;
        return new FileItemRepository(_temporaryFileSystem.CreateFileSystem(), configuration);
    }
}
