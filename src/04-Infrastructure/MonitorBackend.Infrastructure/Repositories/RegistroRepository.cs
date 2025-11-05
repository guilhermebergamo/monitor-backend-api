using Dapper;
using MonitorBackend.Domain.Entities;
using MonitorBackend.Domain.Repositories;
using MonitorBackend.Infrastructure.Data;

namespace MonitorBackend.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório de Registros usando Dapper.
/// Dapper oferece alta performance com controle total sobre o SQL.
/// </summary>
public sealed class RegistroRepository : IRegistroRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RegistroRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IEnumerable<Registro>> GetAllAsync()
    {
        const string sql = @"
            SELECT 
                observacao AS Observacao,
                datahora AS DataHora,
                quantidade AS Quantidade
            FROM registros
            ORDER BY datahora DESC";

        using var connection = _connectionFactory.CreateConnection();

        // Dapper automaticamente mapeia as colunas para as propriedades da entidade
        var registros = await connection.QueryAsync<Registro>(sql);

        return registros;
    }

    public async Task<(IEnumerable<Registro> Registros, int TotalCount, long SomaQuantidade)> GetPaginatedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Calcula o OFFSET
        var offset = (pageNumber - 1) * pageSize;

        const string sqlCount = "SELECT COUNT(*) FROM registros";
        const string sqlSum = "SELECT COALESCE(SUM(quantidade), 0) FROM registros";

        const string sqlData = @"
            SELECT 
                observacao AS Observacao,
                datahora AS DataHora,
                quantidade AS Quantidade
            FROM registros
            ORDER BY datahora DESC
            LIMIT @PageSize OFFSET @Offset";

        using var connection = _connectionFactory.CreateConnection();

        // Busca o total de registros
        var totalCount = await connection.ExecuteScalarAsync<int>(sqlCount);

        // Busca a soma total da quantidade
        var somaQuantidade = await connection.ExecuteScalarAsync<long>(sqlSum);

        // Busca os registros paginados
        var registros = await connection.QueryAsync<Registro>(sqlData, new
        {
            PageSize = pageSize,
            Offset = offset
        });

        return (registros, totalCount, somaQuantidade);
    }

    public async Task<Registro?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT 
                observacao AS Observacao,
                data_hora AS DataHora,
                quantidade AS Quantidade
            FROM registros
            WHERE id = @Id";

        using var connection = _connectionFactory.CreateConnection();

        var registro = await connection.QueryFirstOrDefaultAsync<Registro>(sql, new { Id = id });

        return registro;
    }

    public async Task<int> InsertAsync(string observacao, DateTime dataHora, int quantidade)
    {
        const string sql = @"
            INSERT INTO registros (observacao, datahora, quantidade)
            VALUES (@Observacao, @DataHora, @Quantidade)
            RETURNING id";

        using var connection = _connectionFactory.CreateConnection();

        var id = await connection.ExecuteScalarAsync<int>(sql, new
        {
            Observacao = observacao,
            DataHora = dataHora,
            Quantidade = quantidade
        });

        return id;
    }

    public async Task<int> GetUltimoRegistroAsync(CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT 
                quantidade
            FROM registros
            ORDER BY id DESC
            LIMIT 1";

        using var connection = _connectionFactory.CreateConnection();

        // Retorna a quantidade do último registro, ou 0 se não houver registros
        var quantidade = await connection.QueryFirstOrDefaultAsync<int?>(sql);

        return quantidade ?? 0;
    }
}
