# Sistema de Consumo de Recursos - Implementação Completa

## 📊 Resumo Executivo

Este documento descreve a implementação de uma stack completa de recursos para simular uma aplicação de médio porte, permitindo medição real de consumo e custos no Azure Container Apps.

## 🎯 Objetivos

- Simular carga realista de CPU e memória
- Permitir medição precisa de custos
- Implementar features de produção real
- Manter arquitetura limpa e escalável

## 🚀 Features Implementadas

### 1. **Redis Cache Distribuído** ✅
- **Pacote**: StackExchange.Redis 2.9.32
- **Consumo Estimado**: 200-300MB RAM
- **Localização**: `MonitorBackend.Infrastructure.Services.RedisCacheService`
- **Configuração**: appsettings.json → `ConnectionStrings:RedisConnection`
- **Uso**: 
  - Cache de queries frequentes
  - Serialização/deserialização JSON
  - TTL configurável por chave
  - Fallback automático se Redis indisponível

### 2. **Hangfire Background Jobs** ✅
- **Pacotes**: 
  - Hangfire.AspNetCore 1.8.21
  - Hangfire.PostgreSql 1.20.12
- **Consumo Estimado**: 150-200MB RAM + CPU contínuo
- **Localização**: `MonitorBackend.Infrastructure.BackgroundJobs.RecurringJobs`
- **Dashboard**: `/hangfire` (sem autenticação em dev)
- **Jobs Configurados**:
  1. **Data Cleanup Job** - Diário às 2h (simula limpeza)
  2. **Statistics Aggregation** - Horário (processa agregações)
  3. **Monthly Report** - Mensal dia 1 (relatórios pesados)
  4. **Database Health Check** - A cada 5min (monitora DB)

### 3. **Serilog Logs Estruturados** ✅
- **Pacotes**:
  - Serilog.AspNetCore 9.0.0
  - Serilog.Sinks.Console 6.1.1
  - Serilog.Enrichers.Environment 3.0.1
- **Consumo Estimado**: 50-100MB RAM
- **Features**:
  - Logs em JSON estruturado
  - Enrichers de máquina e ambiente
  - Buffer em memória
  - Console formatado para desenvolvimento

### 4. **Health Checks Avançados** ✅
- **Pacotes**:
  - AspNetCore.HealthChecks.Npgsql 9.0.0
  - AspNetCore.HealthChecks.UI.Client 9.0.0
- **Consumo Estimado**: Mínimo (verificações periódicas)
- **Endpoints**:
  - `/health` - Health check completo com UI formatada
  - `/health/ready` - Readiness probe (DB)
  - `/health/live` - Liveness probe (app rodando)
- **Verificações**:
  - PostgreSQL connectivity
  - Memória alocada (threshold 1GB)
  - Espaço em disco (threshold 1GB)

### 5. **Rate Limiting** ✅
- **Pacote**: AspNetCoreRateLimit 5.0.0
- **Consumo Estimado**: 30-50MB RAM
- **Configuração**:
  - 60 requisições/minuto geral
  - 100 POST /api/registros por minuto
  - Por IP
  - HTTP 429 quando excede

### 6. **Sistema de Filas Assíncronas** ✅
- **Tecnologia**: System.Threading.Channels (nativo .NET)
- **Consumo Estimado**: 100-150MB RAM
- **Localização**: `MonitorBackend.Infrastructure.Services.EventQueueService`
- **Features**:
  - Canal limitado (10.000 eventos)
  - Worker background processando continuamente
  - Processamento paralelo assíncrono

### 7. **Application Insights** ✅
- **Pacote**: Microsoft.ApplicationInsights.AspNetCore 2.23.0
- **Consumo Estimado**: 50MB RAM
- **Configuração**: appsettings.json → `ApplicationInsights:ConnectionString`
- **Features**:
  - Telemetria automática
  - Rastreamento de requisições
  - Métricas customizadas
  - Integração nativa com Azure

### 8. **Endpoint de Métricas** ✅
- **Endpoint**: `/metrics`
- **Informações**:
  - Uso de memória em tempo real
  - Lista de features ativas
  - Timestamp e ambiente
  - Links para dashboards

## 📦 Novos Arquivos Criados

```
src/04-Infrastructure/MonitorBackend.Infrastructure/
├── Services/
│   ├── RedisCacheService.cs          # Cache distribuído
│   └── EventQueueService.cs          # Filas assíncronas
└── BackgroundJobs/
    └── RecurringJobs.cs               # Jobs do Hangfire
```

## 🔧 Arquivos Modificados

### Program.cs
- Configuração completa do Serilog
- Registro de serviços Redis, Hangfire, Rate Limiting
- Health Checks com múltiplos probes
- Application Insights
- Dashboard do Hangfire
- Novos endpoints de métricas

### appsettings.json / appsettings.Development.json
- Adicionado `ConnectionStrings:RedisConnection`
- Adicionado `ApplicationInsights:ConnectionString`
- Ajustado níveis de log para Hangfire

### docker-compose.yml
- Adicionado serviço Redis
- Configurado health check do Redis
- Aumentado recursos da API:
  - CPU: 0.5-1.0 cores
  - RAM: 1-2GB
- Dependência do Redis

### MonitorBackend.Api.csproj
Novos pacotes:
- StackExchange.Redis
- Hangfire.AspNetCore
- Hangfire.PostgreSql
- Serilog.AspNetCore + Sinks
- AspNetCoreRateLimit
- AspNetCore.HealthChecks.Npgsql
- AspNetCore.HealthChecks.UI.Client
- Microsoft.ApplicationInsights.AspNetCore

### MonitorBackend.Infrastructure.csproj
Novos pacotes:
- StackExchange.Redis
- Hangfire.Core
- Microsoft.Extensions.Hosting.Abstractions

## 📊 Estimativa de Consumo Final

### Memória
| Componente | Idle | Sob Carga |
|------------|------|-----------|
| API Base .NET | 150MB | 200MB |
| Redis Cache | 50MB | 300MB |
| Hangfire | 100MB | 200MB |
| Serilog Buffer | 30MB | 100MB |
| Health Checks | 10MB | 20MB |
| Rate Limiting | 20MB | 50MB |
| Event Queues | 50MB | 150MB |
| Application Insights | 30MB | 50MB |
| **TOTAL** | **~500MB** | **~1.2GB** |

### CPU
- **Idle**: 10-15%
- **Carga Média**: 30-50%
- **Picos**: 60-80%

## 💰 Estimativa de Custo (Azure Container Apps)

### Configuração Recomendada
- **CPU**: 1.0 vCPU
- **RAM**: 2GB
- **Réplicas**: 2-3 (alta disponibilidade)

### Custos Estimados (Central US)
- **1.0 vCPU + 2GB** = ~$0.000012/segundo
- **24/7 com 2 réplicas** = ~$62/mês
- **Com scale-to-zero (12h/dia)** = ~$31/mês

## 🚀 Como Usar

### Local (com Docker Compose)
```bash
# Subir todos os serviços
docker-compose up -d

# Verificar logs
docker-compose logs -f api

# Acessar dashboards
http://localhost:8080/swagger       # API Docs
http://localhost:8080/hangfire       # Jobs
http://localhost:8080/health         # Health
http://localhost:8080/metrics        # Métricas
```

### Azure Container Apps

#### 1. Atualizar Recursos
```bash
az containerapp update \
  --name monitor-api-dev \
  --resource-group rg-monitor-dev \
  --cpu 1.0 \
  --memory 2.0Gi \
  --min-replicas 2 \
  --max-replicas 3
```

#### 2. Adicionar Redis (Azure Cache for Redis)
```bash
# Criar Redis
az redis create \
  --name monitor-redis-dev \
  --resource-group rg-monitor-dev \
  --location centralus \
  --sku Basic \
  --vm-size c0

# Pegar connection string
az redis list-keys \
  --name monitor-redis-dev \
  --resource-group rg-monitor-dev
```

#### 3. Configurar Secrets no Container App
```bash
# Redis
az containerapp secret set \
  --name monitor-api-dev \
  --resource-group rg-monitor-dev \
  --secrets redis-connection="<connection-string>"

# Application Insights
az containerapp secret set \
  --name monitor-api-dev \
  --resource-group rg-monitor-dev \
  --secrets appinsights-key="<instrumentation-key>"
```

#### 4. Atualizar Environment Variables
```bash
az containerapp update \
  --name monitor-api-dev \
  --resource-group rg-monitor-dev \
  --set-env-vars \
    "ConnectionStrings__RedisConnection=secretref:redis-connection" \
    "ApplicationInsights__ConnectionString=secretref:appinsights-key"
```

## 🔍 Monitoramento

### Dashboards Disponíveis

1. **Swagger** (`/swagger`)
   - Documentação da API
   - Testes manuais

2. **Hangfire** (`/hangfire`)
   - Jobs agendados
   - Histórico de execuções
   - Retry automático
   - Performance de jobs

3. **Health Checks** (`/health`)
   - Status de componentes
   - PostgreSQL connectivity
   - Memória e disco
   - Readiness/Liveness

4. **Métricas** (`/metrics`)
   - Uso de memória atual
   - Features ativas
   - Links para dashboards

### Logs Estruturados

Com Serilog, todos os logs são estruturados em JSON:

```json
{
  "timestamp": "2025-11-04T21:30:00Z",
  "level": "Information",
  "message": "Request processed",
  "properties": {
    "machineName": "container-abc",
    "environmentName": "Production",
    "requestPath": "/api/registros",
    "duration_ms": 45
  }
}
```

## 📈 Próximos Passos

### Para Aumentar Carga Ainda Mais

1. **Adicionar EF Core**
   - Change Tracker consome mais RAM
   - ~100-200MB adicional

2. **Ativar Distributed Tracing**
   - OpenTelemetry
   - Jaeger/Zipkin

3. **Worker de Processamento Pesado**
   - Processamento de imagens
   - Geração de PDFs
   - Exportação Excel

4. **SignalR para Real-time**
   - WebSockets abertos
   - ~50-100MB por 1000 conexões

5. **Elasticsearch para Logs**
   - Buffer de logs
   - ~200-300MB adicional

## 🎯 Conclusão

Com esta implementação, a aplicação agora consome recursos equivalentes a um sistema de médio porte em produção, permitindo:

✅ Medição precisa de custos Azure
✅ Features realistas de produção
✅ Monitoramento completo
✅ Escalabilidade horizontal
✅ Alta disponibilidade

**Uso Estimado Final**:
- **Memória**: 500MB (idle) → 1.2GB (carga)
- **CPU**: 10% (idle) → 50% (carga média)
- **Custo**: ~$31-62/mês no Azure Container Apps

---

**Data de Implementação**: 04/11/2025
**Versão**: 1.0.0
**Status**: ✅ Pronto para Deploy
