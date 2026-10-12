using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Application.Abstractions.Messaging;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Handles a query
    /// </summary>
    /// <param name="query">The query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response from the query</returns>
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
