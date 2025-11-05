# Sistema de Telemetria de Recursos

## 📊 Visão Geral

Sistema completo de monitoramento de recursos (memória, CPU, threads, GC) que loga automaticamente o uso de recursos em múltiplos pontos da aplicação.

## 🎯 Onde os Logs Aparecem

### **Azure Container Apps - Log Stream**
```bash
az containerapp logs show \
  --name monitor-api-dev \
  --resource-group rg-monitor-dev \
  --follow
```

Acesso via Portal Azure:
1. Acesse: https://portal.azure.com
2. Container Apps > monitor-api-dev
3. Monitoring > Log stream
4. Filtre por "Resource Usage" para ver apenas logs de telemetria

## 📍 Pontos de Log Automático

### 1. **Background Worker (A cada 1 minuto)**
```
Resource Usage [Periodic Check] - Memory: 450.23MB (Working: 380.12MB, GC: 350.45MB) | CPU: 12.5s | Threads: 25 | GC Gen0: 15, Gen1: 3, Gen2: 1
```
- **Frequência**: A cada 1 minuto
- **Contexto**: "Periodic Check"
- **Arquivo**: `ResourceMonitorWorker.cs`

### 2. **Cada Requisição HTTP**
```
Request [GET] /api/registros | Duration: 245ms | Status: 200 | Memory Before: 450.23MB | Memory After: 452.10MB | Memory Delta: +1.87MB | Working Set Delta: +1.50MB | GC Gen0: +1, Gen1: +0, Gen2: +0
```
- **Frequência**: Cada requisição (exceto /health e /hangfire)
- **Info Capturada**: 
  - Duração da requisição
  - Memória ANTES e DEPOIS
  - Delta de memória usado
  - Coleções do GC disparadas
- **Arquivo**: `ResourceTelemetryMiddleware.cs`

### 3. **Background Jobs do Hangfire**

#### DataCleanupJob (diário às 2h)
```
Resource Usage [DataCleanupJob - Start] - Memory: 450.23MB ...
Resource Usage [DataCleanupJob - End] - Memory: 455.67MB ...
```

#### StatisticsAggregationJob (a cada hora)
```
Resource Usage [StatisticsAggregationJob - Start] - Memory: 450.23MB ...
Resource Usage [StatisticsAggregationJob - End] - Memory: 680.45MB ...
```

#### MonthlyReportJob (1º de cada mês)
```
Resource Usage [MonthlyReportJob - Start] - Memory: 450.23MB ...
Resource Usage [MonthlyReportJob - End] - Memory: 520.30MB ...
```

#### DatabaseHealthCheckJob (a cada 5 minutos)
```
Resource Usage [DatabaseHealthCheckJob - Start] - Memory: 450.23MB ...
Resource Usage [DatabaseHealthCheckJob - End] - Memory: 451.10MB ...
```

### 4. **Endpoint /metrics (Em Tempo Real)**
```bash
curl https://monitor-api-dev.livelyisland-44050ad2.centralus.azurecontainerapps.io/metrics
```

Resposta JSON:
```json
{
  "service": "Monitor Backend API",
  "timestamp": "2025-11-04T12:34:56Z",
  "environment": "Production",
  "resources": {
    "memory": {
      "gc_memory_mb": 350.45,
      "working_set_mb": 380.12,
      "private_memory_mb": 420.67,
      "total_mb": 450.23
    },
    "cpu": {
      "total_processor_time_seconds": 12.5,
      "user_processor_time_seconds": 10.2
    },
    "threads": {
      "count": 25
    },
    "garbage_collector": {
      "gen0_collections": 15,
      "gen1_collections": 3,
      "gen2_collections": 1
    }
  }
}
```

## 📈 Métricas Coletadas

### **Memória**
- **GC Memory**: Memória alocada pelo Garbage Collector
- **Working Set**: Memória física (RAM) usada
- **Private Memory**: Memória total alocada pelo processo
- **Total**: Maior valor entre GC e Private

### **CPU**
- **Total Processor Time**: Tempo total de CPU (user + kernel)
- **User Processor Time**: Tempo de CPU em modo usuário

### **Threads**
- **Count**: Número total de threads ativas

### **Garbage Collector**
- **Gen0**: Coleções de geração 0 (frequentes, objetos temporários)
- **Gen1**: Coleções de geração 1 (intermediárias)
- **Gen2**: Coleções de geração 2 (raras, objetos de longa duração)

## 🔍 Como Interpretar os Logs

### **Exemplo de Log Completo**
```
[14:30:15 INF] Resource Usage [GET /api/registros] - Memory: 450.23MB (Working: 380.12MB, GC: 350.45MB) | CPU: 12.5s | Threads: 25 | GC Gen0: 15, Gen1: 3, Gen2: 1
```

**Interpretação:**
- **450.23MB**: Total de memória usada
- **380.12MB**: Memória física (RAM)
- **350.45MB**: Memória gerenciada pelo .NET
- **12.5s**: 12.5 segundos de CPU consumidos desde o início
- **25 threads**: Aplicação usando 25 threads
- **Gen0: 15**: 15 coleções rápidas (normal)
- **Gen1: 3**: 3 coleções intermediárias (ok)
- **Gen2: 1**: 1 coleção completa (desejável ser baixo)

### **Delta de Memória em Requisições**
```
Memory Before: 450.23MB | Memory After: 452.10MB | Memory Delta: +1.87MB
```

**Interpretação:**
- Esta requisição alocou **1.87MB** de memória
- Se o delta for muito alto (>50MB), pode indicar problema de performance

### **Garbage Collector**
```
GC Gen0: +1, Gen1: +0, Gen2: +0
```

**Interpretação:**
- **+1 Gen0**: 1 coleta rápida durante a requisição (normal)
- **+0 Gen1/Gen2**: Nenhuma coleta pesada (ótimo!)

## 📊 Monitoramento no Azure

### **Application Insights (Se Configurado)**
```csharp
// appsettings.json
"ApplicationInsights": {
  "ConnectionString": "InstrumentationKey=..."
}
```

Os logs estruturados também aparecem em:
- **Azure Monitor** > Logs
- **Application Insights** > Transaction Search
- **Application Insights** > Performance

### **Queries Úteis (Kusto/KQL)**

#### Ver uso de memória ao longo do tempo
```kql
traces
| where message contains "Resource Usage"
| extend memory_mb = extract("Memory: ([0-9.]+)MB", 1, message)
| project timestamp, memory_mb
| render timechart
```

#### Ver requisições mais custosas
```kql
traces
| where message contains "Memory Delta"
| extend delta = extract("Memory Delta: ([+-]?[0-9.]+)MB", 1, message)
| where todouble(delta) > 10  // Mais de 10MB
| order by timestamp desc
```

#### Jobs mais pesados
```kql
traces
| where message contains "Job - End"
| extend context = extract("\\[(.+) - End\\]", 1, message)
| extend memory = extract("Memory: ([0-9.]+)MB", 1, message)
| summarize avg_memory = avg(todouble(memory)) by context
| order by avg_memory desc
```

## 🛠️ Arquivos Criados/Modificados

### **Novos Arquivos**
1. `src/04-Infrastructure/MonitorBackend.Infrastructure/Services/ResourceTelemetryService.cs`
   - Serviço de telemetria que coleta métricas
   
2. `src/04-Infrastructure/MonitorBackend.Infrastructure/BackgroundJobs/ResourceMonitorWorker.cs`
   - Worker que loga a cada 1 minuto
   
3. `src/01-API/MonitorBackend.Api/Middleware/ResourceTelemetryMiddleware.cs`
   - Middleware que loga em cada requisição

### **Arquivos Modificados**
1. `src/01-API/MonitorBackend.Api/Program.cs`
   - Registra serviço de telemetria
   - Registra middleware
   - Atualiza endpoint /metrics

2. `src/04-Infrastructure/MonitorBackend.Infrastructure/BackgroundJobs/RecurringJobs.cs`
   - Todos os 4 jobs agora logam recursos no início e fim

## 🎯 Benefícios

### **1. Detecção de Memory Leaks**
- Se a memória cresce continuamente sem cair, há leak
- Logs periódicos mostram tendência ao longo do tempo

### **2. Performance de Requisições**
- Delta de memória identifica endpoints problemáticos
- Duração mostra requisições lentas

### **3. Otimização de Jobs**
- Compara memória Start vs End
- Identifica jobs que consomem muitos recursos

### **4. Análise de Custo**
- Correlaciona uso de recursos com custo do Azure
- Identifica picos de consumo

### **5. Troubleshooting**
- Logs antes/depois facilitam debug
- Contexto completo em cada log

## 🚀 Exemplo de Uso no Azure

### **1. Ver logs em tempo real**
```bash
# Conecta ao log stream
az containerapp logs show \
  --name monitor-api-dev \
  --resource-group rg-monitor-dev \
  --follow

# Filtra apenas telemetria
az containerapp logs show \
  --name monitor-api-dev \
  --resource-group rg-monitor-dev \
  --follow | grep "Resource Usage"
```

### **2. Ver logs históricos**
```bash
# Últimas 100 linhas
az containerapp logs show \
  --name monitor-api-dev \
  --resource-group rg-monitor-dev \
  --tail 100
```

### **3. Exportar logs para análise**
```bash
# Salva em arquivo
az containerapp logs show \
  --name monitor-api-dev \
  --resource-group rg-monitor-dev \
  --tail 1000 > resource-logs.txt

# Filtra apenas deltas grandes
grep "Memory Delta" resource-logs.txt | grep -E "\+[0-9]{2,}\."
```

## 💡 Dicas de Uso

1. **Baseline**: Observe por 1 hora para estabelecer consumo normal
2. **Alertas**: Configure alertas se memória > 1.5GB (75% do limite)
3. **Trends**: Exporte logs diários para análise de tendência
4. **Correlação**: Compare logs de telemetria com Application Insights
5. **Otimização**: Foque em requisições com delta > 10MB

## 📝 Exemplo Real Esperado

```
[14:00:00 INF] Resource Usage [Startup] - Memory: 250.12MB ...
[14:01:00 INF] Resource Usage [Periodic Check] - Memory: 280.45MB ...
[14:01:30 INF] Request [GET] /api/registros | Duration: 245ms | Memory Delta: +2.10MB ...
[14:02:00 INF] Resource Usage [Periodic Check] - Memory: 285.67MB ...
[14:05:00 INF] Resource Usage [DatabaseHealthCheckJob - Start] - Memory: 290.23MB ...
[14:05:01 INF] Resource Usage [DatabaseHealthCheckJob - End] - Memory: 291.10MB ...
[14:03:00 INF] Resource Usage [Periodic Check] - Memory: 295.30MB ...
[15:00:00 INF] Resource Usage [StatisticsAggregationJob - Start] - Memory: 300.12MB ...
[15:00:12 INF] Resource Usage [StatisticsAggregationJob - End] - Memory: 680.45MB ...
[15:01:00 INF] Resource Usage [Periodic Check] - Memory: 320.67MB ...  # GC liberou memória
```

## 🎉 Pronto!

Agora toda execução da aplicação loga automaticamente o uso de recursos, permitindo:
- ✅ Monitoramento contínuo no Azure
- ✅ Análise de performance por requisição
- ✅ Detecção de memory leaks
- ✅ Otimização de custos
- ✅ Troubleshooting facilitado
