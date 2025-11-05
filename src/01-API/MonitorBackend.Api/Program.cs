using MonitorBackend.Application.Abstractions;
using MonitorBackend.Application.Commands;
using MonitorBackend.Application.Dispatchers;
using MonitorBackend.Application.Queries;
using MonitorBackend.Domain.Repositories;
using MonitorBackend.Infrastructure.Data;
using MonitorBackend.Infrastructure.Repositories;
using MonitorBackend.Infrastructure.Services;
using MonitorBackend.Infrastructure.BackgroundJobs;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using Hangfire;
using Hangfire.PostgreSql;
using AspNetCoreRateLimit;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

// === Configuração do Serilog (Logs Estruturados) ===
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Hangfire", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Usa Serilog
builder.Host.UseSerilog();

// === Configuração de Serviços ===

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Monitor Backend API",
        Version = "v1",
        Description = "API backend com Clean Architecture, CQRS e PostgreSQL (Neon)",
        Contact = new()
        {
            Name = "Monitor Backend",
            Url = new Uri("https://github.com/guilhermebergamo/monitor-backend-api")
        }
    });
});

// Configuração do banco de dados PostgreSQL (Neon)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada.");

// Registra a factory de conexões (Singleton)
builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
    new NpgsqlConnectionFactory(connectionString));

// Registra os repositórios (Scoped)
builder.Services.AddScoped<IRegistroRepository, RegistroRepository>();

// === Redis Cache (Distribuído) ===
var redisConnection = builder.Configuration.GetConnectionString("RedisConnection") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    try
    {
        var configuration = ConfigurationOptions.Parse(redisConnection);
        configuration.AbortOnConnectFail = false; // Não falha se Redis estiver indisponível
        return ConnectionMultiplexer.Connect(configuration);
    }
    catch
    {
        // Se Redis não estiver disponível, retorna uma conexão simulada
        Log.Warning("Redis not available, using in-memory fallback");
        return ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false");
    }
});
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// === Hangfire (Background Jobs) ===
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UsePostgreSqlStorage(options =>
          {
              options.UseNpgsqlConnection(connectionString);
          });
});
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2; // 2 workers paralelos
});

// Registra jobs
builder.Services.AddScoped<DataCleanupJob>();
builder.Services.AddScoped<StatisticsAggregationJob>();
builder.Services.AddScoped<MonthlyReportJob>();
builder.Services.AddScoped<DatabaseHealthCheckJob>();

// === Sistema de Filas (Channels) ===
builder.Services.AddSingleton<EventQueueService>();
builder.Services.AddHostedService<EventProcessorWorker>();

// === Rate Limiting ===
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.RealIpHeader = "X-Real-IP";
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1m",
            Limit = 60 // 60 requisições por minuto
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/registros",
            Period = "1m",
            Limit = 100 // 100 POSTs por minuto
        }
    };
});
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

// === Health Checks ===
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString,
        name: "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql", "postgresql" })
    .AddCheck("memory", () =>
    {
        var allocated = GC.GetTotalMemory(false);
        var threshold = 1024L * 1024 * 1024; // 1GB
        return allocated < threshold
            ? HealthCheckResult.Healthy($"Memory usage: {allocated / 1024 / 1024}MB")
            : HealthCheckResult.Degraded($"Memory usage: {allocated / 1024 / 1024}MB");
    }, tags: new[] { "memory" })
    .AddCheck("disk-space", () =>
    {
        var drive = new DriveInfo(Directory.GetCurrentDirectory());
        var freeSpaceGB = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
        return freeSpaceGB > 1
            ? HealthCheckResult.Healthy($"Free disk space: {freeSpaceGB}GB")
            : HealthCheckResult.Degraded($"Low disk space: {freeSpaceGB}GB");
    }, tags: new[] { "disk" });

// === Application Insights (Telemetria Azure) ===
var appInsightsKey = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrEmpty(appInsightsKey))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = appInsightsKey;
    });
}

// === CQRS: Registra o Dispatcher e os Handlers ===
// Dispatchers
builder.Services.AddScoped<IQueryDispatcher, QueryDispatcher>();
builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();

// Handlers de Queries (um para cada Query)
builder.Services.AddScoped<IQueryHandler<GetRegistrosQuery, GetRegistrosQueryResult>, GetRegistrosQueryHandler>();

// Handlers de Commands (um para cada Command)
builder.Services.AddScoped<ICommandHandler<RegistrarAcessoCommand, RegistrarAcessoCommandResult>, RegistrarAcessoCommandHandler>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:5173";

        // Em desenvolvimento, permite múltiplas origens comuns
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins(
                "http://localhost:5173",  // Vite
                "http://localhost:3000",  // React/Next.js
                "http://localhost:4200",  // Angular
                frontendUrl
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        }
        else
        {
            // Em produção, apenas a URL configurada
            policy.WithOrigins(frontendUrl)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// === Configuração do Pipeline HTTP ===

// Rate Limiting
app.UseIpRateLimiting();

// Swagger em todos os ambientes (disponível tanto em dev quanto prod)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Monitor Backend API v1");
    c.RoutePrefix = "swagger"; // Swagger em /swagger
    c.DocumentTitle = "Monitor Backend API - Documentação";
});

// Hangfire Dashboard (sem autenticação para desenvolvimento)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DisplayStorageConnectionString = false,
    DashboardTitle = "Monitor Backend Jobs"
});

// Configura os jobs recorrentes
RecurringJobs.ConfigureJobs();

app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

// Redirecionar a raiz para o Swagger
app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

// Health Checks com UI formatada
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).WithName("HealthCheck")
  .WithTags("Health");

// Health check detalhado em JSON
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).WithName("ReadinessCheck")
  .WithTags("Health");

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Apenas verifica se a aplicação está rodando
}).WithName("LivenessCheck")
  .WithTags("Health");

// Endpoint de métricas simples
app.MapGet("/metrics", () => new
{
    service = "Monitor Backend API",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    architecture = "Clean Architecture + CQRS + Redis + Hangfire",
    features = new[]
    {
        "PostgreSQL (Neon)",
        "Redis Cache",
        "Hangfire Jobs",
        "Rate Limiting",
        "Serilog Logging",
        "Health Checks",
        "Application Insights"
    },
    memory_mb = GC.GetTotalMemory(false) / 1024 / 1024,
    endpoints = new
    {
        swagger = "/swagger",
        hangfire = "/hangfire",
        health = "/health"
    }
}).WithName("Metrics")
  .WithTags("Monitoring");

try
{
    Log.Information("Starting Monitor Backend API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
