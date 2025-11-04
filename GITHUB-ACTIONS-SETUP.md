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

### 1. Criar Azure App Service (ou Container App)

#### Para Development:
```bash
# Login no Azure
az login

# Definir variáveis
RESOURCE_GROUP="seu-resource-group-dev"
APP_NAME="monitor-api-dev"
LOCATION="eastus"

# Criar App Service Plan (Linux)
az appservice plan create \
  --name "${APP_NAME}-plan" \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

# Criar Web App
az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan "${APP_NAME}-plan" \
  --runtime "DOTNET:8.0"

# Configurar variáveis de ambiente
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ASPNETCORE_ENVIRONMENT="Development" \
    ConnectionStrings__DefaultConnection="Host=SEU_HOST;Database=SEU_DB;Username=SEU_USER;Password=SUA_SENHA;SSL Mode=Require;Trust Server Certificate=true" \
    FrontendUrl="https://seu-frontend-dev.azurestaticapps.net"
```

#### Para Production:
```bash
# Definir variáveis
RESOURCE_GROUP="seu-resource-group-prod"
APP_NAME="monitor-api-prod"
LOCATION="eastus"

# Criar App Service Plan (Linux)
az appservice plan create \
  --name "${APP_NAME}-plan" \
  --resource-group $RESOURCE_GROUP \
  --sku P1v2 \
  --is-linux

# Criar Web App
az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan "${APP_NAME}-plan" \
  --runtime "DOTNET:8.0"

# Configurar variáveis de ambiente
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    ConnectionStrings__DefaultConnection="Host=SEU_HOST_PROD;Database=SEU_DB_PROD;Username=SEU_USER;Password=SUA_SENHA;SSL Mode=Require;Trust Server Certificate=true" \
    FrontendUrl="https://seu-frontend-prod.azurestaticapps.net"
```

### 2. Criar Service Principal para GitHub Actions

O Service Principal permite que o GitHub Actions se autentique no Azure.

#### Para Development:
```bash
# Criar Service Principal
az ad sp create-for-rbac \
  --name "github-actions-monitor-dev" \
  --role contributor \
  --scopes /subscriptions/{SUBSCRIPTION_ID}/resourceGroups/{RESOURCE_GROUP_DEV} \
  --sdk-auth

# ⚠️ IMPORTANTE: Copie TODO o JSON retornado. Será usado no GitHub!
```

#### Para Production:
```bash
# Criar Service Principal
az ad sp create-for-rbac \
  --name "github-actions-monitor-prod" \
  --role contributor \
  --scopes /subscriptions/{SUBSCRIPTION_ID}/resourceGroups/{RESOURCE_GROUP_PROD} \
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

#### Para Development:
- **Nome**: `AZURE_CREDENTIALS_DEV`
- **Valor**: Cole o JSON completo do Service Principal criado para dev

#### Para Production:
- **Nome**: `AZURE_CREDENTIALS_PROD`
- **Valor**: Cole o JSON completo do Service Principal criado para prod

## 📝 Atualizar os Workflows

Edite os arquivos de workflow e atualize as variáveis:

### `.github/workflows/deploy-dev.yml`
```yaml
env:
  AZURE_WEBAPP_NAME: 'monitor-api-dev' # ⚠️ Seu App Name
  RESOURCE_GROUP: 'seu-resource-group-dev' # ⚠️ Seu Resource Group
```

### `.github/workflows/deploy-prod.yml`
```yaml
env:
  AZURE_WEBAPP_NAME: 'monitor-api-prod' # ⚠️ Seu App Name
  RESOURCE_GROUP: 'seu-resource-group-prod' # ⚠️ Seu Resource Group
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

Após o deploy, acesse:
- **Dev**: `https://monitor-api-dev.azurewebsites.net`
- **Prod**: `https://monitor-api-prod.azurewebsites.net`

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
# Development
az webapp log tail --name monitor-api-dev --resource-group seu-resource-group-dev

# Production
az webapp log tail --name monitor-api-prod --resource-group seu-resource-group-prod
```

### Verificar Status do App

```bash
# Development
az webapp show --name monitor-api-dev --resource-group seu-resource-group-dev --query state

# Production
az webapp show --name monitor-api-prod --resource-group seu-resource-group-prod --query state
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

### Development (B1 - Basic)
- **App Service B1**: ~$13/mês
- **Banco de dados Neon (free tier)**: Grátis

### Production (P1v2 - Premium)
- **App Service P1v2**: ~$74/mês
- **Banco de dados**: Variável

## 📚 Referências

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [GitHub Actions for Azure](https://github.com/Azure/actions)
- [.NET Deployment Guide](https://docs.microsoft.com/aspnet/core/host-and-deploy/azure-apps/)

## ✅ Checklist de Configuração

- [ ] Azure App Service criado para dev
- [ ] Azure App Service criado para prod (quando necessário)
- [ ] Service Principal criado para dev
- [ ] Service Principal criado para prod (quando necessário)
- [ ] Secret `AZURE_CREDENTIALS_DEV` adicionado no GitHub
- [ ] Secret `AZURE_CREDENTIALS_PROD` adicionado no GitHub (quando necessário)
- [ ] Variáveis de ambiente configuradas no Azure
- [ ] Workflows atualizados com nomes corretos
- [ ] Branch `develop` criada
- [ ] Branch `main` protegida
- [ ] Primeiro deploy testado com sucesso

---

**Dúvidas?** Consulte a documentação oficial ou abra uma issue no repositório.
