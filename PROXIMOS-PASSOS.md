# 🚀 Próximos Passos - Setup Completo

## ✅ O que já está pronto

- ✅ Repositório Git inicializado na branch `develop`
- ✅ GitHub Actions configurado para deploy automático
- ✅ Workflows criados para dev e produção
- ✅ Documentação completa de configuração

## 📋 Checklist de Ações Necessárias

### 1. Criar Repositório no GitHub

```bash
# No GitHub, crie um novo repositório (pode ser privado)
# Depois execute:

git remote add origin https://github.com/SEU_USUARIO/SEU_REPOSITORIO.git
git push -u origin develop
```

### 2. Criar Branch Main

```bash
git checkout -b main
git push -u origin main
git checkout develop
```

### 3. Configurar Azure - Development

Você já tem o Resource Group `rg-monitor-dev` e o Container Registry `monitordevregistry`. Agora precisa:

```bash
# 1. Login no Azure
az login

# 2. Verificar o Container Registry (já existe)
ACR_NAME="monitordevregistry"
RESOURCE_GROUP="rg-monitor-dev"

# Verificar se o admin está habilitado
az acr update --name $ACR_NAME --admin-enabled true

# Anotar credenciais do ACR (para configurar secrets)
az acr credential show --name $ACR_NAME

# 3. Verificar Container Apps Environment (você já tem um, pois tem worker rodando)
# Liste os environments existentes
az containerapp env list --resource-group $RESOURCE_GROUP --output table

# Anote o nome do environment (algo como monitor-env-dev ou similar)
ENVIRONMENT_NAME="SEU_ENVIRONMENT_NAME"  # ⚠️ Ajuste com o nome real

# 4. Criar Container App para a API
az containerapp create \
  --name monitor-api-dev \
  --resource-group $RESOURCE_GROUP \
  --environment $ENVIRONMENT_NAME \
  --image mcr.microsoft.com/azuredocs/containerapps-helloworld:latest \
  --target-port 8080 \
  --ingress external \
  --registry-server "monitordevregistry.azurecr.io" \
  --cpu 0.5 \
  --memory 1.0Gi \
  --min-replicas 0 \
  --max-replicas 3

# 5. Criar Service Principal para GitHub Actions
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

az ad sp create-for-rbac \
  --name "github-actions-monitor-dev" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth

# ⚠️ COPIE TODO O JSON RETORNADO!
```

### 4. Configurar Secrets no GitHub

1. Acesse: `https://github.com/SEU_USUARIO/SEU_REPOSITORIO/settings/secrets/actions`
2. Adicione os seguintes secrets:

**AZURE_CREDENTIALS_DEV**:
- Valor: Cole o JSON do Service Principal

**ACR_NAME**:
- Valor: `monitordevregistry`

3. Salve todos os secrets

### 5. Verificar o Workflow de Dev

O workflow `.github/workflows/deploy-dev.yml` já está configurado com:

```yaml
env:
  AZURE_CONTAINER_APP_NAME: 'monitor-api-dev'  # ✅ Nome do Container App
  RESOURCE_GROUP: 'rg-monitor-dev'  # ✅ Resource Group
  CONTAINER_REGISTRY: 'monitordevregistry.azurecr.io'  # ✅ ACR
```

**Nada precisa ser alterado aqui!** ✅

### 6. Fazer Push e Testar

```bash
git add .
git commit -m "feat: atualizar configuração do Azure"
git push origin develop
```

Acompanhe o deploy em: `https://github.com/SEU_USUARIO/SEU_REPOSITORIO/actions`

### 7. Configurar Variáveis de Ambiente no Container App

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

## 📚 Documentação Completa

Para instruções detalhadas, consulte: **`GITHUB-ACTIONS-SETUP.md`**

## 🔗 Links Úteis

- **Documentação dos Workflows**: `.github/workflows/`
- **Setup Completo do Azure e GitHub**: `GITHUB-ACTIONS-SETUP.md`
- **Comandos Úteis**: `COMMANDS.md`
- **Arquitetura do Projeto**: `ARCHITECTURE.md`

## ⚠️ Importante

- Nunca commite credenciais no código
- Use sempre secrets do GitHub para informações sensíveis
- As connection strings devem ser configuradas no Azure, não no código
- Configure proteção de branch para `main` após criar o repositório

## 🆘 Precisa de Ajuda?

Se tiver dúvidas durante o setup:
1. Verifique os logs do GitHub Actions
2. Consulte o `GITHUB-ACTIONS-SETUP.md`
3. Verifique se todos os secrets estão configurados corretamente
