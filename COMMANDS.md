# Comandos Úteis - Monitor Backend

## 🔨 Desenvolvimento Local

### Build e Run
```bash
# Build da solução
dotnet build MonitorBackend.sln

# Restaurar pacotes
dotnet restore MonitorBackend.sln

# Limpar build
dotnet clean MonitorBackend.sln

# Run API
cd src/01-API/MonitorBackend.Api
dotnet run

# Run com Watch (hot reload)
dotnet watch run
```

### Testes
```bash
# Executar testes (quando implementados)
dotnet test MonitorBackend.sln

# Com verbosidade
dotnet test --verbosity detailed

# Com coverage
dotnet test /p:CollectCoverage=true
```

## 🐳 Docker

### Build Local
```bash
# API
docker build -f Dockerfile.api -t monitor-api:latest .

# Multi-stage
docker build --target api-runtime -t monitor-api:latest .
```

### Run Local
```bash
# API
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=..." \
  monitor-api:latest
```

### Docker Compose
```bash
# Iniciar tudo
docker-compose up -d

# Ver logs
docker-compose logs -f

# Apenas API
docker-compose up -d api

# Rebuild
docker-compose build
docker-compose up -d

# Parar tudo
docker-compose down

# Limpar volumes
docker-compose down -v
```

## ☁️ Azure

### Login
```bash
az login
az account set --subscription "SUBSCRIPTION_ID"
```

### Container Registry
```bash
# Login
az acr login --name monitorbackendacr

# Listar imagens
az acr repository list --name monitorbackendacr --output table

# Listar tags
az acr repository show-tags --name monitorbackendacr --repository monitor-api --output table

# Build no ACR (alternativa ao Docker local)
az acr build --registry monitorbackendacr --image monitor-api:v1 -f Dockerfile.api .
```

### Container Apps
```bash
# Listar apps
az containerapp list --resource-group monitor-backend-rg --output table

# Ver detalhes
az containerapp show --name monitor-api --resource-group monitor-backend-rg

# Ver logs
az containerapp logs show --name monitor-api --resource-group monitor-backend-rg --follow

# Atualizar imagem
az containerapp update \
  --name monitor-api \
  --resource-group monitor-backend-rg \
  --image monitorbackendacr.azurecr.io/monitor-api:v2

# Escalar
az containerapp update \
  --name monitor-api \
  --resource-group monitor-backend-rg \
  --min-replicas 1 \
  --max-replicas 3

# Reiniciar
az containerapp revision restart \
  --name monitor-api \
  --resource-group monitor-backend-rg

# Deletar
az containerapp delete --name monitor-api --resource-group monitor-backend-rg --yes
```

### Secrets
```bash
# Criar secret
az containerapp secret set \
  --name monitor-api \
  --resource-group monitor-backend-rg \
  --secrets "db-connection=Host=..."

# Listar secrets
az containerapp secret list --name monitor-api --resource-group monitor-backend-rg

# Usar secret em env var
az containerapp update \
  --name monitor-api \
  --resource-group monitor-backend-rg \
  --set-env-vars "ConnectionStrings__DefaultConnection=secretref:db-connection"
```

## 🗄️ PostgreSQL (Neon)

### Conexão Local
```bash
psql -h YOUR_HOST.neon.tech -U YOUR_USER -d YOUR_DB
```

### Comandos SQL Úteis
```sql
-- Listar tabelas
\dt

-- Descrever tabela
\d registros

-- Ver dados
SELECT * FROM registros ORDER BY data_hora DESC LIMIT 10;

-- Contar registros
SELECT COUNT(*) FROM registros;

-- Inserir teste
INSERT INTO registros (observacao, data_hora, quantidade) 
VALUES ('Teste manual', NOW(), 100);

-- Limpar tabela
TRUNCATE TABLE registros;

-- Sair
\q
```

## 📊 Monitoramento

### Logs em tempo real
```bash
# Azure
az containerapp logs show --name monitor-api --resource-group monitor-backend-rg --follow

# Docker Compose
docker-compose logs -f api

# Docker direto
docker logs -f monitor-api
```

### Métricas
```bash
# Ver uso de recursos (Docker)
docker stats monitor-api

# Azure - via portal
# https://portal.azure.com → Container App → Metrics
```

## 🧪 Testes da API

### cURL
```bash
# Health check
curl http://localhost:8080/

# Registros
curl http://localhost:8080/api/registros

# Com jq para formatar JSON
curl http://localhost:8080/api/registros | jq

# Azure
curl https://monitor-api.azurecontainerapps.io/api/registros
```

### PowerShell
```powershell
# Health check
Invoke-RestMethod -Uri "http://localhost:8080/"

# Registros
Invoke-RestMethod -Uri "http://localhost:8080/api/registros" | ConvertTo-Json

# Azure
$response = Invoke-RestMethod -Uri "https://monitor-api.azurecontainerapps.io/api/registros"
$response.registros | Format-Table
```

### HTTPie (alternativa moderna)
```bash
# Instalar: pip install httpie
http localhost:8080/api/registros
```

## 🔄 CI/CD

### GitHub Actions
```bash
# Ver runs
gh run list

# Ver logs da última run
gh run view --log

# Re-run workflow
gh run rerun WORKFLOW_ID
```

## 🧹 Limpeza

### Local
```bash
# Limpar build
dotnet clean
rm -rf **/bin **/obj

# Docker
docker system prune -a
docker volume prune
```

### Azure
```bash
# Deletar Resource Group (CUIDADO!)
az group delete --name monitor-backend-rg --yes --no-wait

# Deletar apenas Container App
az containerapp delete --name monitor-api --resource-group monitor-backend-rg --yes
```

## 🔧 Utilitários

### Ver versões
```bash
dotnet --version
docker --version
az --version
```

### Atualizar ferramentas
```bash
# .NET
dotnet sdk check
winget upgrade Microsoft.DotNet.SDK.8

# Azure CLI
az upgrade

# Docker Desktop - via GUI
```

## 📦 NuGet

### Adicionar pacotes
```bash
dotnet add src/MonitorBackend.Api/MonitorBackend.Api.csproj package Serilog
```

### Remover pacotes
```bash
dotnet remove src/MonitorBackend.Api/MonitorBackend.Api.csproj package Serilog
```

### Listar pacotes
```bash
dotnet list package
dotnet list package --outdated
```

## 🎯 Dicas

### Watch multiple projects
```bash
# Terminal 1
cd src/MonitorBackend.Api
dotnet watch run
```

### Environment Variables (Windows)
```powershell
$env:ConnectionStrings__DefaultConnection="Host=..."
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run
```

### Environment Variables (Linux/Mac)
```bash
export ConnectionStrings__DefaultConnection="Host=..."
export ASPNETCORE_ENVIRONMENT="Development"
dotnet run
```

---

**Nota**: Substitua valores como `monitor-backend-rg`, `monitorbackendacr` pelos seus valores reais.
