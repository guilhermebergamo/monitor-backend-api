using MonitorBackend.Application.Abstractions;
using MonitorBackend.Domain.Repositories;

namespace MonitorBackend.Application.Queries;

/// <summary>
/// Handler responsável por processar GetRegistrosQuery.
/// Este handler é específico e dedicado a esta Query.
/// Não usa interface de service layer - implementação direta do CQRS.
/// </summary>
public sealed class GetRegistrosQueryHandler : IQueryHandler<GetRegistrosQuery, GetRegistrosQueryResult>
{
    private readonly IRegistroRepository _repository;

    public GetRegistrosQueryHandler(IRegistroRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<GetRegistrosQueryResult> Handle(
        GetRegistrosQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validação da paginação
            var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
            var pageSize = query.PageSize < 1 ? 10 : query.PageSize > 100 ? 100 : query.PageSize;

            // Busca os registros paginados no repositório
            var (registros, totalCount, somaQuantidade) = await _repository.GetPaginatedAsync(
                pageNumber,
                pageSize,
                cancellationToken);

            // Retorna o resultado usando factory method
            return GetRegistrosQueryResult.CreateSuccess(registros, totalCount, somaQuantidade, pageNumber, pageSize);
        }
        catch (Exception ex)
        {
            // Em produção, usar ILogger para registrar o erro
            return GetRegistrosQueryResult.CreateError($"Erro ao buscar registros: {ex.Message}");
        }
    }
}
