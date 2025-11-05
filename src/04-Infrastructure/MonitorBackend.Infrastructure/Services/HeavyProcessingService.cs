using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace MonitorBackend.Infrastructure.Services;

/// <summary>
/// Serviço de processamento pesado para consumir CPU e memória.
/// Simula operações intensivas em produção.
/// </summary>
public interface IHeavyProcessingService
{
    Task<byte[]> ProcessLargeDataAsync(int sizeMB, CancellationToken cancellationToken = default);
    Task<string> PerformCpuIntensiveCalculationAsync(int iterations, CancellationToken cancellationToken = default);
    Task<List<ComputedResult>> ProcessInParallelAsync(int itemCount, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateLargeReportAsync(int recordCount, CancellationToken cancellationToken = default);
}

public class HeavyProcessingService : IHeavyProcessingService
{
    private readonly ILogger<HeavyProcessingService> _logger;
    private readonly IResourceTelemetryService _telemetry;

    public HeavyProcessingService(
        ILogger<HeavyProcessingService> logger,
        IResourceTelemetryService telemetry)
    {
        _logger = logger;
        _telemetry = telemetry;
    }

    /// <summary>
    /// Processa e aloca uma grande quantidade de dados em memória.
    /// Consome: sizeMB de RAM + processamento de hash.
    /// </summary>
    public async Task<byte[]> ProcessLargeDataAsync(int sizeMB, CancellationToken cancellationToken = default)
    {
        _telemetry.LogResourceUsage($"ProcessLargeData - Start ({sizeMB}MB)");
        _logger.LogInformation("Processando {SizeMB}MB de dados...", sizeMB);

        // Aloca array grande em memória
        var dataSize = sizeMB * 1024 * 1024;
        var data = new byte[dataSize];

        // Preenche com dados "aleatórios" (CPU-bound)
        var random = new Random();
        random.NextBytes(data);

        // Processa os dados (mais CPU)
        using var sha256 = SHA256.Create();
        var hash = await Task.Run(() => sha256.ComputeHash(data), cancellationToken);

        // Simula processamento adicional
        await Task.Delay(100, cancellationToken);

        _telemetry.LogResourceUsage($"ProcessLargeData - End ({sizeMB}MB)");
        _logger.LogInformation("Processamento de {SizeMB}MB concluído. Hash: {Hash}",
            sizeMB, Convert.ToHexString(hash)[..16]);

        return data; // Retorna dados grandes para manter na memória
    }

    /// <summary>
    /// Realiza cálculos matemáticos intensivos para consumir CPU.
    /// Consome: 100% de 1 core durante o processamento.
    /// </summary>
    public async Task<string> PerformCpuIntensiveCalculationAsync(int iterations, CancellationToken cancellationToken = default)
    {
        _telemetry.LogResourceUsage($"CpuIntensiveCalculation - Start ({iterations:N0} iterations)");
        _logger.LogInformation("Iniciando cálculo intensivo com {Iterations:N0} iterações...", iterations);

        var result = await Task.Run(() =>
        {
            double sum = 0;
            for (int i = 0; i < iterations; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Operações matemáticas pesadas
                sum += Math.Sqrt(i) * Math.Sin(i) * Math.Cos(i) * Math.Tan(i / 100.0 + 1);
                sum += Math.Pow(i % 100, 2);
                sum += Math.Log(i + 1) * Math.Exp(i % 10 / 10.0);

                // Hash adicional a cada 1000 iterações para consumir mais CPU
                if (i % 1000 == 0)
                {
                    using var sha = SHA256.Create();
                    var bytes = Encoding.UTF8.GetBytes($"{sum}-{i}");
                    sha.ComputeHash(bytes);
                }
            }
            return sum.ToString("N2");
        }, cancellationToken);

        _telemetry.LogResourceUsage($"CpuIntensiveCalculation - End ({iterations:N0} iterations)");
        _logger.LogInformation("Cálculo concluído. Resultado: {Result}", result);

        return result;
    }

    /// <summary>
    /// Processa múltiplos itens em paralelo para consumir múltiplos cores.
    /// Consome: CPU de múltiplos cores + memória para todos os itens.
    /// </summary>
    public async Task<List<ComputedResult>> ProcessInParallelAsync(int itemCount, CancellationToken cancellationToken = default)
    {
        _telemetry.LogResourceUsage($"ProcessInParallel - Start ({itemCount} items)");
        _logger.LogInformation("Processando {ItemCount} itens em paralelo...", itemCount);

        var tasks = Enumerable.Range(0, itemCount).Select(async i =>
        {
            // Cada task faz processamento pesado
            var data = new byte[1024 * 1024]; // 1MB por item
            new Random(i).NextBytes(data);

            using var sha = SHA256.Create();
            var hash = await Task.Run(() => sha.ComputeHash(data), cancellationToken);

            // Cálculos adicionais
            double computation = 0;
            for (int j = 0; j < 10000; j++)
            {
                computation += Math.Sqrt(i * j + 1) * Math.Sin(j);
            }

            return new ComputedResult
            {
                Id = i,
                Hash = Convert.ToHexString(hash),
                Computation = computation,
                ProcessedAt = DateTime.UtcNow,
                DataSize = data.Length
            };
        });

        var results = await Task.WhenAll(tasks);

        _telemetry.LogResourceUsage($"ProcessInParallel - End ({itemCount} items)");
        _logger.LogInformation("Processamento paralelo concluído. {Count} itens processados.", results.Length);

        return results.ToList();
    }

    /// <summary>
    /// Gera um relatório grande com muitos registros processados.
    /// Consome: muita RAM + CPU para agregações.
    /// </summary>
    public async Task<byte[]> GenerateLargeReportAsync(int recordCount, CancellationToken cancellationToken = default)
    {
        _telemetry.LogResourceUsage($"GenerateLargeReport - Start ({recordCount:N0} records)");
        _logger.LogInformation("Gerando relatório com {RecordCount:N0} registros...", recordCount);

        var reportData = new List<ReportLine>();

        await Task.Run(() =>
        {
            for (int i = 0; i < recordCount; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                reportData.Add(new ReportLine
                {
                    Id = i,
                    Timestamp = DateTime.UtcNow.AddSeconds(-i),
                    Value = Math.Sqrt(i) * 1000,
                    Description = $"Report line {i} with some data for memory consumption: {Guid.NewGuid()}",
                    ComputedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"line-{i}"))),
                    Metadata = new Dictionary<string, string>
                    {
                        ["key1"] = $"value-{i}",
                        ["key2"] = $"computed-{i * 2}",
                        ["key3"] = $"hash-{i % 1000}"
                    }
                });
            }
        }, cancellationToken);

        // Serializa para JSON (mais memória)
        var json = System.Text.Json.JsonSerializer.Serialize(reportData);
        var bytes = Encoding.UTF8.GetBytes(json);

        _telemetry.LogResourceUsage($"GenerateLargeReport - End ({recordCount:N0} records, {bytes.Length / 1024 / 1024}MB)");
        _logger.LogInformation("Relatório gerado: {RecordCount:N0} registros, {SizeMB}MB",
            recordCount, bytes.Length / 1024 / 1024);

        return bytes;
    }
}

public record ComputedResult
{
    public int Id { get; init; }
    public string Hash { get; init; } = string.Empty;
    public double Computation { get; init; }
    public DateTime ProcessedAt { get; init; }
    public int DataSize { get; init; }
}

public record ReportLine
{
    public int Id { get; init; }
    public DateTime Timestamp { get; init; }
    public double Value { get; init; }
    public string Description { get; init; } = string.Empty;
    public string ComputedHash { get; init; } = string.Empty;
    public Dictionary<string, string> Metadata { get; init; } = new();
}
