using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonitorBackend.Infrastructure.Services;

namespace MonitorBackend.Infrastructure.BackgroundJobs;

/// <summary>
/// Background worker que monitora e loga uso de recursos a cada minuto.
/// Logs aparecerão no Azure Container Apps Log Stream.
/// </summary>
public class ResourceMonitorWorker : BackgroundService
{
    private readonly IResourceTelemetryService _telemetryService;
    private readonly ILogger<ResourceMonitorWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public ResourceMonitorWorker(
        IResourceTelemetryService telemetryService,
        ILogger<ResourceMonitorWorker> logger)
    {
        _telemetryService = telemetryService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Resource Monitor Worker iniciado - Logs a cada {Interval}", _interval);

        // Log inicial
        _telemetryService.LogResourceUsage("Startup");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
                _telemetryService.LogResourceUsage("Periodic Check");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Resource Monitor Worker stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao coletar telemetria de recursos");
            }
        }

        _logger.LogInformation("Resource Monitor Worker stopped");
    }
}
