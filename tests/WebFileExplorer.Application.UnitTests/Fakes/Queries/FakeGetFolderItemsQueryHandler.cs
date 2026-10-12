using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.FileSystemManagement.Queries.GetFolderItems;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.Domain.FolderItems;
using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Application.UnitTests.Fakes.Queries;

internal sealed class FakeGetFolderItemsQueryHandler :
    IQueryHandler<GetFolderItemsQuery, IReadOnlyList<FolderItemResponse>>
{
    public List<string> Requests { get; } = [];

    public Task<Result<IReadOnlyList<FolderItemResponse>>> Handle(GetFolderItemsQuery query, CancellationToken cancellationToken)
    {
        Requests.Add(query.RelativeFolderPath);

        IReadOnlyList<FolderItemResponse> result;

        if (string.IsNullOrEmpty(query.RelativeFolderPath))
        {
            result = new List<FolderItemResponse>()
            {
                new FolderItemResponse
                {
                    Id = new FolderItemId("Photos"),
                    RelativePath = "Photos",
                    Name = "Photos"
                }
            };
        }
        else
        {
            result = new List<FolderItemResponse>();
        }

        return Task.FromResult(Result.Success(result));
    }
}
