using Microsoft.AspNetCore.Mvc;
using MonitorBackend.Infrastructure.Services;

namespace MonitorBackend.Api.Controllers;

/// <summary>
/// Controller com endpoints extremamente pesados para testes de carga e consumo de recursos.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HeavyOpsController : ControllerBase
{
    private readonly IHeavyProcessingService _processingService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<HeavyOpsController> _logger;

    public HeavyOpsController(
        IHeavyProcessingService processingService,
        ICacheService cacheService,
        ILogger<HeavyOpsController> logger)
    {
        _processingService = processingService;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Processa uma grande quantidade de dados em memória.
    /// GET /api/heavyops/process-data?sizeMB=100
    /// </summary>
    /// <param name="sizeMB">Tamanho dos dados em MB (padrão: 50MB, máx: 500MB)</param>
    [HttpGet("process-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessLargeData(
        [FromQuery] int sizeMB = 50,
        CancellationToken cancellationToken = default)
    {
        if (sizeMB < 1 || sizeMB > 500)
        {
            return BadRequest(new { message = "sizeMB deve estar entre 1 e 500" });
        }

        _logger.LogInformation("Processando {SizeMB}MB de dados via API...", sizeMB);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var data = await _processingService.ProcessLargeDataAsync(sizeMB, cancellationToken);
        stopwatch.Stop();

        return Ok(new
        {
            success = true,
            processedSizeMB = data.Length / 1024 / 1024,
            durationMs = stopwatch.ElapsedMilliseconds,
            hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(data))[..16],
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Realiza cálculos matemáticos intensivos (CPU-bound).
    /// GET /api/heavyops/cpu-intensive?iterations=1000000
    /// </summary>
    /// <param name="iterations">Número de iterações (padrão: 1M, máx: 10M)</param>
    [HttpGet("cpu-intensive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CpuIntensiveCalculation(
        [FromQuery] int iterations = 1_000_000,
        CancellationToken cancellationToken = default)
    {
        if (iterations < 1000 || iterations > 10_000_000)
        {
            return BadRequest(new { message = "iterations deve estar entre 1.000 e 10.000.000" });
        }

        _logger.LogInformation("Iniciando cálculo intensivo com {Iterations:N0} iterações...", iterations);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _processingService.PerformCpuIntensiveCalculationAsync(iterations, cancellationToken);
        stopwatch.Stop();

        return Ok(new
        {
            success = true,
            iterations,
            result,
            durationMs = stopwatch.ElapsedMilliseconds,
            cpuTimeSeconds = stopwatch.Elapsed.TotalSeconds,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Processa múltiplos itens em paralelo (usa múltiplos cores).
    /// GET /api/heavyops/parallel-processing?itemCount=50
    /// </summary>
    /// <param name="itemCount">Número de itens para processar (padrão: 50, máx: 200)</param>
    [HttpGet("parallel-processing")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ParallelProcessing(
        [FromQuery] int itemCount = 50,
        CancellationToken cancellationToken = default)
    {
        if (itemCount < 1 || itemCount > 200)
        {
            return BadRequest(new { message = "itemCount deve estar entre 1 e 200" });
        }

        _logger.LogInformation("Processando {ItemCount} itens em paralelo...", itemCount);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = await _processingService.ProcessInParallelAsync(itemCount, cancellationToken);
        stopwatch.Stop();

        return Ok(new
        {
            success = true,
            itemCount = results.Count,
            totalDataSizeMB = results.Sum(r => r.DataSize) / 1024 / 1024,
            durationMs = stopwatch.ElapsedMilliseconds,
            avgComputationPerItem = results.Average(r => r.Computation),
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Gera um relatório grande com muitos registros.
    /// GET /api/heavyops/generate-report?recordCount=50000
    /// </summary>
    /// <param name="recordCount">Número de registros (padrão: 50k, máx: 500k)</param>
    [HttpGet("generate-report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateLargeReport(
        [FromQuery] int recordCount = 50_000,
        CancellationToken cancellationToken = default)
    {
        if (recordCount < 100 || recordCount > 500_000)
        {
            return BadRequest(new { message = "recordCount deve estar entre 100 e 500.000" });
        }

        _logger.LogInformation("Gerando relatório com {RecordCount:N0} registros...", recordCount);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var report = await _processingService.GenerateLargeReportAsync(recordCount, cancellationToken);
        stopwatch.Stop();

        return Ok(new
        {
            success = true,
            recordCount,
            reportSizeMB = report.Length / 1024 / 1024,
            reportSizeKB = report.Length / 1024,
            durationMs = stopwatch.ElapsedMilliseconds,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Stresstest completo: combina todos os processos pesados.
    /// POST /api/heavyops/stress-test
    /// </summary>
    [HttpPost("stress-test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> StressTest(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("=== STRESS TEST INICIADO ===");
        var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

        var results = new List<string>();

        try
        {
            // 1. Processamento de dados grandes
            _logger.LogInformation("[1/5] Processamento de 100MB de dados...");
            var data = await _processingService.ProcessLargeDataAsync(100, cancellationToken);
            results.Add($"✓ Processados {data.Length / 1024 / 1024}MB de dados");

            // 2. Cálculo intensivo de CPU
            _logger.LogInformation("[2/5] Cálculo intensivo de CPU (5M iterações)...");
            var cpuResult = await _processingService.PerformCpuIntensiveCalculationAsync(5_000_000, cancellationToken);
            results.Add($"✓ Cálculo CPU concluído: {cpuResult}");

            // 3. Processamento paralelo
            _logger.LogInformation("[3/5] Processamento paralelo (100 itens)...");
            var parallelResults = await _processingService.ProcessInParallelAsync(100, cancellationToken);
            results.Add($"✓ Processados {parallelResults.Count} itens em paralelo");

            // 4. Geração de relatório
            _logger.LogInformation("[4/5] Geração de relatório (100k registros)...");
            var report = await _processingService.GenerateLargeReportAsync(100_000, cancellationToken);
            results.Add($"✓ Relatório gerado: {report.Length / 1024 / 1024}MB");

            // 5. Cache warming
            _logger.LogInformation("[5/5] Cache warming (50 itens)...");
            var random = new Random();
            for (int i = 0; i < 50; i++)
            {
                var cacheData = new byte[100 * 1024]; // 100KB
                random.NextBytes(cacheData);
                await _cacheService.SetAsync($"stress:test:{i}", Convert.ToBase64String(cacheData), TimeSpan.FromMinutes(5));
            }
            results.Add("✓ Cache aquecido (50 itens, ~5MB)");

            totalStopwatch.Stop();

            _logger.LogWarning("=== STRESS TEST CONCLUÍDO em {Duration}ms ===", totalStopwatch.ElapsedMilliseconds);

            return Ok(new
            {
                success = true,
                message = "Stress test concluído com sucesso!",
                totalDurationMs = totalStopwatch.ElapsedMilliseconds,
                totalDurationSeconds = totalStopwatch.Elapsed.TotalSeconds,
                operations = results,
                memoryUsedMB = GC.GetTotalMemory(false) / 1024 / 1024,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante stress test");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                completedOperations = results
            });
        }
    }

    /// <summary>
    /// Aloca e mantém memória por um tempo (memory leak intencional para testes).
    /// POST /api/heavyops/memory-leak?sizeMB=100&durationSeconds=30
    /// </summary>
    [HttpPost("memory-leak")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> IntentionalMemoryLeak(
        [FromQuery] int sizeMB = 100,
        [FromQuery] int durationSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        if (sizeMB < 1 || sizeMB > 500)
            return BadRequest(new { message = "sizeMB deve estar entre 1 e 500" });

        if (durationSeconds < 1 || durationSeconds > 300)
            return BadRequest(new { message = "durationSeconds deve estar entre 1 e 300" });

        _logger.LogWarning("⚠️ Memory leak intencional: {SizeMB}MB por {Duration}s", sizeMB, durationSeconds);

        var data = new byte[sizeMB * 1024 * 1024];
        new Random().NextBytes(data);

        // Mantém na memória
        await Task.Delay(TimeSpan.FromSeconds(durationSeconds), cancellationToken);

        return Ok(new
        {
            success = true,
            allocatedSizeMB = data.Length / 1024 / 1024,
            heldForSeconds = durationSeconds,
            message = "Memória alocada e mantida pelo período especificado",
            timestamp = DateTime.UtcNow
        });
    }
}
