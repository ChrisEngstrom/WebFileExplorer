using WebFileExplorer.Application.Abstractions.Messaging;
using WebFileExplorer.Application.FileSystemManagement.Queries.GetFileItems;
using WebFileExplorer.Application.Responses;
using WebFileExplorer.Domain.FileItems;
using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Application.UnitTests.Fakes.Queries;

internal sealed class FakeGetFileItemsQueryHandler : IQueryHandler<GetFileItemsQuery, IReadOnlyList<FileItemResponse>>
{
    public List<string> Requests { get; } = [];

    public Task<Result<IReadOnlyList<FileItemResponse>>> Handle(GetFileItemsQuery query, CancellationToken cancellationToken)
    {
        Requests.Add(query.folderItemId.Value);

        string name = string.IsNullOrEmpty(query.folderItemId.Value) ? "root.txt" : "nested.txt";
        string path = string.IsNullOrEmpty(query.folderItemId.Value) ? name : $"Photos/{name}";

        IReadOnlyList<FileItemResponse> result = new List<FileItemResponse>()
        {
            new FileItemResponse
            {
                Id = new FileItemId(path),
                RelativePath = path,
                Name = name,
                SizeBytes = string.IsNullOrEmpty(query.folderItemId.Value) ? 13 : 58
            }
        };

        return Task.FromResult(Result.Success(result));
    }
}
