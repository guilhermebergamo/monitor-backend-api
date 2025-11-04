namespace MonitorBackend.Application.Abstractions;

/// <summary>
/// Dispatcher simples para executar Queries.
/// Este é o ponto central do CQRS sem usar MediatR.
/// O Controller chama o Dispatcher, que encontra e executa o Handler correto.
/// </summary>
public interface IQueryDispatcher
{
    Task<TResult> Dispatch<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}
