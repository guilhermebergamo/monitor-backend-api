using MonitorBackend.Domain.Entities;

namespace MonitorBackend.Domain.Repositories;

/// <summary>
/// Contrato do repositório de Registros.
/// Define operações de acesso a dados seguindo Repository Pattern.
/// </summary>
public interface IRegistroRepository
{
    Task<IEnumerable<Registro>> GetAllAsync();
    Task<(IEnumerable<Registro> Registros, int TotalCount, long SomaQuantidade)> GetPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Registro?> GetByIdAsync(int id);
    Task<int> GetUltimoRegistroAsync(CancellationToken cancellationToken);
    Task<int> InsertAsync(string observacao, DateTime dataHora, int quantidade);
}
