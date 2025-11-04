# Guia de Configuração do GitHub Actions

Este documento explica como configurar o GitHub Actions para deploy automático no Azure Container Apps.

## Secrets Necessários

Configure os seguintes secrets no repositório GitHub (Settings → Secrets and variables → Actions):

### 1. AZURE_CREDENTIALS

Crie um Service Principal no Azure:

```bash
az ad sp create-for-rbac \
  --name "github-actions-monitor-backend" \
  --role contributor \
  --scopes /subscriptions/SEU_SUBSCRIPTION_ID/resourceGroups/monitor-backend-rg \
  --sdk-auth
```

Copie o JSON retornado e adicione como secret `AZURE_CREDENTIALS`.

### 2. ACR_USERNAME

```bash
az acr credential show --name monitorbackendacr --query username -o tsv
```

### 3. ACR_PASSWORD

```bash
az acr credential show --name monitorbackendacr --query "passwords[0].value" -o tsv
```

## Variáveis de Ambiente

Edite o arquivo `.github/workflows/deploy.yml` e atualize:

```yaml
env:
  AZURE_CONTAINER_REGISTRY: seu-registry-name
  AZURE_RESOURCE_GROUP: seu-resource-group
  API_CONTAINER_APP_NAME: monitor-api
  WORKER_CONTAINER_APP_NAME: monitor-worker
```

## Trigger do Workflow

O workflow é executado quando:
- Push para branch `main`
- Pull request para branch `main`
- Manualmente via GitHub UI (workflow_dispatch)

## Como Usar

1. Configure os secrets no GitHub
2. Faça push para a branch `main`
3. O workflow será executado automaticamente
4. Acompanhe o progresso na aba "Actions" do GitHub

## Troubleshooting

### Erro de autenticação no ACR
- Verifique se os secrets ACR_USERNAME e ACR_PASSWORD estão corretos
- Confirme que o admin está habilitado no ACR: `az acr update -n monitorbackendacr --admin-enabled true`

### Erro no deploy
- Verifique se o Service Principal tem permissões no Resource Group
- Confirme que os nomes dos Container Apps estão corretos

### Build falhou
- Verifique se todos os projetos compilam localmente
- Confirme que o .NET 8 SDK está configurado corretamente
