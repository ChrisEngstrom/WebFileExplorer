using WebFileExplorer.SharedKernel;

namespace WebFileExplorer.Application.Abstractions.Messaging;

public interface ICommandHandler<TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Handles a command
    /// </summary>
    /// <param name="command">The command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response from the command</returns>
    Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}
