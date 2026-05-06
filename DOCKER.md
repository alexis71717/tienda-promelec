# Dockerización de Tienda PromElec

Este documento describe **paso a paso** cómo construir, ejecutar y publicar la imagen Docker de la API.

---

## 1. Estructura

```
TiendaPromElec_Final/
├── Dockerfile              # Multi-stage: build → publish → runtime
├── .dockerignore           # Excluye bin/, obj/, tests/, etc.
├── docker-compose.yml      # API + SQL Server orquestados
└── .env.example            # Plantilla de variables de entorno
```

---

## 2. Pre-requisitos

- **Docker Desktop** (Windows/Mac) o **Docker Engine** (Linux) ≥ 20.10
- **Una cuenta en Docker Hub** ([https://hub.docker.com](https://hub.docker.com))
- **Migraciones generadas** (la primera vez):
  ```bash
  cd TiendaPromElec
  dotnet ef migrations add InitialCreate
  cd ..
  ```
  > Si no tienes la herramienta `dotnet-ef`, instálala con:
  > `dotnet tool install --global dotnet-ef --version 8.0.10`

---

## 3. Construir la imagen

Desde la raíz del proyecto (donde está el `Dockerfile`):

```bash
docker build -t <tu-usuario-dockerhub>/tiendapromelec-api:latest .
```

Ejemplo:
```bash
docker build -t juanperez/tiendapromelec-api:latest .
docker build -t juanperez/tiendapromelec-api:1.0 .   # versión fija también
```

> El build es multi-stage: la imagen final pesa solo la base `aspnet:8.0` (~210 MB) y **no incluye el SDK** ni el código fuente.

---

## 4. Probar la imagen localmente (sin compose)

```bash
docker run -d \
  --name promelec-api \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Server=tcp:host.docker.internal,1433;Database=PromElec;User Id=sa;Password=YourPwd#1;TrustServerCertificate=True" \
  -e Jwt__Secret="$(openssl rand -base64 48)" \
  -e Seed__AdminEmail="admin@promelec.com" \
  -e Seed__AdminPassword="Admin#2025!" \
  <tu-usuario>/tiendapromelec-api:latest
```

Para verificar:
```bash
curl http://localhost:8080/api/Product
docker logs -f promelec-api
```

---

## 5. Levantar todo con docker-compose (recomendado)

`docker-compose.yml` ya orquesta SQL Server + API.

**1)** Copia `.env.example` a `.env` y rellena los valores:
```bash
cp .env.example .env
nano .env   # o vi/code
```

**2)** Levanta el stack:
```bash
docker-compose up -d --build
```

**3)** Verifica:
```bash
docker-compose ps
docker-compose logs -f api
curl http://localhost:8080/api/Product
```

**4)** Para detenerlo:
```bash
docker-compose down              # mantiene volúmenes
docker-compose down -v           # limpia también la BD
```

---

## 6. Publicar la imagen en Docker Hub

```bash
# 1) Login
docker login

# 2) (Opcional) Re-etiqueta si la creaste con otro nombre
docker tag tiendapromelec-api:latest <tu-usuario>/tiendapromelec-api:latest

# 3) Push
docker push <tu-usuario>/tiendapromelec-api:latest
docker push <tu-usuario>/tiendapromelec-api:1.0
```

Verificar que la imagen está pública:
```
https://hub.docker.com/r/<tu-usuario>/tiendapromelec-api
```

---

## 7. Detalles del Dockerfile

| Stage | Imagen base | Propósito |
|-------|-------------|-----------|
| `build` | `mcr.microsoft.com/dotnet/sdk:8.0` | Restaurar paquetes y compilar |
| `publish` | (continúa en build) | `dotnet publish -c Release` |
| `final` | `mcr.microsoft.com/dotnet/aspnet:8.0` | Runtime ligero, **usuario no-root (uid 1001)** |

**Buenas prácticas aplicadas:**

- ✅ Multi-stage para imagen final pequeña.
- ✅ Usuario no-root (`appuser:appgroup`).
- ✅ `HEALTHCHECK` integrado.
- ✅ `EXPOSE 8080` (puerto único; HTTPS termina en el reverse proxy).
- ✅ Variables sensibles inyectadas como env vars (no embebidas).
- ✅ Capa de restore separada de la capa de copia para aprovechar la caché.

---

## 8. Despliegue en servidor Linux (extra)

```bash
# En tu servidor
sudo apt-get install docker.io docker-compose
git clone https://github.com/<usuario>/tienda-promelec.git
cd tienda-promelec
cp .env.example .env && nano .env
docker-compose up -d --build
```

Para exponerlo al exterior **detrás de Nginx con HTTPS gestionado por Let’s Encrypt**:

```nginx
server {
    listen 443 ssl http2;
    server_name api.tudominio.com;

    ssl_certificate     /etc/letsencrypt/live/api.tudominio.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/api.tudominio.com/privkey.pem;

    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto https;
    }
}
```

---

## 9. Solución de problemas

| Síntoma | Causa probable | Solución |
|---------|----------------|----------|
| `Unable to connect to any of the specified MySQL hosts` | Connection string apunta a `localhost` dentro del contenedor | Usa el nombre del servicio (`sqlserver`) o `host.docker.internal` |
| `Jwt:Secret no configurado` al arrancar | Falta env var `Jwt__Secret` | Define `JWT_SECRET` en `.env` o pásalo con `-e` |
| `IDX10720: Unable to create KeyedHashAlgorithm` | Secret < 32 caracteres | Usa al menos 32 caracteres |
| 502 desde Nginx | El contenedor se está reiniciando | `docker-compose logs -f api` |
| SQL Server no arranca | Password no cumple política (8+ caracteres con números, mayúscula, minúscula, símbolo) | Cambia `SA_PASSWORD` en `.env` |
