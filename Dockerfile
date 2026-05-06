# =====================================================================
# DOCKERFILE MULTI-STAGE PARA TiendaPromElec API
# - Stage 1: build (compila con SDK)
# - Stage 2: publish (publica binarios optimizados)
# - Stage 3: runtime (imagen mínima aspnet, usuario no-root)
# =====================================================================

# ---------- STAGE 1: BUILD ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia solo el csproj primero para aprovechar la caché de Docker
COPY ["TiendaPromElec/TiendaPromElec.csproj", "TiendaPromElec/"]
RUN dotnet restore "TiendaPromElec/TiendaPromElec.csproj"

# Copia el resto del código y compila
COPY TiendaPromElec/ TiendaPromElec/
WORKDIR /src/TiendaPromElec
RUN dotnet build "TiendaPromElec.csproj" -c Release -o /app/build --no-restore

# ---------- STAGE 2: PUBLISH ----------
FROM build AS publish
RUN dotnet publish "TiendaPromElec.csproj" -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- STAGE 3: RUNTIME ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Crea un usuario no-root para mejorar la seguridad del contenedor
RUN groupadd --system --gid 1001 appgroup \
 && useradd  --system --uid 1001 --gid appgroup --no-create-home appuser

# Copia los binarios publicados desde el stage anterior
COPY --from=publish /app/publish .

# Permisos para el usuario no-root
RUN chown -R appuser:appgroup /app
USER appuser

# Variables de entorno por defecto (se sobreescriben en docker run / compose)
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=true

EXPOSE 8080

# Healthcheck básico (golpea Swagger o cualquier endpoint público)
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD wget --quiet --tries=1 --spider http://localhost:8080/api/Product || exit 1

ENTRYPOINT ["dotnet", "TiendaPromElec.dll"]
