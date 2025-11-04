using MonitorBackend.Application.Abstractions;
using MonitorBackend.Application.Commands;
using MonitorBackend.Application.Dispatchers;
using MonitorBackend.Application.Queries;
using MonitorBackend.Domain.Repositories;
using MonitorBackend.Infrastructure.Data;
using MonitorBackend.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

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

// Swagger em todos os ambientes (disponível tanto em dev quanto prod)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Monitor Backend API v1");
    c.RoutePrefix = "swagger"; // Swagger em /swagger
    c.DocumentTitle = "Monitor Backend API - Documentação";
});

app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

// Redirecionar a raiz para o Swagger
app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

// Health check em /health
app.MapGet("/health", () => new
{
    service = "Monitor Backend API",
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    architecture = "Clean Architecture + CQRS",
    database = "PostgreSQL (Neon)",
    swagger = "/swagger"
}).WithName("HealthCheck")
  .WithTags("Health");

app.Run();
