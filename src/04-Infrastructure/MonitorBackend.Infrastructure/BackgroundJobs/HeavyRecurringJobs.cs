using Hangfire;
using Microsoft.Extensions.Logging;
using MonitorBackend.Infrastructure.Services;

namespace MonitorBackend.Infrastructure.BackgroundJobs;

/// <summary>
/// Jobs extremamente pesados para consumir recursos massivamente.
/// </summary>
public static class HeavyRecurringJobs
{
    public static void ConfigureHeavyJobs()
    {
        // Job 1: Processamento pesado a cada 2 minutos
        RecurringJob.AddOrUpdate<HeavyDataProcessingJob>(
            "heavy-data-processing",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/2 * * * *"); // A cada 2 minutos

        // Job 2: Cálculos intensivos a cada 3 minutos
        RecurringJob.AddOrUpdate<IntensiveCpuJob>(
            "intensive-cpu-calculation",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/3 * * * *"); // A cada 3 minutos

        // Job 3: Processamento paralelo a cada 5 minutos
        RecurringJob.AddOrUpdate<ParallelProcessingJob>(
            "parallel-processing",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/5 * * * *"); // A cada 5 minutos

        // Job 4: Geração de relatório grande a cada 10 minutos
        RecurringJob.AddOrUpdate<LargeReportGenerationJob>(
            "large-report-generation",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/10 * * * *"); // A cada 10 minutos

        // Job 5: Limpeza de cache e re-cache (pesado) a cada 15 minutos
        RecurringJob.AddOrUpdate<CacheWarmupJob>(
            "cache-warmup",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/15 * * * *"); // A cada 15 minutos
    }
}

/// <summary>
/// Job que processa grandes volumes de dados.
/// Consome: 100-200MB de RAM por execução.
/// </summary>
public class HeavyDataProcessingJob
{
    private readonly IHeavyProcessingService _processingService;
    private readonly IResourceTelemetryService _telemetry;
    private readonly Microsoft.Extensions.Logging.ILogger<HeavyDataProcessingJob> _logger;

    public HeavyDataProcessingJob(
        IHeavyProcessingService processingService,
        IResourceTelemetryService telemetry,
        Microsoft.Extensions.Logging.ILogger<HeavyDataProcessingJob> logger)
    {
        _processingService = processingService;
        _telemetry = telemetry;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _telemetry.LogResourceUsage("HeavyDataProcessingJob - Start");
        _logger.LogInformation("=== HeavyDataProcessingJob iniciado ===");

        try
        {
            // Processa 50MB de dados
            var data1 = await _processingService.ProcessLargeDataAsync(50, cancellationToken);
            await Task.Delay(1000, cancellationToken);

            // Processa mais 50MB
            var data2 = await _processingService.ProcessLargeDataAsync(50, cancellationToken);
            await Task.Delay(1000, cancellationToken);

            // Força GC para ver se consegue liberar
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            _logger.LogInformation("HeavyDataProcessingJob concluído. Processados: {TotalMB}MB",
                (data1.Length + data2.Length) / 1024 / 1024);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no HeavyDataProcessingJob");
        }
        finally
        {
            _telemetry.LogResourceUsage("HeavyDataProcessingJob - End");
        }
    }
}

/// <summary>
/// Job que realiza cálculos matemáticos intensivos.
/// Consome: 100% CPU durante execução.
/// </summary>
public class IntensiveCpuJob
{
    private readonly IHeavyProcessingService _processingService;
    private readonly IResourceTelemetryService _telemetry;
    private readonly Microsoft.Extensions.Logging.ILogger<IntensiveCpuJob> _logger;

    public IntensiveCpuJob(
        IHeavyProcessingService processingService,
        IResourceTelemetryService telemetry,
        Microsoft.Extensions.Logging.ILogger<IntensiveCpuJob> logger)
    {
        _processingService = processingService;
        _telemetry = telemetry;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _telemetry.LogResourceUsage("IntensiveCpuJob - Start");
        _logger.LogInformation("=== IntensiveCpuJob iniciado ===");

        try
        {
            // 5 milhões de iterações (muito CPU)
            var result = await _processingService.PerformCpuIntensiveCalculationAsync(5_000_000, cancellationToken);

            _logger.LogInformation("IntensiveCpuJob concluído. Resultado: {Result}", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no IntensiveCpuJob");
        }
        finally
        {
            _telemetry.LogResourceUsage("IntensiveCpuJob - End");
        }
    }
}

/// <summary>
/// Job que processa itens em paralelo.
/// Consome: múltiplos cores + RAM proporcional aos itens.
/// </summary>
public class ParallelProcessingJob
{
    private readonly IHeavyProcessingService _processingService;
    private readonly IResourceTelemetryService _telemetry;
    private readonly Microsoft.Extensions.Logging.ILogger<ParallelProcessingJob> _logger;

    public ParallelProcessingJob(
        IHeavyProcessingService processingService,
        IResourceTelemetryService telemetry,
        Microsoft.Extensions.Logging.ILogger<ParallelProcessingJob> logger)
    {
        _processingService = processingService;
        _telemetry = telemetry;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _telemetry.LogResourceUsage("ParallelProcessingJob - Start");
        _logger.LogInformation("=== ParallelProcessingJob iniciado ===");

        try
        {
            // Processa 100 itens em paralelo (100MB + muito CPU)
            var results = await _processingService.ProcessInParallelAsync(100, cancellationToken);

            _logger.LogInformation("ParallelProcessingJob concluído. {Count} itens processados.", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no ParallelProcessingJob");
        }
        finally
        {
            _telemetry.LogResourceUsage("ParallelProcessingJob - End");
        }
    }
}

/// <summary>
/// Job que gera relatórios grandes.
/// Consome: 200-500MB de RAM por execução.
/// </summary>
public class LargeReportGenerationJob
{
    private readonly IHeavyProcessingService _processingService;
    private readonly IResourceTelemetryService _telemetry;
    private readonly Microsoft.Extensions.Logging.ILogger<LargeReportGenerationJob> _logger;

    public LargeReportGenerationJob(
        IHeavyProcessingService processingService,
        IResourceTelemetryService telemetry,
        Microsoft.Extensions.Logging.ILogger<LargeReportGenerationJob> logger)
    {
        _processingService = processingService;
        _telemetry = telemetry;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _telemetry.LogResourceUsage("LargeReportGenerationJob - Start");
        _logger.LogInformation("=== LargeReportGenerationJob iniciado ===");

        try
        {
            // Gera relatório com 100.000 registros
            var report = await _processingService.GenerateLargeReportAsync(100_000, cancellationToken);

            _logger.LogInformation("LargeReportGenerationJob concluído. Tamanho: {SizeMB}MB",
                report.Length / 1024 / 1024);

            // Simula salvamento/envio
            await Task.Delay(2000, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no LargeReportGenerationJob");
        }
        finally
        {
            _telemetry.LogResourceUsage("LargeReportGenerationJob - End");
        }
    }
}

/// <summary>
/// Job que aquece o cache com dados grandes.
/// Consome: RAM do Redis + processamento.
/// </summary>
public class CacheWarmupJob
{
    private readonly ICacheService _cacheService;
    private readonly IResourceTelemetryService _telemetry;
    private readonly Microsoft.Extensions.Logging.ILogger<CacheWarmupJob> _logger;

    public CacheWarmupJob(
        ICacheService cacheService,
        IResourceTelemetryService telemetry,
        Microsoft.Extensions.Logging.ILogger<CacheWarmupJob> logger)
    {
        _cacheService = cacheService;
        _telemetry = telemetry;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _telemetry.LogResourceUsage("CacheWarmupJob - Start");
        _logger.LogInformation("=== CacheWarmupJob iniciado ===");

        try
        {
            // Gera dados grandes para cachear
            var random = new Random();
            for (int i = 0; i < 50; i++)
            {
                var data = new byte[100 * 1024]; // 100KB por item
                random.NextBytes(data);

                var key = $"warmup:data:{i}";
                await _cacheService.SetAsync(key, Convert.ToBase64String(data), TimeSpan.FromMinutes(30));

                if (i % 10 == 0)
                {
                    _logger.LogInformation("Cached {Count}/50 items", i);
                }
            }

            _logger.LogInformation("CacheWarmupJob concluído. 50 itens cacheados (~5MB)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no CacheWarmupJob");
        }
        finally
        {
            _telemetry.LogResourceUsage("CacheWarmupJob - End");
        }
    }
}
