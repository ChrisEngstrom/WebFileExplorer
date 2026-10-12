using Moq;
using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.Configurations;
using WebFileExplorer.Application.Errors;
using WebFileExplorer.Application.FileSystemManagement.Queries.BrowseFileSystem;
using WebFileExplorer.Application.FileSystemManagement.Queries.GetFileItems;
using WebFileExplorer.Application.FileSystemManagement.Queries.GetFolderItems;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.Application.UnitTests.Fakes.Queries;
using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Application.UnitTests.FileSystemManagement.Queries.BrowseFileSystem;

public class BrowseQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_SortFoldersBeforeFiles_When_ChildrenAreUnsorted()
    {
        // Arrange
        var mockGetFolderItemsQueryHandler = new Mock<IQueryHandler<GetFolderItemsQuery, IReadOnlyList<FolderItemResponse>>>();
        var mockGetFileItemsQueryHandler = new Mock<IQueryHandler<GetFileItemsQuery, IReadOnlyList<FileItemResponse>>>();
        mockGetFolderItemsQueryHandler.Setup(x => x.Handle(
                It.IsAny<GetFolderItemsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<FolderItemResponse>>(
            new List<FolderItemResponse>()
            {
                new FolderItemResponse
                {
                    Id = new ("z-folder"),
                    Name = "z-folder",
                    RelativePath = "z-folder"
                },
                new FolderItemResponse
                {
                    Id = new ("A-folder"),
                    Name = "A-folder",
                    RelativePath = "A-folder"
                }
            }));
        mockGetFileItemsQueryHandler.Setup(x => x.Handle(
                It.IsAny<GetFileItemsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<FileItemResponse>>(
            new List<FileItemResponse>()
            {
                new FileItemResponse
                {
                    Id = new ("z.txt"),
                    Name = "z.txt",
                    RelativePath = "z.txt",
                    SizeBytes = 3
                },
                new FileItemResponse
                {
                    Id = new ("A.txt"),
                    Name = "A.txt",
                    RelativePath = "A.txt",
                    SizeBytes = 2
                }
            }));

        var browseQueryHandler = new BrowseQueryHandler(
            mockGetFolderItemsQueryHandler.Object,
            mockGetFileItemsQueryHandler.Object,
            new AppConfiguration());

        // Act
        var result = await browseQueryHandler.Handle(new BrowseQuery(string.Empty), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(
            ["A-folder", "z-folder", "A.txt", "z.txt"],
            result.Value.Items.Select(item => item.RelativePath));
        Assert.Equal(2, result.Value.FolderCount);
        Assert.Equal(2, result.Value.FileCount);
        Assert.Equal(5, result.Value.TotalSizeBytes);
    }

    [Fact]
    public async Task Handle_Should_Succeed_When_TotalRowsEqualScanLimit()
    {
        // Arrange
        var browseQueryHandler = new BrowseQueryHandler(
            new FakeGetFolderItemsQueryHandler(),
            new FakeGetFileItemsQueryHandler(),
            new AppConfiguration { MaxScannedItems = 2 });

        // Act
        var result = await browseQueryHandler.Handle(new BrowseQuery(string.Empty), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task Handle_Should_ComposeImmediateChildrenAndVisibleTotals_When_BrowsingRoot()
    {
        // Arrange
        var fakeGetFolderItemsQueryHandler = new FakeGetFolderItemsQueryHandler();
        var fakeGetFileItemsQueryHandler = new FakeGetFileItemsQueryHandler();

        var browseQueryHandler = new BrowseQueryHandler(
            fakeGetFolderItemsQueryHandler,
            fakeGetFileItemsQueryHandler,
            new AppConfiguration());

        // Act
        var result = await browseQueryHandler.Handle(new BrowseQuery(string.Empty), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.FolderCount);
        Assert.Equal(1, result.Value.FileCount);
        Assert.Equal(13, result.Value.TotalSizeBytes);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal([string.Empty], fakeGetFolderItemsQueryHandler.Requests);
        Assert.Equal([string.Empty], fakeGetFileItemsQueryHandler.Requests);
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidPath_When_RequestPathIsUnsafe()
    {
        // Arrange
        var mockGetFolderQueryHandler = new Mock<IQueryHandler<GetFolderItemsQuery, IReadOnlyList<FolderItemResponse>>>();
        var mockGetFileQueryHandler = new Mock<IQueryHandler<GetFileItemsQuery, IReadOnlyList<FileItemResponse>>>();

        var browseQueryHandler = new BrowseQueryHandler(
            mockGetFolderQueryHandler.Object,
            mockGetFileQueryHandler.Object,
            new AppConfiguration());

        // Act
        var result = await browseQueryHandler.Handle(new BrowseQuery("../outside"), CancellationToken.None);

        // Assert
        Assert.Equal("Path.Invalid", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.True(result.IsFailure);

        mockGetFolderQueryHandler.VerifyNoOtherCalls();
        mockGetFileQueryHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_Should_ReturnFolderQueryFailure_When_FolderQueryReturnsFailure()
    {
        // Arrange
        var error = Error.NotFound("Test.FolderQueryFailure", "A supplied folder query failure.");
        var mockGetFolderItemsQueryHandler = new Mock<IQueryHandler<GetFolderItemsQuery, IReadOnlyList<FolderItemResponse>>>();
        var mockGetFileItemsQueryHandler = new Mock<IQueryHandler<GetFileItemsQuery, IReadOnlyList<FileItemResponse>>>();
        mockGetFolderItemsQueryHandler.Setup(x => x.Handle(
                It.IsAny<GetFolderItemsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IReadOnlyList<FolderItemResponse>>(error));

        var browseQueryHandler = new BrowseQueryHandler(
            mockGetFolderItemsQueryHandler.Object,
            mockGetFileItemsQueryHandler.Object,
            new AppConfiguration());

        // Act
        var result = await browseQueryHandler.Handle(new BrowseQuery(string.Empty), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        mockGetFolderItemsQueryHandler.Verify(
            x => x.Handle(It.IsAny<GetFolderItemsQuery>(), CancellationToken.None),
            Times.Once);
        mockGetFileItemsQueryHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_Should_ReturnFileQueryFailure_When_FileQueryReturnsFailure()
    {
        // Arrange
        var error = Error.NotFound("Test.FileQueryFailure", "A supplied file query failure.");
        var mockGetFolderItemsQueryHandler = new Mock<IQueryHandler<GetFolderItemsQuery, IReadOnlyList<FolderItemResponse>>>();
        var mockGetFileItemsQueryHandler = new Mock<IQueryHandler<GetFileItemsQuery, IReadOnlyList<FileItemResponse>>>();
        mockGetFolderItemsQueryHandler.Setup(x => x.Handle(
                It.IsAny<GetFolderItemsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<FolderItemResponse>>([]));
        mockGetFileItemsQueryHandler.Setup(x => x.Handle(
                It.IsAny<GetFileItemsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IReadOnlyList<FileItemResponse>>(error));

        var browseQueryHandler = new BrowseQueryHandler(
            mockGetFolderItemsQueryHandler.Object,
            mockGetFileItemsQueryHandler.Object,
            new AppConfiguration());

        // Act
        var result = await browseQueryHandler.Handle(new BrowseQuery(string.Empty), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        mockGetFolderItemsQueryHandler.Verify(
            x => x.Handle(It.IsAny<GetFolderItemsQuery>(), CancellationToken.None),
            Times.Once);
        mockGetFileItemsQueryHandler.Verify(
            x => x.Handle(It.IsAny<GetFileItemsQuery>(), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_RejectFolderAndFileRows_When_TotalExceedsScanLimit()
    {
        // Arrange
        // The fakes used return one folder and one file so the file triggers the
        //  LimitExceeded because files are evaluated second
        var browseQueryHandler = new BrowseQueryHandler(
            new FakeGetFolderItemsQueryHandler(),
            new FakeGetFileItemsQueryHandler(),
            new AppConfiguration { MaxScannedItems = 1 });

        // Act
        var result = await browseQueryHandler.Handle(new BrowseQuery(string.Empty), CancellationToken.None);

        // Assert
        Assert.Equal("FileSystem.LimitExceeded", result.Error.Code);
        Assert.Equal(ErrorType.LimitExceeded, result.Error.Type);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Should_RejectFolderRows_When_ScanLimitIsExceeded()
    {
        // Arrange
        var mockGetFolderQueryHandler = new Mock<IQueryHandler<GetFolderItemsQuery, IReadOnlyList<FolderItemResponse>>>();
        mockGetFolderQueryHandler.Setup(x => x.Handle(
                It.IsAny<GetFolderItemsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<FolderItemResponse>>(
                new List<FolderItemResponse>()
                {
                    new FolderItemResponse
                    {
                        Id = new ("Photos"),
                        Name = "Photos",
                        RelativePath = "Photos"
                    },
                    new FolderItemResponse
                    {
                        Id = new ("Trips"),
                        Name = "Trips",
                        RelativePath = "Trips"
                    }
                }));

        var browseQueryHandler = new BrowseQueryHandler(
            mockGetFolderQueryHandler.Object,
            new FakeGetFileItemsQueryHandler(),
            new AppConfiguration { MaxScannedItems = 1 });

        // Act
        var result = await browseQueryHandler.Handle(new BrowseQuery(string.Empty), CancellationToken.None);

        // Assert
        Assert.Equal("FileSystem.LimitExceeded", result.Error.Code);
        Assert.Equal(ErrorType.LimitExceeded, result.Error.Type);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_FolderIsMissing()
    {
        // Arrange
        var mockGetFolderItemsQueryHandler = new Mock<IQueryHandler<GetFolderItemsQuery, IReadOnlyList<FolderItemResponse>>>();
        var mockGetFileItemsQueryHandler = new Mock<IQueryHandler<GetFileItemsQuery, IReadOnlyList<FileItemResponse>>>();
        mockGetFolderItemsQueryHandler.Setup(x => x.Handle(
                It.IsAny<GetFolderItemsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IReadOnlyList<FolderItemResponse>>(FileSystemErrors.FolderNotFound));

        var browseQueryHandler = new BrowseQueryHandler(
            mockGetFolderItemsQueryHandler.Object,
            mockGetFileItemsQueryHandler.Object,
            new AppConfiguration());

        // Act
        var result = await browseQueryHandler.Handle(new BrowseQuery("Gone"), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Folder.NotFound", result.Error.Code);
        mockGetFileItemsQueryHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_Should_SkipFileQuery_When_CancelledAfterFolderQuery()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var mockGetFolderItemsQueryHandler = new Mock<IQueryHandler<GetFolderItemsQuery, IReadOnlyList<FolderItemResponse>>>();
        mockGetFolderItemsQueryHandler.Setup(x => x.Handle(
                It.IsAny<GetFolderItemsQuery>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => cancellationTokenSource.Cancel())
            .ReturnsAsync(Result.Success<IReadOnlyList<FolderItemResponse>>([]));
        var fakeGetFileItemsQueryHandler = new FakeGetFileItemsQueryHandler();

        var browseQueryHandler = new BrowseQueryHandler(
            mockGetFolderItemsQueryHandler.Object,
            fakeGetFileItemsQueryHandler,
            new AppConfiguration());

        // Act/Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => browseQueryHandler.Handle(
                new BrowseQuery(string.Empty),
                cancellationTokenSource.Token));
        mockGetFolderItemsQueryHandler.Verify(
            x => x.Handle(It.IsAny<GetFolderItemsQuery>(), cancellationTokenSource.Token),
            Times.Once);
        Assert.Empty(fakeGetFileItemsQueryHandler.Requests);
    }

    [Fact]
    public async Task Handle_Should_ThrowOperationCanceledException_When_CancelledAfterFileQuery()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var mockGetFileItemsQueryHandler = new Mock<IQueryHandler<GetFileItemsQuery, IReadOnlyList<FileItemResponse>>>();
        mockGetFileItemsQueryHandler.Setup(x => x.Handle(
                It.IsAny<GetFileItemsQuery>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => cancellationTokenSource.Cancel())
            .ReturnsAsync(Result.Success<IReadOnlyList<FileItemResponse>>([]));
        var fakeGetFolderItemsQueryHandler = new FakeGetFolderItemsQueryHandler();

        var browseQueryHandler = new BrowseQueryHandler(
            fakeGetFolderItemsQueryHandler,
            mockGetFileItemsQueryHandler.Object,
            new AppConfiguration());

        // Act/Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => browseQueryHandler.Handle(
                new BrowseQuery(string.Empty),
                cancellationTokenSource.Token));
        mockGetFileItemsQueryHandler.Verify(
            x => x.Handle(It.IsAny<GetFileItemsQuery>(), cancellationTokenSource.Token),
            Times.Once);
        Assert.Equal([string.Empty], fakeGetFolderItemsQueryHandler.Requests);
    }
}
