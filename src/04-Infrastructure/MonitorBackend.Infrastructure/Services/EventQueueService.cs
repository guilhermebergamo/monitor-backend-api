using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MonitorBackend.Infrastructure.Services;

/// <summary>
/// Sistema de filas assíncronas usando Channels.
/// Consome memória: ~100-150MB para buffer de mensagens.
/// </summary>
public interface IEventQueueService
{
    Task EnqueueAsync<T>(T @event, CancellationToken cancellationToken = default);
}

public sealed class EventQueueService : IEventQueueService
{
    private readonly Channel<object> _channel;

    public EventQueueService()
    {
        // Canal com capacidade limitada para controle de memória
        _channel = Channel.CreateBounded<object>(new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public async Task EnqueueAsync<T>(T @event, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(@event!, cancellationToken);
    }

    public ChannelReader<object> Reader => _channel.Reader;
}

/// <summary>
/// Worker que processa eventos da fila em background
/// </summary>
public class EventProcessorWorker : BackgroundService
{
    private readonly EventQueueService _queueService;
    private readonly ILogger<EventProcessorWorker> _logger;

    public EventProcessorWorker(EventQueueService queueService, ILogger<EventProcessorWorker> logger)
    {
        _queueService = queueService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Event Processor Worker started");

        await foreach (var @event in _queueService.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                // Processa o evento (simula processamento pesado)
                await ProcessEventAsync(@event, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event");
            }
        }
    }

    private async Task ProcessEventAsync(object @event, CancellationToken cancellationToken)
    {
        // Simula processamento que consome CPU e memória
        await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);

        // Aqui você processaria o evento real
        _logger.LogDebug("Event processed: {EventType}", @event.GetType().Name);
    }
}
