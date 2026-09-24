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

# Variables de entorno para producción
ENV ASPNETCORE_ENVIRONMENT=Production

# Render asigna PORT dinámicamente; la app lo lee en Program.cs
# Exponer puerto por defecto como documentación
EXPOSE 10000

# Shell form para que $PORT se expanda correctamente en runtime
ENTRYPOINT ["dotnet", "GRUPAL.dll"]
