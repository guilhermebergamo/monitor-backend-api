namespace MonitorBackend.Application.Abstractions;

/// <summary>
/// Interface para despachar Commands (operações de escrita)
/// </summary>
public interface ICommandDispatcher
{
    Task<TResult> Dispatch<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
}
