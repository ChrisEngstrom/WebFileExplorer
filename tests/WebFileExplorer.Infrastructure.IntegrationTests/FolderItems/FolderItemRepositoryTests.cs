using WebFileExplorer.Domain.FolderItems;
using WebFileExplorer.Infrastructure.FolderItems;
using WebFileExplorer.Infrastructure.IntegrationTests.Fixtures;

namespace WebFileExplorer.Infrastructure.IntegrationTests.FolderItems;

public sealed class FolderItemRepositoryTests : IDisposable
{
    private readonly TemporaryFileSystem _temporaryFileSystem = new ();

    [Fact]
    public async Task GetFolderItemsFromRelativePath_Should_ReturnSortedImmediateFolders_When_ItemsAreMixed()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(
            _temporaryFileSystem.RootPath,
            "z-folder",
            "nested"));
        Directory.CreateDirectory(Path.Combine(_temporaryFileSystem.RootPath, "A-folder"));
        await File.WriteAllTextAsync(
            Path.Combine(_temporaryFileSystem.RootPath, "file.txt"),
            "file",
            TestContext.Current.CancellationToken);

        var folderItemRepository = new FolderItemRepository(_temporaryFileSystem.CreateFileSystem());

        // Act
        var result = (await folderItemRepository.GetFolderItemsFromRelativePath(string.Empty)).ToArray();

        // Assert
        Assert.Equal(["A-folder", "z-folder"], result.Select(folder => folder.RelativePath));
        Assert.Equal(["A-folder", "z-folder"], result.Select(folder => folder.Name));
        Assert.Equal(["A-folder", "z-folder"], result.Select(folder => folder.Id.Value));
    }

    [Fact]
    public async Task GetFolderItemsFromRelativePath_Should_ReturnEmptyItems_When_FolderIsEmpty()
    {
        // Arrange
        var folderItemRepository = new FolderItemRepository(_temporaryFileSystem.CreateFileSystem());

        // Act
        var result = await folderItemRepository.GetFolderItemsFromRelativePath(string.Empty);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Photos")]
    public async Task GetById_Should_ReturnRelativeIdentity_When_FolderExists(string path)
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_temporaryFileSystem.RootPath, "Photos"));

        var repository = new FolderItemRepository(_temporaryFileSystem.CreateFileSystem());

        // Act
        var result = await repository.GetById(new FolderItemId(path));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(path, result.Id.Value);
        Assert.Equal(path, result.RelativePath);
    }

    [Fact]
    public async Task GetById_Should_ReturnNull_When_FolderIsMissing()
    {
        // Arrange
        var folderItemRepository = new FolderItemRepository(_temporaryFileSystem.CreateFileSystem());

        // Act
        var result = await folderItemRepository.GetById(new FolderItemId("missing"));

        // Assert
        Assert.Null(result);
    }

    public void Dispose() => _temporaryFileSystem.Dispose();
}
