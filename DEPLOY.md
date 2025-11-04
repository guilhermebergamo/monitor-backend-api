# Deploy no Azure Container Apps - Guia Passo a Passo

Este guia detalha como fazer o deploy do Monitor Backend no Azure Container Apps.

## Pré-requisitos

- Azure CLI instalado (`az --version`)
- Docker instalado
- Conta Azure ativa
- Banco PostgreSQL Neon configurado

## 1. Login no Azure

```bash
az login
az account set --subscription "SUA_SUBSCRIPTION_ID"
```

## 2. Criar Resource Group

```bash
az group create \
  --name monitor-backend-rg \
  --location eastus
```

## 3. Criar Container Registry (ACR)

```bash
az acr create \
  --resource-group monitor-backend-rg \
  --name monitorbackendacr \
  --sku Basic \
  --admin-enabled true
```

```bash
az acr login --name monitorbackendacr
```

## 4. Build e Push das Imagens

### API
```bash
# No diretório raiz do projeto
docker build -f Dockerfile.api -t monitor-api:latest .
docker tag monitor-api:latest monitorbackendacr.azurecr.io/monitor-api:latest
docker push monitorbackendacr.azurecr.io/monitor-api:latest
```

### Worker
```bash
docker build -f Dockerfile.worker -t monitor-worker:latest .
docker tag monitor-worker:latest monitorbackendacr.azurecr.io/monitor-worker:latest
docker push monitorbackendacr.azurecr.io/monitor-worker:latest
```

## 5. Criar Container Apps Environment

```bash
az containerapp env create \
  --name monitor-backend-env \
  --resource-group monitor-backend-rg \
  --location eastus
```

## 6. Obter Credenciais do ACR

```bash
$ACR_USERNAME = az acr credential show --name monitorbackendacr --query username -o tsv
$ACR_PASSWORD = az acr credential show --name monitorbackendacr --query "passwords[0].value" -o tsv
```

## 7. Deploy da API

```bash
az containerapp create `
  --name monitor-api `
  --resource-group monitor-backend-rg `
  --environment monitor-backend-env `
  --image monitorbackendacr.azurecr.io/monitor-api:latest `
  --target-port 8080 `
  --ingress external `
  --registry-server monitorbackendacr.azurecr.io `
  --registry-username $ACR_USERNAME `
  --registry-password $ACR_PASSWORD `
  --cpu 0.25 `
  --memory 0.5Gi `
  --min-replicas 0 `
  --max-replicas 1 `
  --env-vars `
    "ConnectionStrings__DefaultConnection=Host=SEU_HOST.neon.tech;Database=SEU_DB;Username=SEU_USER;Password=SUA_SENHA;SSL Mode=Require;Trust Server Certificate=true" `
    "FrontendUrl=https://seu-frontend.azurestaticapps.net" `
    "ASPNETCORE_ENVIRONMENT=Production"
```

## 8. Deploy do Worker

```bash
az containerapp create `
  --name monitor-worker `
  --resource-group monitor-backend-rg `
  --environment monitor-backend-env `
  --image monitorbackendacr.azurecr.io/monitor-worker:latest `
  --registry-server monitorbackendacr.azurecr.io `
  --registry-username $ACR_USERNAME `
  --registry-password $ACR_PASSWORD `
  --cpu 0.25 `
  --memory 0.5Gi `
  --min-replicas 1 `
  --max-replicas 1 `
  --env-vars `
    "WorkerSettings__IntervalMinutes=5" `
    "WorkerSettings__FrontendHealthCheckUrl=https://seu-frontend.azurestaticapps.net" `
    "DOTNET_ENVIRONMENT=Production"
```

## 9. Obter URL da API

```bash
az containerapp show `
  --name monitor-api `
  --resource-group monitor-backend-rg `
  --query properties.configuration.ingress.fqdn -o tsv
```

Resultado exemplo: `monitor-api.proudsand-12345678.eastus.azurecontainerapps.io`

## 10. Testar API

```bash
# Health check
curl https://monitor-api.proudsand-12345678.eastus.azurecontainerapps.io/

# Endpoint de registros
curl https://monitor-api.proudsand-12345678.eastus.azurecontainerapps.io/api/registros
```

## 11. Ver Logs

### API
```bash
az containerapp logs show `
  --name monitor-api `
  --resource-group monitor-backend-rg `
  --follow
```

### Worker
```bash
az containerapp logs show `
  --name monitor-worker `
  --resource-group monitor-backend-rg `
  --follow
```

## 12. Atualizar Imagem (Redeployment)

Quando fizer alterações no código:

```bash
# Rebuild e push
docker build -f Dockerfile.api -t monitorbackendacr.azurecr.io/monitor-api:v2 .
docker push monitorbackendacr.azurecr.io/monitor-api:v2

# Atualizar container app
az containerapp update `
  --name monitor-api `
  --resource-group monitor-backend-rg `
  --image monitorbackendacr.azurecr.io/monitor-api:v2
```

## 13. Configurar Secrets (Recomendado)

```bash
# Criar secret para connection string
az containerapp secret set `
  --name monitor-api `
  --resource-group monitor-backend-rg `
  --secrets "db-connection=Host=..."

# Atualizar env var para usar secret
az containerapp update `
  --name monitor-api `
  --resource-group monitor-backend-rg `
  --set-env-vars "ConnectionStrings__DefaultConnection=secretref:db-connection"
```

## 14. Configurar CORS (Se necessário)

Atualize o `appsettings.json` antes do build:

```json
{
  "FrontendUrl": "https://seu-frontend.azurestaticapps.net"
}
```

Ou configure via variável de ambiente:

```bash
az containerapp update `
  --name monitor-api `
  --resource-group monitor-backend-rg `
  --set-env-vars "FrontendUrl=https://seu-frontend.azurestaticapps.net"
```

## 15. Monitoramento

### Habilitar Application Insights (Opcional)

```bash
# Criar App Insights
az monitor app-insights component create `
  --app monitor-backend-insights `
  --location eastus `
  --resource-group monitor-backend-rg

# Obter instrumentation key
$APPINSIGHTS_KEY = az monitor app-insights component show `
  --app monitor-backend-insights `
  --resource-group monitor-backend-rg `
  --query instrumentationKey -o tsv

# Adicionar ao container app
az containerapp update `
  --name monitor-api `
  --resource-group monitor-backend-rg `
  --set-env-vars "ApplicationInsights__InstrumentationKey=$APPINSIGHTS_KEY"
```

## 16. Limpeza (Deletar tudo)

```bash
az group delete --name monitor-backend-rg --yes --no-wait
```

## 💰 Custos Estimados

- **Container Apps**: ~$5-15/mês (Consumption Plan)
- **Container Registry**: ~$5/mês (Basic SKU)
- **Total**: ~$10-20/mês

## 🔒 Segurança

1. **Nunca** exponha connection strings no código
2. Use **Secrets** do Container Apps para dados sensíveis
3. Configure **Managed Identity** para acesso ao ACR (mais seguro que username/password)
4. Use **Azure Key Vault** para secrets críticos

## 📊 Escalabilidade

Para escalar automaticamente:

```bash
az containerapp update `
  --name monitor-api `
  --resource-group monitor-backend-rg `
  --min-replicas 1 `
  --max-replicas 5 `
  --scale-rule-name http-rule `
  --scale-rule-type http `
  --scale-rule-http-concurrency 10
```

## 🎯 CI/CD com GitHub Actions

Crie `.github/workflows/deploy.yml` para deploy automático.

---

**Dica**: Use o portal do Azure (https://portal.azure.com) para visualizar métricas, logs e configurar alertas.
