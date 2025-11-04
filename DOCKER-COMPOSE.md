# Docker Compose - Guia de Uso

Este guia explica como usar o Docker Compose para executar o projeto localmente.

## 🚀 Início Rápido

```bash
# Iniciar todos os serviços
docker-compose up -d

# Ver logs
docker-compose logs -f

# Parar todos os serviços
docker-compose down

# Parar e remover volumes (limpa o banco)
docker-compose down -v
```

## 📦 Serviços Incluídos

### 1. PostgreSQL (postgres)
- **Porta**: 5432
- **Database**: monitordb
- **Username**: postgres
- **Password**: postgres
- **Inicialização**: Executa automaticamente `database-setup.sql`

### 2. API (api)
- **Porta**: 8080
- **URL**: http://localhost:8080
- **Swagger**: http://localhost:8080
- **Health Check**: http://localhost:8080/
- **Registros**: http://localhost:8080/api/registros

## 🔧 Comandos Úteis

### Ver logs de um serviço específico
```bash
docker-compose logs -f api
docker-compose logs -f postgres
```

### Rebuild das imagens
```bash
docker-compose build
docker-compose up -d
```

### Entrar no container
```bash
# API
docker exec -it monitor-api /bin/sh

# PostgreSQL
docker exec -it monitor-postgres psql -U postgres -d monitordb
```

### Verificar status
```bash
docker-compose ps
```

### Reiniciar um serviço
```bash
docker-compose restart api
```

## 🧪 Testar a API

### Com cURL
```bash
# Health check
curl http://localhost:8080/

# Registros
curl http://localhost:8080/api/registros
```

### Com PowerShell
```powershell
Invoke-RestMethod -Uri "http://localhost:8080/api/registros" -Method Get | ConvertTo-Json
```

### Swagger UI
Abra no navegador: http://localhost:8080

## 🗄️ Acessar o PostgreSQL

### Usando psql
```bash
docker exec -it monitor-postgres psql -U postgres -d monitordb

# Comandos SQL
\dt                           # Listar tabelas
SELECT * FROM registros;      # Ver dados
\q                            # Sair
```

### Connection String (para DBeaver, pgAdmin, etc.)
```
Host: localhost
Port: 5432
Database: monitordb
Username: postgres
Password: postgres
```

## 🔄 Atualizar Código

Quando fizer alterações no código:

```bash
# Rebuild e restart
docker-compose build
docker-compose up -d

# Ou rebuild específico
docker-compose build api
docker-compose restart api
```

## 🧹 Limpeza Completa

```bash
# Para tudo e remove volumes
docker-compose down -v

# Remove imagens também
docker-compose down --rmi all -v
```

## ⚙️ Customizar Configurações

Edite o `docker-compose.yml` para alterar:

### Porta da API
```yaml
ports:
  - "5000:8080"  # Acesse em localhost:5000
```

### Connection String
```yaml
environment:
  - ConnectionStrings__DefaultConnection=Host=postgres;Database=customdb;...
```

## 🐛 Troubleshooting

### API não inicia
```bash
# Ver logs detalhados
docker-compose logs api

# Verificar se o banco está rodando
docker-compose ps postgres
```

### Porta já em uso
```bash
# Alterar a porta no docker-compose.yml
ports:
  - "9090:8080"  # Usar porta 9090 no host
```

### Banco de dados vazio
```bash
# Executar script SQL manualmente
docker exec -i monitor-postgres psql -U postgres -d monitordb < database-setup.sql
```

## 📊 Monitorar Performance

```bash
# Ver uso de recursos
docker stats

# Ver apenas os containers do projeto
docker stats monitor-api monitor-postgres
```

## 🎯 Apenas PostgreSQL

Se quiser rodar apenas o banco:

```bash
docker-compose up -d postgres
```

Depois execute a API localmente:

```bash
# Terminal - API
cd src/01-API/MonitorBackend.Api
dotnet run
```

---

**Nota**: O Docker Compose é ideal para desenvolvimento local. Para produção, use Azure Container Apps conforme o guia DEPLOY.md.
