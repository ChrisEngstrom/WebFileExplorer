using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.FileSystemManagement.Queries.BrowseFileSystem;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Application.UnitTests.Fakes.Queries;

// Configurable immediate listings let Search tests describe a tree without running Browse.
internal sealed class FakeBrowseQueryHandler : IQueryHandler<BrowseQuery, FileSystemItemsResponse>
{
    public Dictionary<string, FileSystemItemsResponse> Responses { get; } = [];

    public List<string> Requests { get; } = [];

    public Task<Result<FileSystemItemsResponse>> Handle(BrowseQuery query, CancellationToken cancellationToken)
    {
        Requests.Add(query.Path);

        return Task.FromResult(Result.Success(Responses[query.Path]));
    }
}
