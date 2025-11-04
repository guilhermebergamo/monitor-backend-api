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

Você já tem o Resource Group de dev. Agora precisa:

```bash
# 1. Login no Azure
az login

# 2. Listar seus resource groups para confirmar o nome
az group list --output table

# 3. Criar o App Service para dev (ajuste os nomes)
RESOURCE_GROUP="seu-resource-group-dev"  # ⚠️ AJUSTAR
APP_NAME="monitor-api-dev"                # ⚠️ AJUSTAR

az appservice plan create \
  --name "${APP_NAME}-plan" \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan "${APP_NAME}-plan" \
  --runtime "DOTNET:8.0"

# 4. Criar Service Principal para GitHub Actions
az ad sp create-for-rbac \
  --name "github-actions-monitor-dev" \
  --role contributor \
  --scopes /subscriptions/{SUBSCRIPTION_ID}/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth

# ⚠️ COPIE TODO O JSON RETORNADO!
```

### 4. Configurar Secrets no GitHub

1. Acesse: `https://github.com/SEU_USUARIO/SEU_REPOSITORIO/settings/secrets/actions`
2. Clique em **New repository secret**
3. Nome: `AZURE_CREDENTIALS_DEV`
4. Valor: Cole o JSON do Service Principal
5. Salve

### 5. Atualizar o Workflow de Dev

Edite `.github/workflows/deploy-dev.yml`:

```yaml
env:
  AZURE_WEBAPP_NAME: 'monitor-api-dev'  # ⚠️ Seu App Name real
  RESOURCE_GROUP: 'seu-resource-group-dev'  # ⚠️ Seu Resource Group real
```

### 6. Fazer Push e Testar

```bash
git add .
git commit -m "feat: atualizar configuração do Azure"
git push origin develop
```

Acompanhe o deploy em: `https://github.com/SEU_USUARIO/SEU_REPOSITORIO/actions`

### 7. Configurar Variáveis de Ambiente no Azure

```bash
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ASPNETCORE_ENVIRONMENT="Development" \
    ConnectionStrings__DefaultConnection="Host=ep-dark-dream-a820yymb-pooler.eastus2.azure.neon.tech;Database=MonitordbDevelop;Username=neondb_owner;Password=npg_Er10paDuIsmd;SSL Mode=Require;Trust Server Certificate=true" \
    FrontendUrl="http://localhost:5173"
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
