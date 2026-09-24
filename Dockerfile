# ============================================================
# Dockerfile – AlToke (.NET 10 MVC)
# Multi-stage build optimizado para Render
# ============================================================

# ── Etapa 1: Build ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copiar solo el .csproj primero para aprovechar la caché de capas
COPY GRUPAL.csproj ./
RUN dotnet restore

# Copiar el resto del código y compilar en Release
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Etapa 2: Runtime ────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Crear directorio persistente para SQLite
RUN mkdir -p /app/data

# Copiar los archivos publicados
COPY --from=build /app/publish .

# Render asigna el puerto vía variable de entorno PORT
ENV ASPNETCORE_URLS=http://+:${PORT:-10000}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

ENTRYPOINT ["dotnet", "GRUPAL.dll"]
