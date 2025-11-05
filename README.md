# Monitor Backend - Clean Architecture com .NET 8

🏗️ Projeto backend completo implementando **Clean Architecture**, **CQRS manual** (sem MediatR), **Dapper** e **PostgreSQL (Neon)**.

## 📦 Estrutura do Projeto

```
monitor-backend/
├── src/
│   ├── 01-API/
│   │   └── MonitorBackend.Api/         # API REST
│   │       ├── Controllers/            # Endpoints REST
│   │       └── appsettings.json       # Configurações
│   ├── 02-Application/
│   │   └── MonitorBackend.Application/ # Camada de Aplicação
│   │       ├── Abstractions/           # IQuery, IQueryHandler, IQueryDispatcher
│   │       ├── Dispatchers/            # QueryDispatcher
│   │       └── Queries/                # Queries CQRS com Handlers
│   │           └── Validators/         # Validadores FluentValidation
│   ├── 03-Domain/
│   │   └── MonitorBackend.Domain/      # Camada de Domínio
│   │       ├── Entities/               # Entidades do negócio
│   │       └── Repositories/           # Contratos de repositórios
│   ├── 04-Infrastructure/
│   │   └── MonitorBackend.Infrastructure/  # Camada de Infraestrutura
│   │       ├── Data/                   # Configuração de banco
│   │       └── Repositories/           # Implementação com Dapper
├── Dockerfile                          # Multi-stage build
├── Dockerfile.api                      # Build otimizado API
└── README.md
```

## 🚀 Tecnologias Utilizadas

- **.NET 8** - Framework principal
- **Clean Architecture** - Separação em camadas (Domain, Application, Infrastructure, API)
- **CQRS Manual** - Queries e Commands sem MediatR
- **Dapper** - Micro-ORM para PostgreSQL
- **FluentValidation** - Validações de entrada
- **PostgreSQL (Neon)** - Banco de dados online

- **Swagger/OpenAPI** - Documentação da API
- **Docker** - Containerização para Azure
- **Redis Cache** - Cache distribuído (Azure Cache for Redis)
- **Hangfire** - Background jobs com PostgreSQL storage
- **Serilog** - Logs estruturados
- **Application Insights** - Telemetria Azure
- **Rate Limiting** - Proteção contra sobrecarga
- **Health Checks** - Monitoramento de saúde
- **Resource Telemetry** - Métricas de memória/CPU/threads

## 🔥 Sistema de Processamento Pesado

Projeto inclui **sistema extremamente pesado** para simular aplicação de produção de grande porte:
- ⚡ **Background jobs a cada 2-15 minutos** (processamento massivo)
- 💾 **Consumo de 800MB-1.5GB de RAM** (vs 300-500MB sem carga)
- 🖥️ **60-90% CPU contínuo** (vs 10-15% idle)
- 🔄 **Processamento paralelo multi-core**
- 📊 **Endpoints para stress test e benchmarks**

**📖 Documentação completa**: [HEAVY-SYSTEM.md](HEAVY-SYSTEM.md)

## 🎯 Funcionalidades

### API REST
- **GET /api/registros** - Lista todos os registros do banco
  - Retorna: `Observacao`, `DataHora`, `Quantidade`
  - Implementado com CQRS (Query + Handler)
- **GET /** - Health check simples
- **GET /api/registros/health** - Health check do controller



## ⚙️ Configuração

### 1. Configurar o Banco de Dados

Edite `src/01-API/MonitorBackend.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=SEU_HOST.neon.tech;Database=SEU_DB;Username=SEU_USER;Password=SUA_SENHA;SSL Mode=Require;Trust Server Certificate=true"
  },
  "FrontendUrl": "https://seu-frontend.azurestaticapps.net"
}
```

### 2. Criar a Tabela no PostgreSQL (Neon)

Execute este SQL no seu banco Neon:

```sql
CREATE TABLE registros (
    id SERIAL PRIMARY KEY,
    observacao VARCHAR(500) NOT NULL,
    data_hora TIMESTAMP NOT NULL DEFAULT NOW(),
    quantidade INTEGER NOT NULL DEFAULT 0
);

-- Inserir dados de exemplo
INSERT INTO registros (observacao, data_hora, quantidade) VALUES
    ('Sistema iniciado com sucesso', NOW(), 1),
    ('Processamento concluído', NOW() - INTERVAL '1 hour', 150),
    ('Backup realizado', NOW() - INTERVAL '2 hours', 1),
    ('Atualização de cache', NOW() - INTERVAL '3 hours', 2500);
```

## 🏃‍♂️ Como Executar Localmente

### Pré-requisitos
- .NET 8 SDK
- PostgreSQL (Neon online) configurado

### Executar a API

```bash
cd src/01-API/MonitorBackend.Api
dotnet restore
dotnet run
```

Acesse: `http://localhost:5000` (Swagger na raiz)



## 🐳 Build com Docker

### Opção 1: Build separado (recomendado para Azure Container Apps)

#### API
```bash
docker build -f Dockerfile.api -t monitor-api:latest .
docker run -p 8080:8080 -e ConnectionStrings__DefaultConnection="Host=..." monitor-api:latest
```



### Opção 2: Build multi-stage

```bash
# API
docker build --target api-runtime -t monitor-api:latest .


```

## ☁️ Deploy no Azure Container Apps

### 1. Criar Container Registry

```bash
az acr create --resource-group meu-rg --name meuregistry --sku Basic
az acr login --name meuregistry
```

### 2. Push das Imagens

```bash
# Tag e push API
docker tag monitor-api:latest meuregistry.azurecr.io/monitor-api:latest
docker push meuregistry.azurecr.io/monitor-api:latest


```

### 3. Criar Container Apps

#### API
```bash
az containerapp create \
  --name monitor-api \
  --resource-group meu-rg \
  --environment meu-env \
  --image meuregistry.azurecr.io/monitor-api:latest \
  --target-port 8080 \
  --ingress external \
  --registry-server meuregistry.azurecr.io \
  --env-vars \
    ConnectionStrings__DefaultConnection="Host=..." \
    FrontendUrl="https://seu-frontend.azurestaticapps.net"
```



### 4. Configurar Variáveis de Ambiente no Portal Azure

No portal do Azure Container Apps, adicione as variáveis sensíveis como **secrets**:
- `ConnectionStrings__DefaultConnection`


## 📊 Custos Estimados no Azure

### Container Apps (Consumption Plan)
- **API**: ~$0.000024/vCPU-second + $0.000004/GiB-second
  - Estimativa: **$5-15/mês** (tráfego baixo)


### Neon PostgreSQL
- **Gratuito** até 0.5 GB
- **Autoscale**: $19/mês (databases maiores)

### Total Estimado: **$7-20/mês** (cenário básico)

## 🧪 Testando a API

### Com cURL
```bash
curl http://localhost:5000/api/registros
```

### Com PowerShell
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/registros" -Method Get
```

### Swagger UI
Acesse: `http://localhost:5000`

## 🧩 Arquitetura CQRS (sem MediatR)

### Query Flow
```
Controller → QueryHandler → Repository (Dapper) → PostgreSQL
```

### Exemplo de Query
```csharp
// 1. Query (request)
public class GetRegistrosQuery { }

// 2. Handler (processor)
public class GetRegistrosQueryHandler : IQueryHandler<GetRegistrosQuery, GetRegistrosQueryResult>
{
    public async Task<GetRegistrosQueryResult> HandleAsync(GetRegistrosQuery query)
    {
        var registros = await _repository.GetAllAsync();
        return new GetRegistrosQueryResult { Registros = registros };
    }
}

// 3. Controller (endpoint)
[HttpGet]
public async Task<IActionResult> GetAll()
{
    var query = new GetRegistrosQuery();
    var result = await _handler.HandleAsync(query);
    return Ok(result);
}
```

## 🔧 Princípios SOLID Aplicados

- **S** - Single Responsibility: Cada classe tem uma responsabilidade única
- **O** - Open/Closed: Aberto para extensão, fechado para modificação
- **L** - Liskov Substitution: Interfaces bem definidas
- **I** - Interface Segregation: Interfaces específicas (IRegistroRepository)
- **D** - Dependency Inversion: Dependência de abstrações (interfaces)

## 📝 Logs

### API
```
[INF] Recebendo requisição GET /api/registros
[INF] Retornando 4 registros
```



## 🛠️ Próximos Passos (Sugestões)

1. **Commands CQRS** - Adicionar operações de escrita (POST, PUT, DELETE)
2. **Cache** - Implementar Redis para cache de queries
3. **Autenticação** - Adicionar JWT/Azure AD
4. **Métricas** - Application Insights para monitoramento
5. **Testes** - Unit tests e integration tests
6. **CI/CD** - GitHub Actions para deploy automático

## 📄 Licença

Este projeto é open-source e está disponível para estudo e uso comercial.

## 👨‍💻 Autor

Desenvolvido como exemplo de Clean Architecture com .NET 8 para estudos de custos e performance no Azure.

---

**Nota**: Lembre-se de nunca commitar credenciais reais no código. Use **Azure Key Vault** ou **User Secrets** para desenvolvimento local.
