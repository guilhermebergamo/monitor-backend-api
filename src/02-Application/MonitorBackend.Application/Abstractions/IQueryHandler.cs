namespace MonitorBackend.Application.Abstractions;

/// <summary>
/// Handler para executar uma Query específica.
/// Cada Query tem seu próprio Handler dedicado.
/// Diferente do pattern com IQueryHandler<TQuery, TResult>, aqui o handler
/// é específico e não precisa de interface genérica.
/// </summary>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken = default);
}
