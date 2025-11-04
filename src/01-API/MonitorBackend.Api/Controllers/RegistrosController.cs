using Microsoft.AspNetCore.Mvc;
using MonitorBackend.Application.Abstractions;
using MonitorBackend.Application.Commands;
using MonitorBackend.Application.Queries;

namespace MonitorBackend.Api.Controllers;

/// <summary>
/// Controller responsável pelos endpoints de Registros.
/// Utiliza CQRS com Dispatcher (sem MediatR).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class RegistrosController : ControllerBase
{
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly ILogger<RegistrosController> _logger;

    public RegistrosController(
        IQueryDispatcher queryDispatcher,
        ICommandDispatcher commandDispatcher,
        ILogger<RegistrosController> logger)
    {
        _queryDispatcher = queryDispatcher ?? throw new ArgumentNullException(nameof(queryDispatcher));
        _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Obtém todos os registros do banco de dados com paginação.
    /// GET /api/registros?pageNumber=1&pageSize=10
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetRegistrosQueryResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processando GET /api/registros?pageNumber={PageNumber}&pageSize={PageSize}",
                pageNumber, pageSize);

            // Cria a query com paginação
            var query = new GetRegistrosQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // Dispatcha a query para o handler correto
            var result = await _queryDispatcher.Dispatch(query, cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Erro ao buscar registros: {Message}", result.Message);
                return StatusCode(500, result);
            }

            _logger.LogInformation("Retornando {Count} registros", result.TotalRegistros);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado ao processar GET /api/registros");
            return StatusCode(500, new { message = "Erro interno do servidor", error = ex.Message });
        }
    }

    /// <summary>
    /// Registra um acesso do frontend (equivalente a F5).
    /// POST /api/registros/acesso
    /// </summary>
    /// <param name="request">Dados do acesso (observação)</param>
    [HttpPost("acesso")]
    [ProducesResponseType(typeof(RegistrarAcessoCommandResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegistrarAcesso(
        [FromBody] RegistrarAcessoRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Registrando acesso: {Observacao}", request.Observacao);

            // Cria o command
            var command = new RegistrarAcessoCommand(request.Observacao);

            // Dispatcha o command para o handler correto
            var result = await _commandDispatcher.Dispatch(command, cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning("Falha ao registrar acesso: {Message}", result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("Acesso registrado com sucesso.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado ao processar POST /api/registros/acesso");
            return StatusCode(500, new { message = "Erro interno do servidor", error = ex.Message });
        }
    }

    /// <summary>
    /// Health check do controller.
    /// GET /api/registros/health
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            controller = nameof(RegistrosController)
        });
    }
}

/// <summary>
/// Request para registrar acesso
/// </summary>
public record RegistrarAcessoRequest(string Observacao);
