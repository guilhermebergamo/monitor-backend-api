# Arquitetura e Padrões - Monitor Backend

## 📐 Clean Architecture

Este projeto implementa Clean Architecture (também conhecida como Onion Architecture ou Hexagonal Architecture)

### Camadas

```
┌─────────────────────────────────────────────────┐
│                                                 │
│  🎯 API (Presentation Layer)                    │
│  - Controllers                                  │
│  - Middleware                                   │
│  - DTOs de entrada/saída                       │
│                                                 │
├─────────────────────────────────────────────────┤
│                                                 │
│  💼 Application (Use Cases Layer)               │
│  - CQRS Queries/Commands                       │
│  - Handlers                                    │
│  - Validações (FluentValidation)              │
│                                                 │
├─────────────────────────────────────────────────┤
│                                                 │
│  🔧 Infrastructure (External Layer)             │
│  - Implementações de repositórios (Dapper)     │
│  - Acesso a banco de dados                    │
│  - Integrações externas                        │
│                                                 │
└─────────────────────────────────────────────────┘
                       ▲
                       │
                       │ Depende de
                       │
┌─────────────────────────────────────────────────┐
│                                                 │
│  ⚙️ Domain (Core Layer)                         │
│  - Entidades                                   │
│  - Interfaces (contratos)                      │
│  - Regras de negócio                           │
│  - SEM dependências externas                   │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Princípios Aplicados

#### 1. Dependency Rule
As dependências apontam sempre para dentro (para o Domain).
- ✅ Infrastructure → Application → Domain
- ✅ API → Application → Domain
- ❌ Domain → Infrastructure (NUNCA!)

#### 2. Separation of Concerns
Cada camada tem uma responsabilidade específica:
- **Domain**: Regras de negócio puras
- **Application**: Orquestração de casos de uso
- **Infrastructure**: Detalhes de implementação
- **API**: Interface com o mundo externo

## 🔄 CQRS (Command Query Responsibility Segregation)

### Por que CQRS?
- **Separação clara** entre leitura (Queries) e escrita (Commands)
- **Performance otimizada** para cada tipo de operação
- **Escalabilidade** independente
- **Código mais limpo** e testável

### Implementação com QueryDispatcher (sem MediatR)

```csharp
// 1. Query Marker Interface
public interface IQuery<TResult> { }

// 2. Query (Request) - Record type
public record GetRegistrosQuery() : IQuery<GetRegistrosQueryResult>;

// 3. Query Result (Response)
public record GetRegistrosQueryResult
{
    public bool Success { get; init; }
    public IEnumerable<Registro> Registros { get; init; } = [];
}

// 4. Handler Interface
public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken = default);
}

// 5. Handler Implementation
public class GetRegistrosQueryHandler : IQueryHandler<GetRegistrosQuery, GetRegistrosQueryResult>
{
    private readonly IRegistroRepository _repository;
    
    public async Task<GetRegistrosQueryResult> Handle(
        GetRegistrosQuery query, 
        CancellationToken cancellationToken = default)
    {
        var registros = await _repository.GetAllAsync(cancellationToken);
        return GetRegistrosQueryResult.CreateSuccess(registros);
    }
}

// 6. Dispatcher Interface
public interface IQueryDispatcher
{
    Task<TResult> Dispatch<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}

// 7. Dispatcher Implementation (using IServiceProvider reflection)
public class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    
    public async Task<TResult> Dispatch<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        var queryType = query.GetType();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));
        var handler = _serviceProvider.GetService(handlerType);
        
        var method = handlerType.GetMethod("Handle");
        return await (Task<TResult>)method!.Invoke(handler, new object[] { query, cancellationToken })!;
    }
}

// 8. Controller (Entry Point) - Uses Dispatcher only
[ApiController]
public class RegistrosController : ControllerBase
{
    private readonly IQueryDispatcher _dispatcher;
    
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Dispatch(new GetRegistrosQuery(), cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

// 9. DI Registration (Program.cs)
services.AddScoped<IQueryDispatcher, QueryDispatcher>();
services.AddScoped<IQueryHandler<GetRegistrosQuery, GetRegistrosQueryResult>, GetRegistrosQueryHandler>();
```

### Por que não usar MediatR?

**Vantagens do QueryDispatcher customizado:**
1. ✅ **Controle total** - Implementação própria, sem biblioteca externa
2. ✅ **Aprendizado** - Entende-se profundamente como funciona o dispatcher pattern
3. ✅ **Flexibilidade** - Fácil customizar para necessidades específicas
4. ✅ **Menos dependências** - Sem pacotes externos além do .NET base
5. ✅ **Simplicidade** - Código direto usando IServiceProvider e reflection

**Vantagens sobre o padrão Service Layer do DDD:**
1. ✅ **Separação clara** - Queries são objetos, não métodos de serviço
2. ✅ **Testabilidade** - Handlers isolados e independentes
3. ✅ **Single Responsibility** - Cada handler tem uma única responsabilidade
4. ✅ **Escalabilidade** - Fácil adicionar novos handlers sem modificar existentes
5. ✅ **Dispatch dinâmico** - Controller não precisa conhecer handlers específicos

**Quando usar MediatR:**
- Projetos muito grandes com centenas de handlers
- Time grande que já conhece a biblioteca
- Necessidade de pipelines complexos (behaviors)
- Quando não quiser manter o código do dispatcher

## 🎯 SOLID Principles

### S - Single Responsibility Principle
Cada classe tem uma única responsabilidade:
```csharp
// ✅ BOM
public class RegistroRepository : IRegistroRepository
{
    // Responsabilidade: Acesso a dados de Registro
}

// ❌ RUIM
public class RegistroRepositoryAndEmailService
{
    // Múltiplas responsabilidades: dados + email
}
```

### O - Open/Closed Principle
Aberto para extensão, fechado para modificação:
```csharp
// ✅ Interface permite extensão sem modificar código existente
public interface IRegistroRepository
{
    Task<IEnumerable<Registro>> GetAllAsync();
}

// Nova implementação sem modificar a original
public class CachedRegistroRepository : IRegistroRepository
{
    // Implementação com cache
}
```

### L - Liskov Substitution Principle
Subtipos devem ser substituíveis por seus tipos base:
```csharp
IRegistroRepository repo = new RegistroRepository();
IRegistroRepository cachedRepo = new CachedRegistroRepository();
// Ambos funcionam da mesma forma para o cliente
```

### I - Interface Segregation Principle
Interfaces específicas ao invés de genéricas:
```csharp
// ✅ BOM - Interfaces específicas
public interface IRegistroRepository { }
public interface ILogRepository { }

// ❌ RUIM - Interface genérica demais
public interface IRepository { }
```

### D - Dependency Inversion Principle
Depender de abstrações, não de implementações:
```csharp
// ✅ BOM - Depende da interface
public class GetRegistrosQueryHandler
{
    private readonly IRegistroRepository _repository;
}

// ❌ RUIM - Depende da implementação concreta
public class GetRegistrosQueryHandler
{
    private readonly RegistroRepository _repository;
}
```

## 🧪 Testabilidade

### Por que este design é testável?

1. **Dependency Injection**: Fácil mockar dependências
2. **Interfaces**: Permite criar mocks
3. **Separação de responsabilidades**: Testes isolados
4. **CQRS**: Handlers são funções puras

### Exemplo de Teste Unitário

```csharp
[Fact]
public async Task GetRegistrosHandler_DeveRetornarRegistros()
{
    // Arrange
    var mockRepo = new Mock<IRegistroRepository>();
    mockRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Registro> { /* dados */ });
    
    var handler = new GetRegistrosQueryHandler(mockRepo.Object);
    var query = new GetRegistrosQuery();
    
    // Act
    var result = await handler.HandleAsync(query);
    
    // Assert
    Assert.True(result.Success);
    Assert.NotEmpty(result.Registros);
}
```

## 🗄️ Dapper vs Entity Framework

### Por que Dapper?

**Vantagens:**
1. ✅ **Performance** - Muito mais rápido que EF Core
2. ✅ **Controle total** - SQL explícito
3. ✅ **Simplicidade** - Menos mágica
4. ✅ **Leveza** - Biblioteca pequena
5. ✅ **Custos Azure** - Menos CPU = menos custo

**Quando usar EF Core:**
- Projetos com mudanças frequentes no schema
- Times sem expertise em SQL
- Necessidade de migrations automáticas

### Performance Comparison
```
Operação: SELECT 1000 registros
EF Core: ~45ms
Dapper:  ~15ms (3x mais rápido)
```

## 🏭 Factory Pattern

```csharp
// ConnectionFactory encapsula criação de conexões
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

// Benefícios:
// 1. Centralização da lógica de conexão
// 2. Fácil substituição em testes
// 3. Reutilização da connection string
```

## 🔄 Worker Service Pattern

### Por que usar Background Service?

1. **Tarefas periódicas** - Health checks, backups, etc.
2. **Processamento assíncrono** - Filas, batch jobs
3. **Monitoramento** - Manter serviços ativos

### Implementação
```csharp
public class FrontendHealthCheckWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DoWorkAsync();
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

## 📊 Performance Considerations

### 1. Connection Pooling
```csharp
// Dapper usa connection pooling do PostgreSQL automaticamente
using var connection = _connectionFactory.CreateConnection();
// Conexão é retornada ao pool quando disposed
```

### 2. Async/Await
```csharp
// ✅ Sempre use async para I/O
public async Task<IEnumerable<Registro>> GetAllAsync()
{
    return await connection.QueryAsync<Registro>(sql);
}
```

### 3. Scoped Dependencies
```csharp
// Repository é Scoped = uma instância por requisição HTTP
builder.Services.AddScoped<IRegistroRepository, RegistroRepository>();
```

## 🔒 Segurança

### 1. SQL Injection Protection
```csharp
// ✅ BOM - Dapper usa parâmetros
var registro = await connection.QueryFirstOrDefaultAsync<Registro>(
    "SELECT * FROM registros WHERE id = @Id", 
    new { Id = id });

// ❌ RUIM - String interpolation vulnerável
var sql = $"SELECT * FROM registros WHERE id = {id}";
```

### 2. Connection String Security
```csharp
// ✅ Usar User Secrets localmente
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=..."

// ✅ Usar Azure Key Vault em produção
builder.Configuration.AddAzureKeyVault(...);
```

## 📈 Escalabilidade

### Horizontal Scaling
```bash
# Container Apps escala automaticamente
az containerapp update \
  --min-replicas 1 \
  --max-replicas 10
```

### Database Scaling (Neon)
- **Read Replicas** para queries pesadas
- **Connection Pooling** para múltiplas instâncias
- **Autoscale** do Neon para performance

## 🎓 Recursos para Estudo

### Livros
- Clean Architecture (Robert C. Martin)
- Domain-Driven Design (Eric Evans)
- Implementing Domain-Driven Design (Vaughn Vernon)

### Cursos
- Microsoft Learn: ASP.NET Core
- Pluralsight: CQRS in Practice
- Clean Architecture Course (Jason Taylor)

### Repositórios Exemplo
- [CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture)
- [eShopOnContainers](https://github.com/dotnet-architecture/eShopOnContainers)

## 🚀 Próximos Passos (Evolução do Projeto)

1. **Commands CQRS** - POST, PUT, DELETE
2. **Event Sourcing** - Histórico de mudanças
3. **Redis Cache** - Cache distribuído
4. **RabbitMQ/Service Bus** - Mensageria
5. **Health Checks** - Monitoramento de saúde
6. **Application Insights** - Telemetria
7. **Unit Tests** - Cobertura de testes
8. **Integration Tests** - Testes E2E
9. **API Versioning** - Versionamento de API
10. **Rate Limiting** - Proteção contra abuso

---

Este documento é um guia vivo e deve ser atualizado conforme o projeto evolui.
