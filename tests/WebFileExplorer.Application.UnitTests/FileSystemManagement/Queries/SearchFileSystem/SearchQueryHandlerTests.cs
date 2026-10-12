using Moq;
using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.Configurations;
using WebFileExplorer.Application.FileSystemManagement.Queries.BrowseFileSystem;
using WebFileExplorer.Application.FileSystemManagement.Queries.SearchFileSystem;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.Application.UnitTests.Fakes.Queries;
using WebFileExplorer.SharedKernel;
using Xunit.Sdk;

namespace WebFileExplorer.Application.UnitTests.FileSystemManagement.Queries.SearchFileSystem;

public class SearchQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnInvalidPath_WithoutBrowsing_When_PathIsUnsafe()
    {
        // Arrange
        var mockBrowseQueryHandler = new Mock<IQueryHandler<BrowseQuery, FileSystemItemsResponse>>();
        var searchQueryHandler = new SearchQueryHandler(mockBrowseQueryHandler.Object, new AppConfiguration());

        // Act
        var result = await searchQueryHandler.Handle(
            new SearchQuery("../outside", "photo"),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Path.Invalid", result.Error.Code);
        mockBrowseQueryHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_Should_SortFoldersBeforeFiles_When_BrowseReturnsUnsortedItems()
    {
        // Arrange
        var fakeBrowseQueryHandler = new FakeBrowseQueryHandler();
        fakeBrowseQueryHandler.Responses[string.Empty] = new FileSystemItemsResponse(
            string.Empty,
            new List<FileSystemItemResponse>()
            {
                new FileSystemItemResponse(
                    "match-z.txt",
                    "match-z.txt",
                    false,
                    3),
                new FileSystemItemResponse(
                    "match-Z",
                    "match-Z",
                    true,
                    null),
                new FileSystemItemResponse(
                    "match-a.txt",
                    "match-a.txt",
                    false,
                    2),
                new FileSystemItemResponse(
                    "match-A",
                    "match-A",
                    true,
                    null)
            },
            2,
            2,
            5);
        fakeBrowseQueryHandler.Responses["match-Z"] = EmptyListing("match-Z");
        fakeBrowseQueryHandler.Responses["match-A"] = EmptyListing("match-A");

        var searchQueryHandler = new SearchQueryHandler(fakeBrowseQueryHandler, new AppConfiguration());

        // Act
        var result = await searchQueryHandler.Handle(new SearchQuery(string.Empty, "match"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(
            ["match-A", "match-Z", "match-a.txt", "match-z.txt"],
            result.Value.Items.Select(item => item.RelativePath));
        Assert.Equal(2, result.Value.FolderCount);
        Assert.Equal(2, result.Value.FileCount);
        Assert.Equal(5, result.Value.TotalSizeBytes);
    }

    [Fact]
    public async Task Handle_Should_ReturnMatchingDescendants_When_SearchingRecursively()
    {
        // Arrange
        var fakeBrowseQueryHandler = NewBrowseTree();
        var searchQueryHandler = new SearchQueryHandler(fakeBrowseQueryHandler, new AppConfiguration());

        // Act
        var result = await searchQueryHandler.Handle(new SearchQuery(string.Empty, "nested"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Photos/nested.txt", Assert.Single(result.Value.Items).RelativePath);
        Assert.Equal(0, result.Value.FolderCount);
        Assert.Equal(1, result.Value.FileCount);
        Assert.Equal(58, result.Value.TotalSizeBytes);

        // Photos does not match, but Search must still visit it to find a matching descendant.
        Assert.Equal([string.Empty, "Photos"], fakeBrowseQueryHandler.Requests);
    }

    [Fact]
    public async Task Handle_Should_ReportZeroFileBytes_When_OnlyFolderMatches()
    {
        // Arrange
        var searchQueryHandler = new SearchQueryHandler(NewBrowseTree(), new AppConfiguration());

        // Act
        var result = await searchQueryHandler.Handle(new SearchQuery(string.Empty, "Photos"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(Assert.Single(result.Value.Items).IsDirectory);
        Assert.Equal(1, result.Value.FolderCount);
        Assert.Equal(0, result.Value.FileCount);
        Assert.Equal(0, result.Value.TotalSizeBytes);
    }

    [Fact]
    public async Task Handle_Should_MatchIgnoringCase_When_QueryHasSurroundingWhitespace()
    {
        // Arrange
        var searchQueryHandler = new SearchQueryHandler(NewBrowseTree(), new AppConfiguration());

        // Act
        var result = await searchQueryHandler.Handle(
            new SearchQuery(string.Empty, "  NeStEd  "),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Photos/nested.txt", Assert.Single(result.Value.Items).RelativePath);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmptyTotals_When_NoNamesMatch()
    {
        // Arrange
        var fakeBrowseQueryHandler = NewBrowseTree();
        var searchQueryHandler = new SearchQueryHandler(fakeBrowseQueryHandler, new AppConfiguration());

        // Act
        var result = await searchQueryHandler.Handle(new SearchQuery(string.Empty, "absent"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.FolderCount);
        Assert.Equal(0, result.Value.FileCount);
        Assert.Equal(0, result.Value.TotalSizeBytes);
        Assert.Equal([string.Empty, "Photos"], fakeBrowseQueryHandler.Requests);
    }

    [Fact]
    public async Task Handle_Should_AcceptQuery_When_LengthIs200Characters()
    {
        // Arrange
        var searchQueryHandler = new SearchQueryHandler(NewBrowseTree(), new AppConfiguration());

        // Act
        var result = await searchQueryHandler.Handle(
            new SearchQuery(string.Empty, new string('x', 200)),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData('x')]
    [InlineData(' ')]
    public async Task Handle_Should_RejectQueryWithoutBrowsing_When_LengthExceeds200Characters(char character)
    {
        // Arrange
        var mockBrowseQueryHandler = new Mock<IQueryHandler<BrowseQuery, FileSystemItemsResponse>>();
        var searchQueryHandler = new SearchQueryHandler(mockBrowseQueryHandler.Object, new AppConfiguration());

        // Act
        var result = await searchQueryHandler.Handle(
            new SearchQuery(string.Empty, new string(character, 201)),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Browse.SearchTooLong", result.Error.Code);
        mockBrowseQueryHandler.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Photos")]
    public async Task Handle_Should_ReturnExactBrowseFailure_When_BrowseReturnsFailure(string failedPath)
    {
        // Arrange
        var error = Error.NotFound("Test.BrowseFailure", "A supplied browse failure.");
        var fakeBrowseQueryHandler = NewBrowseTree();
        var mockBrowseQueryHandler = new Mock<IQueryHandler<BrowseQuery, FileSystemItemsResponse>>();
        mockBrowseQueryHandler.Setup(x => x.Handle(
                It.IsAny<BrowseQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BrowseQuery query, CancellationToken _) =>
                query.Path == failedPath
                    ? Result.Failure<FileSystemItemsResponse>(error)
                    : Result.Success(fakeBrowseQueryHandler.Responses[query.Path]));

        var searchQueryHandler = new SearchQueryHandler(mockBrowseQueryHandler.Object, new AppConfiguration());

        // Act
        var result = await searchQueryHandler.Handle(new SearchQuery(string.Empty, "photo"), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        mockBrowseQueryHandler.Verify(
            x => x.Handle(new BrowseQuery(failedPath), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowOperationCanceledException_When_CancelledAfterBrowse()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var mockBrowseQueryHandler = new Mock<IQueryHandler<BrowseQuery, FileSystemItemsResponse>>();
        mockBrowseQueryHandler.Setup(x => x.Handle(
                It.IsAny<BrowseQuery>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => cancellationTokenSource.Cancel())
            .ReturnsAsync(Result.Success(NewBrowseTree().Responses[string.Empty]));

        var searchQueryHandler = new SearchQueryHandler(mockBrowseQueryHandler.Object, new AppConfiguration());

        // Act/Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => searchQueryHandler.Handle(
                new SearchQuery(string.Empty, "nested"),
                cancellationTokenSource.Token));
        mockBrowseQueryHandler.Verify(
            x => x.Handle(new BrowseQuery(string.Empty), cancellationTokenSource.Token),
            Times.Once);
        mockBrowseQueryHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_Should_ReturnLimitExceeded_When_TotalScannedItemsExceedLimit()
    {
        // Arrange
        var fakeBrowseQueryHandler = NewBrowseTree();
        var searchQueryHandler = new SearchQueryHandler(fakeBrowseQueryHandler, new AppConfiguration { MaxScannedItems = 2 });

        // Act
        var result = await searchQueryHandler.Handle(new SearchQuery(string.Empty, "no-match"), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.LimitExceeded, result.Error.Type);
        Assert.Equal("FileSystem.LimitExceeded", result.Error.Code);

        // Each listing fits the limit, but the aggregate includes all three nonmatching rows.
        Assert.Equal([string.Empty, "Photos"], fakeBrowseQueryHandler.Requests);
    }

    [Fact]
    public async Task Handle_Should_Succeed_When_TotalScannedItemsEqualLimit()
    {
        // Arrange
        var fakeBrowseQueryHandler = NewBrowseTree();
        var searchQueryHandler = new SearchQueryHandler(fakeBrowseQueryHandler, new AppConfiguration { MaxScannedItems = 3 });

        // Act
        var result = await searchQueryHandler.Handle(new SearchQuery(string.Empty, "no-match"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal([string.Empty, "Photos"], fakeBrowseQueryHandler.Requests);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Handle_Should_BrowseOnce_When_QueryIsWhitespaceWithinLimit(string search)
    {
        // Arrange
        var fakeBrowseQueryHandler = NewBrowseTree();
        var searchQueryHandler = new SearchQueryHandler(fakeBrowseQueryHandler, new AppConfiguration());

        // Act
        var result = await searchQueryHandler.Handle(new SearchQuery(string.Empty, search), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Same(fakeBrowseQueryHandler.Responses[string.Empty], result.Value);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal([string.Empty], fakeBrowseQueryHandler.Requests);
    }

    [Fact]
    public async Task Handle_Should_ThrowArgumentNullException_When_RequestIsNull()
    {
        // Arrange
        var mockBrowseQueryHandler = new Mock<IQueryHandler<BrowseQuery, FileSystemItemsResponse>>();
        var searchQueryHandler = new SearchQueryHandler(mockBrowseQueryHandler.Object, new AppConfiguration());

        // Act/Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => searchQueryHandler.Handle(null!, CancellationToken.None));
        mockBrowseQueryHandler.VerifyNoOtherCalls();
    }

    private static FakeBrowseQueryHandler NewBrowseTree()
    {
        var fakeBrowseQueryHandler = new FakeBrowseQueryHandler();
        fakeBrowseQueryHandler.Responses[string.Empty] = new FileSystemItemsResponse(
            string.Empty,
            new List<FileSystemItemResponse>()
            {
                new FileSystemItemResponse(
                    "Photos",
                    "Photos",
                    true,
                    null),
                new FileSystemItemResponse(
                    "root.txt",
                    "root.txt",
                    false,
                    13)
            },
            1,
            1,
            13);
        fakeBrowseQueryHandler.Responses["Photos"] = new FileSystemItemsResponse(
            "Photos",
            new List<FileSystemItemResponse>()
            {
                new FileSystemItemResponse(
                    "nested.txt",
                    "Photos/nested.txt",
                    false,
                    58)
            },
            0,
            1,
            58);

        return fakeBrowseQueryHandler;
    }

    private static FileSystemItemsResponse EmptyListing(string path) => new (
        path,
        [],
        0,
        0,
        0);
}
