# 🚀 Guia de Deploy Automático - GitHub Actions + Azure

Este guia explica como configurar o deploy automático da API para o Azure usando GitHub Actions.

## 📋 Pré-requisitos

- Conta no GitHub
- Conta no Azure com um Resource Group criado
- Azure CLI instalado localmente (para configuração inicial)
- Permissões de Contributor no Resource Group do Azure

## 🏗️ Estrutura de Branches e Ambientes

```
develop  → Azure Development Environment (deploy automático)
main     → Azure Production Environment (deploy automático)
```

## 🔧 Configuração do Azure

### 1. Criar Azure Container Registry (ACR)

Primeiro, precisamos de um Container Registry para armazenar as imagens Docker:

```bash
# Login no Azure
az login

# Definir variáveis
ACR_NAME="monitorregistry"  # ⚠️ Ajuste para seu registry (deve ser único globalmente)
RESOURCE_GROUP="rg-monitor-dev"
LOCATION="eastus"

# Criar Azure Container Registry (se ainda não tiver)
az acr create \
  --name $ACR_NAME \
  --resource-group $RESOURCE_GROUP \
  --sku Basic \
  --admin-enabled true

# Obter credenciais do ACR (anote para usar nos secrets do GitHub)
az acr credential show --name $ACR_NAME
```

### 2. Criar Container Apps Environment e Container App

#### Para Development:
```bash
# Definir variáveis
RESOURCE_GROUP="rg-monitor-dev"
LOCATION="eastus"
ENVIRONMENT_NAME="monitor-env-dev"
CONTAINER_APP_NAME="monitor-api-dev"
ACR_NAME="monitorregistry"  # Seu ACR

# Criar Container Apps Environment (se ainda não tiver)
az containerapp env create \
  --name $ENVIRONMENT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION

# Criar Container App
az containerapp create \
  --name $CONTAINER_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --environment $ENVIRONMENT_NAME \
  --image mcr.microsoft.com/azuredocs/containerapps-helloworld:latest \
  --target-port 8080 \
  --ingress external \
  --registry-server "${ACR_NAME}.azurecr.io" \
  --cpu 0.5 \
  --memory 1.0Gi \
  --min-replicas 0 \
  --max-replicas 3

# Configurar variáveis de ambiente
az containerapp update \
  --name $CONTAINER_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --set-env-vars \
    ASPNETCORE_ENVIRONMENT="Development" \
    ASPNETCORE_URLS="http://+:8080" \
    ConnectionStrings__DefaultConnection="Host=SEU_HOST;Database=SEU_DB;Username=SEU_USER;Password=SUA_SENHA;SSL Mode=Require;Trust Server Certificate=true" \
    FrontendUrl="https://seu-frontend-dev.azurestaticapps.net"
```

#### Para Production:
```bash
# Definir variáveis
RESOURCE_GROUP="rg-monitor-prod"
LOCATION="eastus"
ENVIRONMENT_NAME="monitor-env-prod"
CONTAINER_APP_NAME="monitor-api-prod"
ACR_NAME="monitorregistry"  # Mesmo ACR

# Criar Resource Group de produção (se ainda não existir)
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# Criar Container Apps Environment
az containerapp env create \
  --name $ENVIRONMENT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION

# Criar Container App
az containerapp create \
  --name $CONTAINER_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --environment $ENVIRONMENT_NAME \
  --image mcr.microsoft.com/azuredocs/containerapps-helloworld:latest \
  --target-port 8080 \
  --ingress external \
  --registry-server "${ACR_NAME}.azurecr.io" \
  --cpu 1.0 \
  --memory 2.0Gi \
  --min-replicas 1 \
  --max-replicas 10

# Configurar variáveis de ambiente
az containerapp update \
  --name $CONTAINER_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --set-env-vars \
    ASPNETCORE_ENVIRONMENT="Production" \
    ASPNETCORE_URLS="http://+:8080" \
    ConnectionStrings__DefaultConnection="Host=SEU_HOST_PROD;Database=SEU_DB_PROD;Username=SEU_USER;Password=SUA_SENHA;SSL Mode=Require;Trust Server Certificate=true" \
    FrontendUrl="https://seu-frontend-prod.azurestaticapps.net"
```

### 3. Criar Service Principal para GitHub Actions

O Service Principal permite que o GitHub Actions se autentique no Azure.

#### Para Development:
```bash
# Obter seu Subscription ID
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# Criar Service Principal
az ad sp create-for-rbac \
  --name "github-actions-monitor-dev" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/rg-monitor-dev \
  --sdk-auth

# ⚠️ IMPORTANTE: Copie TODO o JSON retornado. Será usado no GitHub!
```

#### Para Production:
```bash
# Obter seu Subscription ID
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# Criar Service Principal
az ad sp create-for-rbac \
  --name "github-actions-monitor-prod" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/rg-monitor-prod \
  --sdk-auth

# ⚠️ IMPORTANTE: Copie TODO o JSON retornado. Será usado no GitHub!
```

**Exemplo do JSON retornado:**
```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

## 🔐 Configuração dos Secrets no GitHub

### 1. Acessar Configurações do Repositório

1. Vá para o repositório no GitHub
2. Clique em **Settings** (Configurações)
3. No menu lateral, clique em **Secrets and variables** → **Actions**

### 2. Adicionar Secrets

Clique em **New repository secret** e adicione:

#### Secrets para Azure:
- **Nome**: `AZURE_CREDENTIALS_DEV`
- **Valor**: Cole o JSON completo do Service Principal criado para dev

- **Nome**: `AZURE_CREDENTIALS_PROD`
- **Valor**: Cole o JSON completo do Service Principal criado para prod

#### Secrets para Container Registry:
- **Nome**: `ACR_NAME`
- **Valor**: Nome do seu Container Registry (ex: `monitorregistry`)

- **Nome**: `ACR_USERNAME` (opcional, se usar autenticação do admin)
- **Valor**: Username do ACR (obtido com `az acr credential show`)

- **Nome**: `ACR_PASSWORD` (opcional, se usar autenticação do admin)
- **Valor**: Password do ACR (obtido com `az acr credential show`)

## 📝 Atualizar os Workflows

Edite os arquivos de workflow e atualize as variáveis:

### `.github/workflows/deploy-dev.yml`
```yaml
env:
  AZURE_CONTAINER_APP_NAME: 'monitor-api-dev' # ⚠️ Seu Container App Name
  RESOURCE_GROUP: 'rg-monitor-dev' # ⚠️ Já configurado
  CONTAINER_REGISTRY: 'monitorregistry.azurecr.io' # ⚠️ Seu ACR
```

### `.github/workflows/deploy-prod.yml`
```yaml
env:
  AZURE_CONTAINER_APP_NAME: 'monitor-api-prod' # ⚠️ Seu Container App Name
  RESOURCE_GROUP: 'rg-monitor-prod' # ⚠️ Já configurado
  CONTAINER_REGISTRY: 'monitorregistry.azurecr.io' # ⚠️ Seu ACR
```

## 🚀 Testando o Deploy

### 1. Fazer commit e push para develop

```bash
git add .
git commit -m "feat: configurar GitHub Actions para deploy automático"
git push origin develop
```

### 2. Acompanhar o Deploy

1. Vá para o repositório no GitHub
2. Clique na aba **Actions**
3. Você verá o workflow em execução
4. Clique nele para ver os logs em tempo real

### 3. Verificar o Deploy

Após o deploy, obtenha a URL do Container App:
```bash
# Development
az containerapp show --name monitor-api-dev --resource-group rg-monitor-dev --query properties.configuration.ingress.fqdn -o tsv

# Production
az containerapp show --name monitor-api-prod --resource-group rg-monitor-prod --query properties.configuration.ingress.fqdn -o tsv
```

Acesse a URL retornada (geralmente algo como: `https://monitor-api-dev.xxx.azurecontainerapps.io`)

## 🔄 Fluxo de Trabalho

### Desenvolvimento
```bash
# Trabalhar em uma feature branch
git checkout -b feature/nova-funcionalidade

# Fazer commits
git add .
git commit -m "feat: adicionar nova funcionalidade"

# Fazer merge para develop (dispara deploy automático para dev)
git checkout develop
git merge feature/nova-funcionalidade
git push origin develop
```

### Produção
```bash
# Após testes em develop, fazer merge para main
git checkout main
git merge develop
git push origin main
# Deploy automático para produção será iniciado
```

## 🛡️ Proteções Recomendadas

### Proteger Branch de Produção

1. Vá em **Settings** → **Branches**
2. Clique em **Add rule**
3. Em **Branch name pattern**, digite `main`
4. Marque:
   - ✅ Require pull request reviews before merging
   - ✅ Require status checks to pass before merging
   - ✅ Require branches to be up to date before merging

### Adicionar Ambiente de Produção com Aprovação Manual

1. Vá em **Settings** → **Environments**
2. Clique em **New environment**
3. Nome: `production`
4. Marque **Required reviewers** e adicione revisores
5. Salve

O workflow de produção já está configurado para usar este ambiente.

## 🔍 Monitoramento

### Ver Logs da Aplicação no Azure

```bash
# Development - Logs em tempo real
az containerapp logs show \
  --name monitor-api-dev \
  --resource-group rg-monitor-dev \
  --follow

# Development - Últimos logs
az containerapp logs show \
  --name monitor-api-dev \
  --resource-group rg-monitor-dev \
  --tail 100

# Production
az containerapp logs show \
  --name monitor-api-prod \
  --resource-group rg-monitor-prod \
  --follow
```

### Verificar Status do Container App

```bash
# Development
az containerapp show \
  --name monitor-api-dev \
  --resource-group rg-monitor-dev \
  --query properties.runningStatus

# Production
az containerapp show \
  --name monitor-api-prod \
  --resource-group rg-monitor-prod \
  --query properties.runningStatus

# Ver todas as revisões (deployments)
az containerapp revision list \
  --name monitor-api-dev \
  --resource-group rg-monitor-dev \
  --output table
```

## 🐛 Troubleshooting

### Deploy falha na autenticação
- Verifique se o secret `AZURE_CREDENTIALS_DEV` ou `AZURE_CREDENTIALS_PROD` está correto
- Confirme que o Service Principal tem permissões de Contributor no Resource Group

### App não inicia após deploy
- Verifique os logs: `az webapp log tail --name SEU_APP_NAME --resource-group SEU_RG`
- Confirme que as variáveis de ambiente estão configuradas corretamente
- Verifique a connection string do banco de dados

### Build falha
- Verifique os logs do GitHub Actions
- Confirme que o projeto compila localmente: `dotnet build`
- Verifique se todas as dependências estão no .csproj

## 💰 Custos Estimados

### Development
- **Container App (0.5 vCPU, 1GB RAM, scale to zero)**: ~$5-15/mês
- **Container Registry (Basic)**: ~$5/mês
- **Banco de dados Neon (free tier)**: Grátis
- **Total estimado**: ~$10-20/mês

### Production
- **Container App (1 vCPU, 2GB RAM, min 1 replica)**: ~$30-50/mês
- **Container Registry (compartilhado)**: Incluído
- **Banco de dados**: Variável
- **Total estimado**: ~$30-60/mês

💡 **Vantagens do Container Apps**:
- Scale to zero em dev (economia quando não está em uso)
- Cobrança por segundo de uso
- Auto-scaling baseado em demanda

## 📚 Referências

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [GitHub Actions for Azure](https://github.com/Azure/actions)
- [.NET Deployment Guide](https://docs.microsoft.com/aspnet/core/host-and-deploy/azure-apps/)

## ✅ Checklist de Configuração

- [ ] Azure Container Registry (ACR) criado
- [ ] Container Apps Environment criado para dev
- [ ] Container App criado para dev no `rg-monitor-dev`
- [ ] Container Apps Environment criado para prod (quando necessário)
- [ ] Container App criado para prod no `rg-monitor-prod` (quando necessário)
- [ ] Service Principal criado para dev
- [ ] Service Principal criado para prod (quando necessário)
- [ ] Secret `AZURE_CREDENTIALS_DEV` adicionado no GitHub
- [ ] Secret `AZURE_CREDENTIALS_PROD` adicionado no GitHub (quando necessário)
- [ ] Secret `ACR_NAME` adicionado no GitHub
- [ ] Variáveis de ambiente configuradas no Container App
- [ ] Workflows atualizados com nomes e registry corretos
- [ ] Branch `develop` criada
- [ ] Branch `main` protegida
- [ ] Primeiro deploy testado com sucesso

---

**Dúvidas?** Consulte a documentação oficial ou abra uma issue no repositório.
