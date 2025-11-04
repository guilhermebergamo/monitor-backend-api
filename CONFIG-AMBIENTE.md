# 🎯 Configuração Específica do Ambiente

## ✅ Ambiente de Desenvolvimento (PRONTO)

### Azure Resources
- **Resource Group**: `rg-monitor-dev`
- **Container Registry**: `monitordevregistry` (`monitordevregistry.azurecr.io`)
- **Container App (API)**: `monitor-api-dev` ⚠️ *PRECISA CRIAR*
- **Container Apps Environment**: *Já existe (worker rodando)* - verificar nome
- **Frontend URL**: `https://white-river-0f9f4b40f.3.azurestaticapps.net`
- **Database**: Neon PostgreSQL (conexão já configurada)

### GitHub Secrets Necessários
- ✅ `AZURE_CREDENTIALS_DEV` - Service Principal JSON
- ✅ `ACR_NAME` - Valor: `monitordevregistry`
- ⚠️ `ACR_USERNAME` - (opcional) obtido via `az acr credential show`
- ⚠️ `ACR_PASSWORD` - (opcional) obtido via `az acr credential show`

### Status dos Workflows
- ✅ `.github/workflows/deploy-dev.yml` - **Configurado e pronto**
- ⏸️ `.github/workflows/deploy-prod.yml` - Preparado mas inativo (prod não existe)

## ❌ Ambiente de Produção (NÃO EXISTE)

**Ainda não foi criado.** Quando necessário, seguir os passos no `GITHUB-ACTIONS-SETUP.md` seção "Para Production".

### Será necessário criar:
- [ ] Resource Group: `rg-monitor-prod`
- [ ] Container Apps Environment para prod
- [ ] Container App: `monitor-api-prod`
- [ ] Service Principal para produção
- [ ] Secret `AZURE_CREDENTIALS_PROD` no GitHub
- [ ] Frontend de produção (URL ainda não definida)
- [ ] Database de produção (ainda não criado)

## 📋 Próximos Passos Imediatos

### 1. Verificar o Environment do Container Apps
```bash
az login
az containerapp env list --resource-group rg-monitor-dev --output table
```
Anote o nome do environment (onde o worker está rodando).

### 2. Criar o Container App para a API
```bash
ENVIRONMENT_NAME="SEU_ENVIRONMENT_NAME"  # Do comando acima

az containerapp create \
  --name monitor-api-dev \
  --resource-group rg-monitor-dev \
  --environment $ENVIRONMENT_NAME \
  --image mcr.microsoft.com/azuredocs/containerapps-helloworld:latest \
  --target-port 8080 \
  --ingress external \
  --registry-server monitordevregistry.azurecr.io \
  --cpu 0.5 \
  --memory 1.0Gi \
  --min-replicas 0 \
  --max-replicas 3
```

### 3. Configurar Variáveis de Ambiente
```bash
az containerapp update \
  --name monitor-api-dev \
  --resource-group rg-monitor-dev \
  --set-env-vars \
    ASPNETCORE_ENVIRONMENT="Development" \
    ASPNETCORE_URLS="http://+:8080" \
    ConnectionStrings__DefaultConnection="Host=ep-dark-dream-a820yymb-pooler.eastus2.azure.neon.tech;Database=MonitordbDevelop;Username=neondb_owner;Password=npg_Er10paDuIsmd;SSL Mode=Require;Trust Server Certificate=true" \
    FrontendUrl="https://white-river-0f9f4b40f.3.azurestaticapps.net"
```

### 4. Criar Service Principal
```bash
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

az ad sp create-for-rbac \
  --name "github-actions-monitor-dev" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/rg-monitor-dev \
  --sdk-auth
```
**Copie TODO o JSON retornado!**

### 5. Configurar GitHub Secrets
Acesse: `https://github.com/SEU_USUARIO/REPO/settings/secrets/actions`

Adicione:
- `AZURE_CREDENTIALS_DEV` = JSON do Service Principal
- `ACR_NAME` = `monitordevregistry`

### 6. Criar Repositório no GitHub
```bash
# No GitHub, crie um novo repositório
# Depois:
git remote add origin https://github.com/SEU_USUARIO/monitor-backend-api.git
git push -u origin develop
```

### 7. Criar Branch Main
```bash
git checkout -b main
git push -u origin main
git checkout develop
```

### 8. Testar Deploy
Faça qualquer alteração, commit e push para `develop`:
```bash
git add .
git commit -m "test: testar deploy automático"
git push origin develop
```

Acompanhe em: `https://github.com/SEU_USUARIO/REPO/actions`

## 🎉 Resultado Esperado

Após o deploy bem-sucedido:
1. Acesse a URL do Container App (obter com comando abaixo)
2. Teste os endpoints da API

```bash
# Obter URL da API
az containerapp show \
  --name monitor-api-dev \
  --resource-group rg-monitor-dev \
  --query properties.configuration.ingress.fqdn -o tsv
```

### Endpoints Disponíveis
- `GET /` - Health check
- `GET /api/registros` - Lista registros do banco
- `GET /api/registros/health` - Health check do controller
- Swagger: `/swagger` (em desenvolvimento)

## 🔍 Comandos Úteis

```bash
# Ver logs em tempo real
az containerapp logs show --name monitor-api-dev --resource-group rg-monitor-dev --follow

# Ver status
az containerapp show --name monitor-api-dev --resource-group rg-monitor-dev --query properties.runningStatus

# Ver revisões (deploys)
az containerapp revision list --name monitor-api-dev --resource-group rg-monitor-dev --output table

# Escalar manualmente
az containerapp update --name monitor-api-dev --resource-group rg-monitor-dev --min-replicas 1 --max-replicas 5
```

## 📚 Documentação Completa
- **Setup Detalhado**: `GITHUB-ACTIONS-SETUP.md`
- **Guia Rápido**: `PROXIMOS-PASSOS.md`
- **Comandos Úteis**: `COMMANDS.md`
