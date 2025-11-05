using Hangfire;
using MonitorBackend.Domain.Repositories;

namespace MonitorBackend.Infrastructure.BackgroundJobs;

/// <summary>
/// Jobs recorrentes para processamento em background.
/// Consome memória: ~150-200MB + CPU contínuo.
/// </summary>
public static class RecurringJobs
{
    public static void ConfigureJobs()
    {
        // Job 1: Limpeza de registros antigos (executa diariamente às 2h da manhã)
        RecurringJob.AddOrUpdate<DataCleanupJob>(
            "cleanup-old-records",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Daily(2));

        // Job 2: Agregação de estatísticas (executa a cada hora)
        RecurringJob.AddOrUpdate<StatisticsAggregationJob>(
            "aggregate-statistics",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Hourly());

        // Job 3: Geração de relatório mensal (primeiro dia do mês)
        RecurringJob.AddOrUpdate<MonthlyReportJob>(
            "monthly-report",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Monthly(1, 3));

        // Job 4: Health check de banco de dados (a cada 5 minutos)
        RecurringJob.AddOrUpdate<DatabaseHealthCheckJob>(
            "database-health-check",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/5 * * * *");
    }
}

/// <summary>
/// Job de limpeza de dados antigos
/// </summary>
public class DataCleanupJob
{
    private readonly IRegistroRepository _repository;

    public DataCleanupJob(IRegistroRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Simula processamento pesado
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

        // Lógica de limpeza seria implementada aqui
        // Por enquanto apenas simula o processamento
    }
}

/// <summary>
/// Job de agregação de estatísticas
/// </summary>
public class StatisticsAggregationJob
{
    private readonly IRegistroRepository _repository;

    public StatisticsAggregationJob(IRegistroRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Simula agregação de estatísticas
        var registros = await _repository.GetAllAsync();

        // Processa dados em memória (consome RAM)
        var statistics = registros
            .GroupBy(r => r.DataHora.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count(),
                TotalQuantidade = g.Sum(r => r.Quantidade)
            })
            .ToList();

        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
}

/// <summary>
/// Job de relatório mensal
/// </summary>
public class MonthlyReportJob
{
    private readonly IRegistroRepository _repository;

    public MonthlyReportJob(IRegistroRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Simula geração de relatório pesado
        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
    }
}

/// <summary>
/// Job de health check do banco
/// </summary>
public class DatabaseHealthCheckJob
{
    private readonly IRegistroRepository _repository;

    public DatabaseHealthCheckJob(IRegistroRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Verifica se consegue buscar dados
            await _repository.GetUltimoRegistroAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Log de erro seria registrado aqui
            Console.WriteLine($"Database health check failed: {ex.Message}");
        }
    }
}
