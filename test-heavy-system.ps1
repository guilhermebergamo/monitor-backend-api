# Script de Teste do Sistema Pesado
# Execute após o deploy no Azure

$API_URL = "https://monitor-api-dev.livelyisland-44050ad2.centralus.azurecontainerapps.io"

Write-Host "`n🔥 TESTES DO SISTEMA PESADO 🔥`n" -ForegroundColor Yellow

# Função para fazer requisição e mostrar resultado
function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET"
    )
    
    Write-Host "`n[$Name]" -ForegroundColor Cyan
    Write-Host "URL: $Url" -ForegroundColor Gray
    Write-Host "Fazendo requisição..." -ForegroundColor Gray
    
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    try {
        if ($Method -eq "GET") {
            $response = Invoke-RestMethod -Uri $Url -Method Get -TimeoutSec 120
        } else {
            $response = Invoke-RestMethod -Uri $Url -Method Post -TimeoutSec 120
        }
        
        $stopwatch.Stop()
        
        Write-Host "✅ Sucesso! Duração: $($stopwatch.ElapsedMilliseconds)ms" -ForegroundColor Green
        
        if ($response.durationMs) {
            Write-Host "   Processamento: $($response.durationMs)ms" -ForegroundColor White
        }
        if ($response.processedSizeMB) {
            Write-Host "   Processado: $($response.processedSizeMB)MB" -ForegroundColor White
        }
        if ($response.iterations) {
            Write-Host "   Iterações: $($response.iterations)" -ForegroundColor White
        }
        if ($response.itemCount) {
            Write-Host "   Itens: $($response.itemCount)" -ForegroundColor White
        }
        if ($response.recordCount) {
            Write-Host "   Registros: $($response.recordCount)" -ForegroundColor White
        }
        if ($response.totalDurationMs) {
            Write-Host "   Duração Total: $($response.totalDurationMs)ms" -ForegroundColor White
        }
        if ($response.memoryUsedMB) {
            Write-Host "   Memória Usada: $($response.memoryUsedMB)MB" -ForegroundColor White
        }
        
        return $true
    }
    catch {
        $stopwatch.Stop()
        Write-Host "❌ Erro: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Menu
Write-Host "Escolha o teste:" -ForegroundColor Yellow
Write-Host "1. Teste Leve (CPU Intensivo - 1M iterações)"
Write-Host "2. Teste Médio (Processamento Paralelo - 50 itens)"
Write-Host "3. Teste Pesado (Processar 200MB de dados)"
Write-Host "4. Teste EXTREMO (Stress Test Completo - 30-60s)"
Write-Host "5. Todos os testes sequenciais"
Write-Host "6. Ver métricas atuais"
Write-Host "7. Ver Hangfire Dashboard (abre navegador)"
Write-Host "0. Sair"
Write-Host ""

$choice = Read-Host "Digite o número"

switch ($choice) {
    "1" {
        Test-Endpoint -Name "CPU Intensivo (1M iterações)" -Url "$API_URL/api/heavyops/cpu-intensive?iterations=1000000"
    }
    "2" {
        Test-Endpoint -Name "Processamento Paralelo (50 itens)" -Url "$API_URL/api/heavyops/parallel-processing?itemCount=50"
    }
    "3" {
        Test-Endpoint -Name "Processar 200MB" -Url "$API_URL/api/heavyops/process-data?sizeMB=200"
    }
    "4" {
        Write-Host "`n⚠️  ATENÇÃO: Este teste vai consumir MUITOS recursos por 30-60 segundos!" -ForegroundColor Red
        $confirm = Read-Host "Deseja continuar? (s/n)"
        if ($confirm -eq "s") {
            Test-Endpoint -Name "STRESS TEST COMPLETO" -Url "$API_URL/api/heavyops/stress-test" -Method "POST"
        }
    }
    "5" {
        Write-Host "`n🧪 Executando TODOS os testes...`n" -ForegroundColor Yellow
        
        Test-Endpoint -Name "1. CPU Intensivo" -Url "$API_URL/api/heavyops/cpu-intensive?iterations=1000000"
        Start-Sleep -Seconds 2
        
        Test-Endpoint -Name "2. Processar 100MB" -Url "$API_URL/api/heavyops/process-data?sizeMB=100"
        Start-Sleep -Seconds 2
        
        Test-Endpoint -Name "3. Processamento Paralelo" -Url "$API_URL/api/heavyops/parallel-processing?itemCount=50"
        Start-Sleep -Seconds 2
        
        Test-Endpoint -Name "4. Gerar Relatório (50k)" -Url "$API_URL/api/heavyops/generate-report?recordCount=50000"
        Start-Sleep -Seconds 2
        
        Write-Host "`n✅ Todos os testes concluídos!" -ForegroundColor Green
    }
    "6" {
        Write-Host "`n📊 Métricas Atuais:`n" -ForegroundColor Cyan
        $metrics = Invoke-RestMethod -Uri "$API_URL/metrics" -Method Get
        
        Write-Host "Serviço: $($metrics.service)" -ForegroundColor White
        Write-Host "Ambiente: $($metrics.environment)" -ForegroundColor White
        Write-Host "Timestamp: $($metrics.timestamp)" -ForegroundColor White
        Write-Host ""
        Write-Host "💾 Memória:" -ForegroundColor Yellow
        Write-Host "  GC Memory: $($metrics.resources.memory.gc_memory_mb) MB" -ForegroundColor White
        Write-Host "  Working Set: $($metrics.resources.memory.working_set_mb) MB" -ForegroundColor White
        Write-Host "  Private Memory: $($metrics.resources.memory.private_memory_mb) MB" -ForegroundColor White
        Write-Host "  Total: $($metrics.resources.memory.total_mb) MB" -ForegroundColor White
        Write-Host ""
        Write-Host "🖥️  CPU:" -ForegroundColor Yellow
        Write-Host "  Total Processor Time: $($metrics.resources.cpu.total_processor_time_seconds)s" -ForegroundColor White
        Write-Host "  User Processor Time: $($metrics.resources.cpu.user_processor_time_seconds)s" -ForegroundColor White
        Write-Host ""
        Write-Host "🔄 Threads: $($metrics.resources.threads.count)" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "🗑️  Garbage Collector:" -ForegroundColor Yellow
        Write-Host "  Gen0: $($metrics.resources.garbage_collector.gen0_collections)" -ForegroundColor White
        Write-Host "  Gen1: $($metrics.resources.garbage_collector.gen1_collections)" -ForegroundColor White
        Write-Host "  Gen2: $($metrics.resources.garbage_collector.gen2_collections)" -ForegroundColor White
    }
    "7" {
        Write-Host "`nAbrindo Hangfire Dashboard...`n" -ForegroundColor Cyan
        Start-Process "$API_URL/hangfire"
    }
    "0" {
        Write-Host "`nSaindo...`n" -ForegroundColor Gray
        exit
    }
    default {
        Write-Host "`n❌ Opção inválida!`n" -ForegroundColor Red
    }
}

Write-Host "`n✅ Concluído!`n" -ForegroundColor Green
Write-Host "📊 Ver logs no Azure:" -ForegroundColor Cyan
Write-Host "az containerapp logs show --name monitor-api-dev --resource-group rg-monitor-dev --follow`n" -ForegroundColor Gray
