namespace WebFileExplorer.Application.Abstractions.Messaging;

// Associates a query with its response type through IQueryHandler's constraint.
#pragma warning disable S2326
public interface IQuery<TResponse>
{
}
#pragma warning restore S2326
