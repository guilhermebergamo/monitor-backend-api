using MonitorBackend.Application.Abstractions;
using MonitorBackend.Domain.Entities;

namespace MonitorBackend.Application.Commands;

/// <summary>
/// Command para registrar acesso do frontend (F5)
/// </summary>
public record RegistrarAcessoCommand(string Observacao) : ICommand<RegistrarAcessoCommandResult>;

/// <summary>
/// Resultado do comando de registro de acesso com lista completa atualizada e paginação
/// </summary>
public record RegistrarAcessoCommandResult
{
    public IEnumerable<Registro> Registros { get; init; } = [];
    public int TotalRegistros { get; init; }
    public long SomaQuantidade { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalPages { get; init; }
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static RegistrarAcessoCommandResult CreateSuccess(
        IEnumerable<Registro> registros,
        int totalRegistros,
        long somaQuantidade,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var lista = registros.ToList();
        var totalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);

        return new RegistrarAcessoCommandResult
        {
            Registros = lista,
            TotalRegistros = totalRegistros,
            SomaQuantidade = somaQuantidade,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            Success = true,
            Message = "Acesso registrado com sucesso"
        };
    }

    public static RegistrarAcessoCommandResult CreateFailure(string message)
    {
        return new RegistrarAcessoCommandResult
        {
            Success = false,
            Message = message
        };
    }
}
