namespace MonitorBackend.Application.Abstractions;

/// <summary>
/// Interface para handlers de Commands
/// </summary>
public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> Handle(TCommand command, CancellationToken cancellationToken = default);
}
