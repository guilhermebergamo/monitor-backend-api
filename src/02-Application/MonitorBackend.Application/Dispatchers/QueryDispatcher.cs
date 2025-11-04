using MonitorBackend.Application.Abstractions;

namespace MonitorBackend.Application.Dispatchers;

/// <summary>
/// Implementação do Dispatcher de Queries.
/// Este componente resolve o Handler correto para cada Query usando DI.
/// É a alternativa ao MediatR - mais simples e performática.
/// </summary>
public sealed class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public QueryDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task<TResult> Dispatch<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        // Resolve o tipo do handler baseado no tipo da query
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));

        // Busca o handler no container de DI
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            throw new InvalidOperationException(
                $"Nenhum handler registrado para a query '{query.GetType().Name}'. " +
                $"Certifique-se de registrar '{handlerType.Name}' no container de DI.");
        }

        // Invoca o método Handle do handler
        var handleMethod = handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.Handle));

        if (handleMethod == null)
            throw new InvalidOperationException($"Método Handle não encontrado no handler '{handlerType.Name}'");

        var task = (Task<TResult>)handleMethod.Invoke(handler, new object[] { query, cancellationToken })!;

        return await task;
    }
}
