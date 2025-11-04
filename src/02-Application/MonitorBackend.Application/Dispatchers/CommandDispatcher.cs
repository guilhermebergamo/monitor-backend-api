using MonitorBackend.Application.Abstractions;

namespace MonitorBackend.Application.Dispatchers;

/// <summary>
/// Dispatcher que resolve e executa CommandHandlers dinamicamente
/// </summary>
public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResult> Dispatch<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        var commandType = command.GetType();
        var resultType = typeof(TResult);
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, resultType);

        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null)
        {
            throw new InvalidOperationException($"Handler not found for command type {commandType.Name}");
        }

        var method = handlerType.GetMethod("Handle");
        if (method == null)
        {
            throw new InvalidOperationException($"Handle method not found on handler for {commandType.Name}");
        }

        var task = (Task<TResult>)method.Invoke(handler, new object[] { command, cancellationToken })!;
        return await task;
    }
}
