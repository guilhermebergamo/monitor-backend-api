using MonitorBackend.Application.Abstractions;
using MonitorBackend.Domain.Entities;

namespace MonitorBackend.Application.Queries;

/// <summary>
/// Query para obter todos os registros com paginação.
/// Esta é uma Query pura - apenas dados de entrada para a consulta.
/// Não contém lógica de negócio.
/// </summary>
public sealed record GetRegistrosQuery : IQuery<GetRegistrosQueryResult>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;

    // Propriedades opcionais para filtros futuros
    // public DateTime? DataInicio { get; init; }
    // public DateTime? DataFim { get; init; }
}

/// <summary>
/// Resultado da Query GetRegistros com paginação.
/// Encapsula a resposta da consulta.
/// </summary>
public sealed record GetRegistrosQueryResult
{
    public IEnumerable<Registro> Registros { get; init; } = Array.Empty<Registro>();
    public int TotalRegistros { get; init; }
    public long SomaQuantidade { get; init; } // Soma total da coluna quantidade
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static GetRegistrosQueryResult CreateSuccess(
        IEnumerable<Registro> registros,
        int totalRegistros,
        long somaQuantidade,
        int pageNumber,
        int pageSize)
    {
        var lista = registros.ToList();
        var totalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);

        return new GetRegistrosQueryResult
        {
            Registros = lista,
            TotalRegistros = totalRegistros,
            SomaQuantidade = somaQuantidade,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            Success = true,
            Message = "Registros obtidos com sucesso"
        };
    }

    public static GetRegistrosQueryResult CreateError(string message)
    {
        return new GetRegistrosQueryResult
        {
            Registros = Array.Empty<Registro>(),
            TotalRegistros = 0,
            SomaQuantidade = 0,
            PageNumber = 1,
            PageSize = 10,
            TotalPages = 0,
            Success = false,
            Message = message
        };
    }
}
