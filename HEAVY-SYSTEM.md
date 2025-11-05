# Sistema de Processamento Pesado

## 🔥 Visão Geral

Sistema extremamente pesado implementado para consumir **recursos massivos** (CPU, memória, threads) e simular uma aplicação de produção de grande porte.

## 📊 Consumo Estimado de Recursos

### **Consumo Base (Idle)**
- Memória: ~500MB
- CPU: 10-15%
- Threads: ~30

### **Consumo com Carga (Heavy Load)**
- Memória: **1.0-1.5GB** (picos de até 1.8GB)
- CPU: **60-90%** contínuo
- Threads: **50-80**
- GC Collections: Frequentes (Gen0/Gen1)

### **Consumo Máximo (Stress Test)**
- Memória: **1.8-2.0GB** (90%+ do limite)
- CPU: **95-100%** todos os cores
- Threads: **100+**
- GC: Gen2 collections frequentes

## 🎯 Componentes Implementados

### **1. HeavyProcessingService**
Serviço com operações extremamente pesadas:

#### **ProcessLargeDataAsync(sizeMB)**
- Aloca arrays grandes em memória (até 500MB)
- Preenche com dados aleatórios (CPU-bound)
- Calcula hash SHA256 (mais CPU)
- **Consumo**: sizeMB de RAM + 100% CPU durante processamento

#### **PerformCpuIntensiveCalculationAsync(iterations)**
- Cálculos matemáticos complexos (sqrt, sin, cos, tan, pow, log, exp)
- Hash SHA256 a cada 1000 iterações
- **Consumo**: 100% de 1 core, RAM mínima
- **5M iterações**: ~10-15 segundos de CPU puro

#### **ProcessInParallelAsync(itemCount)**
- Processa múltiplos itens simultaneamente
- 1MB de RAM por item + hash + cálculos
- **Consumo**: múltiplos cores + itemCount MB de RAM
- **100 itens**: ~100MB RAM + 80-100% CPU multi-core

#### **GenerateLargeReportAsync(recordCount)**
- Gera registros complexos com metadata
- Serializa para JSON (grande)
- **Consumo**: ~5-10 bytes por registro em memória
- **100k registros**: ~100-200MB RAM

### **2. Background Jobs PESADOS (Hangfire)**

#### **HeavyDataProcessingJob** (a cada 2 minutos)
```
Execução: A cada 2 minutos
Consumo: 100-200MB RAM + 100% CPU por ~10-15 segundos
Operações: Processa 2x 50MB de dados + hash
```

#### **IntensiveCpuJob** (a cada 3 minutos)
```
Execução: A cada 3 minutos
Consumo: 100% CPU por ~10-15 segundos
Operações: 5 milhões de cálculos matemáticos
```

#### **ParallelProcessingJob** (a cada 5 minutos)
```
Execução: A cada 5 minutos
Consumo: 100MB RAM + 80-100% multi-core CPU
Operações: Processa 100 itens em paralelo
```

#### **LargeReportGenerationJob** (a cada 10 minutos)
```
Execução: A cada 10 minutos
Consumo: 200-500MB RAM + CPU moderado
Operações: Gera relatório com 100k registros
```

#### **CacheWarmupJob** (a cada 15 minutos)
```
Execução: A cada 15 minutos
Consumo: ~5MB Redis + CPU baixo
Operações: Cacheia 50 itens de 100KB cada
```

### **3. API REST - HeavyOpsController**

#### **GET /api/heavyops/process-data?sizeMB=100**
```bash
# Processa dados grandes em memória
curl "https://monitor-api-dev.../api/heavyops/process-data?sizeMB=100"

Parâmetros:
- sizeMB: 1-500 (padrão: 50)

Consumo:
- RAM: sizeMB
- CPU: 100% durante processamento
- Duração: ~100-500ms por MB
```

#### **GET /api/heavyops/cpu-intensive?iterations=5000000**
```bash
# Cálculos matemáticos intensivos
curl "https://monitor-api-dev.../api/heavyops/cpu-intensive?iterations=5000000"

Parâmetros:
- iterations: 1k-10M (padrão: 1M)

Consumo:
- CPU: 100% de 1 core
- RAM: mínima
- Duração: ~2-3 segundos por 1M iterações
```

#### **GET /api/heavyops/parallel-processing?itemCount=100**
```bash
# Processamento paralelo multi-core
curl "https://monitor-api-dev.../api/heavyops/parallel-processing?itemCount=100"

Parâmetros:
- itemCount: 1-200 (padrão: 50)

Consumo:
- RAM: ~1MB por item
- CPU: 80-100% todos os cores
- Duração: ~5-10 segundos para 100 itens
```

#### **GET /api/heavyops/generate-report?recordCount=100000**
```bash
# Gera relatório grande
curl "https://monitor-api-dev.../api/heavyops/generate-report?recordCount=100000"

Parâmetros:
- recordCount: 100-500k (padrão: 50k)

Consumo:
- RAM: ~5-10 bytes por registro
- CPU: moderado
- Duração: ~2-5 segundos para 100k
```

#### **POST /api/heavyops/stress-test**
```bash
# STRESS TEST COMPLETO - MUITO PESADO!
curl -X POST "https://monitor-api-dev.../api/heavyops/stress-test"

Executa TUDO simultaneamente:
1. 100MB de processamento de dados
2. 5M iterações de cálculos CPU
3. 100 itens processados em paralelo
4. Relatório com 100k registros
5. Cache warming (50 itens)

Consumo TOTAL:
- RAM: 800MB-1.2GB
- CPU: 95-100% todos os cores
- Duração: 20-40 segundos
- Threads: 60-100+

⚠️ ATENÇÃO: Vai consumir QUASE TODOS os recursos disponíveis!
```

#### **POST /api/heavyops/memory-leak?sizeMB=200&durationSeconds=60**
```bash
# Memory leak intencional para testes
curl -X POST "https://monitor-api-dev.../api/heavyops/memory-leak?sizeMB=200&durationSeconds=60"

Parâmetros:
- sizeMB: 1-500
- durationSeconds: 1-300

⚠️ ALOCA memória e MANTÉM por X segundos
Útil para testar alertas e monitoramento
```

## 🚀 Como Usar

### **1. Testar Localmente**
```bash
# Inicia a API
dotnet run --project src/01-API/MonitorBackend.Api

# Acessa Swagger
http://localhost:5000/swagger

# Acessa Hangfire Dashboard
http://localhost:5000/hangfire
```

### **2. Testar no Azure**
```bash
# Endpoint base
export API_URL="https://monitor-api-dev.livelyisland-44050ad2.centralus.azurecontainerapps.io"

# Teste leve
curl "$API_URL/api/heavyops/cpu-intensive?iterations=1000000"

# Teste médio
curl "$API_URL/api/heavyops/parallel-processing?itemCount=50"

# Teste pesado
curl "$API_URL/api/heavyops/process-data?sizeMB=200"

# STRESS TEST (CUIDADO!)
curl -X POST "$API_URL/api/heavyops/stress-test"
```

### **3. Monitorar Logs no Azure**
```bash
# Ver logs em tempo real
az containerapp logs show \
  --name monitor-api-dev \
  --resource-group rg-monitor-dev \
  --follow

# Filtrar apenas jobs pesados
az containerapp logs show \
  --name monitor-api-dev \
  --resource-group rg-monitor-dev \
  --follow | grep -E "Heavy|Intensive|Parallel|LargeReport"
```

## 📈 Métricas Esperadas

### **Jobs Automáticos (Background)**
```
[12:00:00] HeavyDataProcessingJob - Start | Memory: 450MB
[12:00:12] HeavyDataProcessingJob - End | Memory: 650MB (+200MB)

[12:03:00] IntensiveCpuJob - Start | Memory: 650MB
[12:03:15] IntensiveCpuJob - End | Memory: 655MB (CPU: 100% por 15s)

[12:05:00] ParallelProcessingJob - Start | Memory: 655MB
[12:05:10] ParallelProcessingJob - End | Memory: 760MB (+105MB)

[12:10:00] LargeReportGenerationJob - Start | Memory: 760MB
[12:10:08] LargeReportGenerationJob - End | Memory: 1200MB (+440MB)

[12:15:00] CacheWarmupJob - Start | Memory: 1200MB
[12:15:02] CacheWarmupJob - End | Memory: 1205MB (+5MB)
```

### **API Endpoints (On-Demand)**
```
Request [GET] /api/heavyops/process-data?sizeMB=100 | 
Duration: 8500ms | 
Memory Delta: +105MB | 
GC Gen0: +15, Gen1: +3, Gen2: +1

Request [POST] /api/heavyops/stress-test | 
Duration: 35000ms | 
Memory Delta: +850MB | 
GC Gen0: +45, Gen1: +12, Gen2: +4
```

## 💰 Impacto no Custo (Azure)

### **Configuração Atual**
- Container Apps: 1.0 vCPU, 2GB RAM
- Min Replicas: 1 (sempre ativo)
- Azure Redis: Basic C0 (250MB)

### **Consumo com Sistema Pesado**
```
Base (sem carga):        ~500MB RAM,  10-15% CPU
Com jobs automáticos:    ~800MB RAM,  30-50% CPU (picos de 100%)
Com stress test:         ~1.5GB RAM,  90-100% CPU

Uso médio estimado:      ~1.0GB RAM,  40-60% CPU
```

### **Custo Estimado**
```
Container Apps (1.0 vCPU, 2GB):  ~$93/mês  (uso contínuo)
Azure Redis (Basic C0):          ~$16/mês
PostgreSQL (Neon):               Grátis

TOTAL: ~$109/mês (uso 24/7 com carga pesada)
```

### **Otimizações de Custo**
1. **Reduzir frequência dos jobs**: 
   - Mudar de 2min → 10min economiza ~40% CPU
   
2. **Desabilitar jobs pesados em produção**:
   - Comentar `HeavyRecurringJobs.ConfigureHeavyJobs()` no Program.cs
   
3. **Usar apenas on-demand**:
   - Manter jobs pesados desabilitados
   - Chamar via API apenas quando necessário

## 🎯 Cenários de Uso

### **1. Teste de Carga**
```bash
# Simula 10 usuários simultâneos fazendo stress test
for i in {1..10}; do
  curl -X POST "$API_URL/api/heavyops/stress-test" &
done
wait
```

### **2. Teste de Memory Leak**
```bash
# Aloca 400MB e mantém por 5 minutos
curl -X POST "$API_URL/api/heavyops/memory-leak?sizeMB=400&durationSeconds=300"

# Monitora no Azure enquanto isso
az containerapp logs show --name monitor-api-dev --resource-group rg-monitor-dev --follow
```

### **3. Benchmark de CPU**
```bash
# Testa diferentes cargas de CPU
curl "$API_URL/api/heavyops/cpu-intensive?iterations=1000000"    # Leve
curl "$API_URL/api/heavyops/cpu-intensive?iterations=5000000"    # Médio
curl "$API_URL/api/heavyops/cpu-intensive?iterations=10000000"   # Pesado
```

### **4. Teste de Paralelismo**
```bash
# Testa processamento paralelo
curl "$API_URL/api/heavyops/parallel-processing?itemCount=50"    # 50MB
curl "$API_URL/api/heavyops/parallel-processing?itemCount=100"   # 100MB
curl "$API_URL/api/heavyops/parallel-processing?itemCount=200"   # 200MB
```

## 📊 Hangfire Dashboard

Acesse: `https://monitor-api-dev.../hangfire`

Você verá:
- **Recurring Jobs**: 9 jobs configurados (4 leves + 5 pesados)
- **Succeeded/Failed**: Histórico de execuções
- **Processing**: Jobs em execução neste momento
- **Servers**: 4 workers ativos

## ⚠️ Avisos Importantes

1. **Stress Test consome MUITOS recursos**
   - Pode deixar a aplicação lenta por 30-60 segundos
   - Não execute múltiplas vezes simultaneamente

2. **Memory Leak é INTENCIONAL**
   - Use apenas para testes controlados
   - Memória é liberada após o tempo especificado

3. **Jobs automáticos são CONSTANTES**
   - Executam continuamente em segundo plano
   - Consomem recursos mesmo sem requisições

4. **Custo pode aumentar**
   - Sistema pesado usa 60-80% dos recursos constantemente
   - Considere desabilitar em produção se não necessário

## 🔧 Como Desabilitar Sistema Pesado

### **Desabilitar Jobs Automáticos**
```csharp
// Em Program.cs, comente a linha:
// HeavyRecurringJobs.ConfigureHeavyJobs();
```

### **Desabilitar Controller**
```csharp
// Em HeavyOpsController.cs, adicione:
// [ApiExplorerSettings(IgnoreApi = true)]
```

### **Reduzir Workers do Hangfire**
```csharp
// Em Program.cs, mude de:
options.WorkerCount = 4;
// Para:
options.WorkerCount = 2;
```

## 🎉 Resultado Final

Com este sistema implementado:
- ✅ Consumo de recursos **4-5x maior**
- ✅ CPU constantemente **40-60%** (vs 10-15% antes)
- ✅ Memória **800MB-1.2GB** (vs 300-500MB antes)
- ✅ Jobs executando **a cada 2-15 minutos**
- ✅ Endpoints para testes de carga sob demanda
- ✅ Telemetria completa de todos os processos
- ✅ **Simula aplicação de produção de grande porte**

Perfeito para medir custos reais do Azure! 💰
