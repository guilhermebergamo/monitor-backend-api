# Dockerfile multi-stage para otimização de tamanho e performance
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os arquivos de projeto primeiro - NOVA ESTRUTURA COM PASTAS NUMERADAS
COPY ["src/03-Domain/MonitorBackend.Domain/MonitorBackend.Domain.csproj", "03-Domain/MonitorBackend.Domain/"]
COPY ["src/02-Application/MonitorBackend.Application/MonitorBackend.Application.csproj", "02-Application/MonitorBackend.Application/"]
COPY ["src/04-Infrastructure/MonitorBackend.Infrastructure/MonitorBackend.Infrastructure.csproj", "04-Infrastructure/MonitorBackend.Infrastructure/"]
COPY ["src/01-API/MonitorBackend.Api/MonitorBackend.Api.csproj", "01-API/MonitorBackend.Api/"]

# Restaura as dependências (cache separado)
RUN dotnet restore "01-API/MonitorBackend.Api/MonitorBackend.Api.csproj"

# Copia o código fonte
COPY src/ .

# Build da aplicação em modo Release
RUN dotnet build "01-API/MonitorBackend.Api/MonitorBackend.Api.csproj" -c Release -o /app/build/api

# Publica a aplicação
RUN dotnet publish "01-API/MonitorBackend.Api/MonitorBackend.Api.csproj" -c Release -o /app/publish/api --no-restore

# Stage 2: Runtime - API
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS api-runtime
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copia os binários publicados
COPY --from=build /app/publish/api .

# Configurações de ambiente
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Usuário não-root para segurança
USER $APP_UID

ENTRYPOINT ["dotnet", "MonitorBackend.Api.dll"]


