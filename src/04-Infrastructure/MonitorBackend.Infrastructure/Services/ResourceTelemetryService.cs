using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MonitorBackend.Infrastructure.Services;

/// <summary>
/// Serviço para coletar e logar telemetria de recursos (memória, CPU, GC).
/// Logs estruturados para visualização no Azure.
/// </summary>
public interface IResourceTelemetryService
{
    void LogResourceUsage(string context);
    ResourceMetrics GetCurrentMetrics();
}

public class ResourceTelemetryService : IResourceTelemetryService
{
    private readonly ILogger<ResourceTelemetryService> _logger;
    private readonly Process _currentProcess;

    public ResourceTelemetryService(ILogger<ResourceTelemetryService> logger)
    {
        _logger = logger;
        _currentProcess = Process.GetCurrentProcess();
    }

    public void LogResourceUsage(string context)
    {
        var metrics = GetCurrentMetrics();

        _logger.LogInformation(
            "Resource Usage [{Context}] - " +
            "Memory: {MemoryMB}MB (Working: {WorkingSetMB}MB, GC: {GcMemoryMB}MB) | " +
            "CPU: {CpuTime}s | " +
            "Threads: {ThreadCount} | " +
            "GC Gen0: {Gen0}, Gen1: {Gen1}, Gen2: {Gen2}",
            context,
            metrics.TotalMemoryMB,
            metrics.WorkingSetMB,
            metrics.GcMemoryMB,
            metrics.TotalProcessorTimeSeconds,
            metrics.ThreadCount,
            metrics.Gen0Collections,
            metrics.Gen1Collections,
            metrics.Gen2Collections
        );
    }

    public ResourceMetrics GetCurrentMetrics()
    {
        _currentProcess.Refresh();

        return new ResourceMetrics
        {
            // Memória alocada pelo GC
            GcMemoryBytes = GC.GetTotalMemory(false),
            GcMemoryMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0,

            // Working Set (memória física usada)
            WorkingSetBytes = _currentProcess.WorkingSet64,
            WorkingSetMB = _currentProcess.WorkingSet64 / 1024.0 / 1024.0,

            // Private Memory (memória total alocada)
            PrivateMemoryBytes = _currentProcess.PrivateMemorySize64,
            PrivateMemoryMB = _currentProcess.PrivateMemorySize64 / 1024.0 / 1024.0,

            // Total (maior entre GC e Private)
            TotalMemoryMB = Math.Max(
                GC.GetTotalMemory(false) / 1024.0 / 1024.0,
                _currentProcess.PrivateMemorySize64 / 1024.0 / 1024.0
            ),

            // CPU
            TotalProcessorTime = _currentProcess.TotalProcessorTime,
            TotalProcessorTimeSeconds = _currentProcess.TotalProcessorTime.TotalSeconds,
            UserProcessorTimeSeconds = _currentProcess.UserProcessorTime.TotalSeconds,

            // Threads
            ThreadCount = _currentProcess.Threads.Count,

            // Garbage Collector
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),

            // Timestamp
            Timestamp = DateTime.UtcNow
        };
    }
}

public record ResourceMetrics
{
    public long GcMemoryBytes { get; init; }
    public double GcMemoryMB { get; init; }
    public long WorkingSetBytes { get; init; }
    public double WorkingSetMB { get; init; }
    public long PrivateMemoryBytes { get; init; }
    public double PrivateMemoryMB { get; init; }
    public double TotalMemoryMB { get; init; }
    public TimeSpan TotalProcessorTime { get; init; }
    public double TotalProcessorTimeSeconds { get; init; }
    public double UserProcessorTimeSeconds { get; init; }
    public int ThreadCount { get; init; }
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
    public DateTime Timestamp { get; init; }
}
