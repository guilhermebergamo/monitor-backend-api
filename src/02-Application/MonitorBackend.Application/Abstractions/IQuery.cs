namespace MonitorBackend.Application.Abstractions;

/// <summary>
/// Marker interface para Queries no padrão CQRS.
/// Uma Query representa uma intenção de leitura de dados.
/// </summary>
public interface IQuery<out TResult>
{
}
