using Microsoft.AspNetCore.Http;
using MonitorBackend.Infrastructure.Services;

namespace MonitorBackend.Api.Middleware;

/// <summary>
/// Middleware que loga uso de recursos antes e depois de cada requisição.
/// Mostra quanto de memória foi usada durante o processamento.
/// </summary>
public class ResourceTelemetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResourceTelemetryMiddleware> _logger;

    public ResourceTelemetryMiddleware(
        RequestDelegate next,
        ILogger<ResourceTelemetryMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IResourceTelemetryService telemetryService)
    {
        var path = context.Request.Path.Value ?? "unknown";
        var method = context.Request.Method;

        // Ignora endpoints de health e hangfire para não poluir logs
        if (path.StartsWith("/health") || path.StartsWith("/hangfire"))
        {
            await _next(context);
            return;
        }

        // Captura métricas ANTES da requisição
        var metricsBefore = telemetryService.GetCurrentMetrics();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Captura métricas DEPOIS da requisição
            var metricsAfter = telemetryService.GetCurrentMetrics();

            // Calcula diferença de memória
            var memoryDeltaMB = metricsAfter.TotalMemoryMB - metricsBefore.TotalMemoryMB;
            var workingSetDeltaMB = metricsAfter.WorkingSetMB - metricsBefore.WorkingSetMB;

            _logger.LogInformation(
                "Request [{Method}] {Path} | " +
                "Duration: {DurationMs}ms | " +
                "Status: {StatusCode} | " +
                "Memory Before: {MemoryBeforeMB:F2}MB | " +
                "Memory After: {MemoryAfterMB:F2}MB | " +
                "Memory Delta: {MemoryDeltaMB:F2}MB | " +
                "Working Set Delta: {WorkingSetDeltaMB:F2}MB | " +
                "GC Gen0: +{Gen0Delta}, Gen1: +{Gen1Delta}, Gen2: +{Gen2Delta}",
                method,
                path,
                stopwatch.ElapsedMilliseconds,
                context.Response.StatusCode,
                metricsBefore.TotalMemoryMB,
                metricsAfter.TotalMemoryMB,
                memoryDeltaMB,
                workingSetDeltaMB,
                metricsAfter.Gen0Collections - metricsBefore.Gen0Collections,
                metricsAfter.Gen1Collections - metricsBefore.Gen1Collections,
                metricsAfter.Gen2Collections - metricsBefore.Gen2Collections
            );
        }
    }
}
