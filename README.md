# Tienda PromElec — Reto Escudo

> **SC1.3 — Back-End Avanzado con .NET Core 8.0 LTS**
> Reto: Seguridad, Pruebas y Despliegue Inteligente

API REST en **.NET 8** con autenticación JWT, autorización basada en roles, validación de inputs, base de datos SQL Server vía EF Core y despliegue dockerizado.

---

## 📋 Tabla de contenidos

1. [Arquitectura](#1-arquitectura)
2. [Requisitos previos](#2-requisitos-previos)
3. [Setup paso a paso](#3-setup-paso-a-paso)
4. [Cómo correr la API](#4-cómo-correr-la-api)
5. [Pruebas](#5-pruebas)
6. [Documentación adicional](#6-documentación-adicional)
7. [Cumplimiento de la rúbrica](#7-cumplimiento-de-la-rúbrica)

---

## 1. Arquitectura

```
TiendaPromElec_Final/
├── TiendaPromElec/                      → Web API principal
│   ├── Controllers/
│   │   ├── AuthController.cs            → /api/auth/register, /login
│   │   ├── ProductController.cs         → CRUD productos (Admin para escrituras)
│   │   └── OrderController.cs           → CRUD pedidos (auth global)
│   ├── Data/AppDbContext.cs             → IdentityDbContext + entidades + seed
│   ├── DTOs/                            → DTOs para auth y productos
│   ├── Models/
│   │   ├── ApplicationUser.cs           → IdentityUser extendido
│   │   ├── Product.cs / Category.cs / Customer.cs / Order.cs / OrderDetail.cs
│   ├── Services/
│   │   ├── ITokenService / TokenService → JWT firmado HMAC-SHA256
│   │   ├── IAuthService / AuthService   → Registro / login con Identity
│   │   ├── IProductService / ProductService
│   │   └── IOrderService / OrderService
│   ├── Properties/launchSettings.json
│   ├── appsettings.json                 → SIN secretos
│   ├── appsettings.Example.json         → Plantilla con valores de ejemplo
│   ├── Program.cs                       → JWT + CORS + HSTS + Identity + cabeceras
│   └── TiendaPromElec.csproj
│
├── TiendaPromElec.UnitTests/            → 10 pruebas (5+/5−) con xUnit + Moq
│   ├── Services/
│   │   ├── ProductServiceTests.cs       → 4+/3−
│   │   └── TokenServiceTests.cs         → 0+/1−
│   └── Controllers/AuthControllerTests.cs → 1+/1−
│
├── TiendaPromElec.IntegrationTests/     → 20 pruebas (10 funcionalidad + 10 seguridad)
│   ├── Helpers/
│   │   ├── CustomWebApplicationFactory.cs → SQLite InMemory + Identity seed
│   │   └── AuthHelper.cs                  → Login + tokens
│   ├── Functionality/ProductFunctionalityTests.cs   → 5+/5−
│   └── Security/SecurityTests.cs                    → 5+/5−
│
├── Dockerfile                           → Multi-stage (sdk → aspnet)
├── .dockerignore
├── docker-compose.yml                   → API + SQL Server
├── .env.example                         → Plantilla de variables
├── SECURITY_ANALYSIS.md                 → Mapeo OWASP Top 10 + mitigaciones
├── DOCKER.md                            → Guía de Docker
└── README.md                            → Este archivo
```

---

## 2. Requisitos previos

| Herramienta | Versión | Notas |
|-------------|---------|-------|
| .NET SDK | **8.0** LTS | `dotnet --version` |
| SQL Server | 2019+ | Express, LocalDB o instancia completa |
| Docker | 20.10+ | (Solo para sección Docker) |
| `dotnet-ef` | 8.0.10 | `dotnet tool install --global dotnet-ef --version 8.0.10` |

---

## 3. Setup paso a paso

### 3.1 Restaurar paquetes NuGet

Desde la raíz del proyecto:

```bash
dotnet restore
```

### 3.2 Configurar User Secrets (desarrollo)

> **Importante:** El `appsettings.json` **NO contiene** ningún secreto. Debes configurar:
> - La cadena de conexión
> - El secreto JWT
> - La contraseña del admin por defecto

```bash
cd TiendaPromElec

# Connection string (ajusta a tu instancia local)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=.\\SQLEXPRESS;Database=PromElec;Trusted_Connection=True;TrustServerCertificate=True"

# JWT Secret de al menos 32 caracteres aleatorios
# En Linux/Mac:
dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 48)"
# En PowerShell:
# dotnet user-secrets set "Jwt:Secret" (-join ((48..57)+(65..90)+(97..122) | Get-Random -Count 48 | %{[char]$_}))

# Credenciales del admin (se siembran en el primer arranque)
dotnet user-secrets set "Seed:AdminEmail" "admin@promelec.com"
dotnet user-secrets set "Seed:AdminPassword" "Admin#2025!"

cd ..
```

Verificar:
```bash
cd TiendaPromElec && dotnet user-secrets list
```

### 3.3 Generar la migración inicial

> Como el esquema cambió (se añadieron tablas de Identity), **debes generar una migración nueva**.

```bash
cd TiendaPromElec
dotnet ef migrations add InitialCreate
cd ..
```

Esto crea la carpeta `Migrations/`. La primera vez que arranque la API, las migraciones se aplicarán automáticamente (`db.Database.Migrate()` en `Program.cs`).

---

## 4. Cómo correr la API

```bash
cd TiendaPromElec
dotnet run
```

Por defecto la API levanta en:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- **Swagger UI:** `https://localhost:5001/swagger`

### 4.1 Probar el flujo de autenticación

```bash
# 1) Login como admin (creado automáticamente por el seed)
curl -k -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@promelec.com","password":"Admin#2025!"}'

# Copia el token de la respuesta

# 2) Usar el token para crear un producto
curl -k -X POST https://localhost:5001/api/Product \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TU-TOKEN>" \
  -d '{
    "name":"Demo",
    "description":"Producto de prueba",
    "brand":"DemoBrand",
    "price":99.99,
    "stock":5,
    "categoryId":1
  }'
```

---

## 5. Pruebas

### 5.1 Pruebas unitarias (10)

```bash
dotnet test TiendaPromElec.UnitTests
```

Estructura:

| Suite | Positivas | Negativas |
|-------|-----------|-----------|
| `ProductServiceTests` | 4 | 3 |
| `TokenServiceTests` | 0 | 1 |
| `AuthControllerTests` | 1 | 1 |
| **TOTAL** | **5** | **5** |

Tecnologías:
- **xUnit** como framework de tests
- **Moq** para `ILogger<T>` y `IAuthService`
- **EF Core InMemory** para `AppDbContext`
- **In-memory `IConfiguration`** para `TokenService`

### 5.2 Pruebas de integración (20)

```bash
dotnet test TiendaPromElec.IntegrationTests
```

Estructura:

| Suite | Positivas | Negativas |
|-------|-----------|-----------|
| `ProductFunctionalityTests` (funcionalidad) | 5 | 5 |
| `SecurityTests` (seguridad) | 5 | 5 |
| **TOTAL** | **10** | **10** |

Tecnologías:
- **`WebApplicationFactory<Program>`** para levantar la API in-process
- **SQLite InMemory** con conexión persistente compartida (más realista que EF InMemory; **soporta transacciones**)
- Login real contra `/api/auth/login` para obtener tokens JWT en cada test

### 5.3 Correr todo a la vez

```bash
dotnet test
```

---

## 6. Documentación adicional

- 📄 [`SECURITY_ANALYSIS.md`](SECURITY_ANALYSIS.md) — Análisis de seguridad mapeado a OWASP Top 10
- 📄 [`DOCKER.md`](DOCKER.md) — Guía de dockerización + publicación + nginx

---

## 7. Cumplimiento de la rúbrica

| Sección | Puntos | Implementación |
|---------|--------|----------------|
| **1. Análisis de seguridad e implementación de mejoras** | 25 | `SECURITY_ANALYSIS.md` mapea 7 vulnerabilidades OWASP. Se implementaron políticas de autorización (`AdminOnly`), CORS restrictivo, HSTS, HTTPS forzado, secretos en User Secrets/env vars, validación con DataAnnotations, hashing PBKDF2, lockout, cabeceras de seguridad y logging de eventos. |
| **2. Pruebas unitarias** | 25 | 10 tests (5+/5−) en `TiendaPromElec.UnitTests/` con xUnit y **Moq** (mock de `ILogger`, `IAuthService`, `IConfiguration`). Cobertura de servicios (`ProductService`, `TokenService`) y controlador (`AuthController`). |
| **3. Pruebas de integración** | 25 | 20 tests con `WebApplicationFactory<Program>` y **SQLite InMemory**. 10 funcionalidad (5+/5−) verificando registro, login, CRUD de productos. 10 seguridad (5+/5−) verificando autenticación, autorización por roles, cabeceras y rechazo de tokens malformados. |
| **4. Dockerización** | 25 | `Dockerfile` multi-stage (build → publish → runtime), usuario no-root, healthcheck, `.dockerignore`, `docker-compose.yml` con SQL Server + variables de entorno, `.env.example`, instrucciones de push a Docker Hub en `DOCKER.md`. |
| **TOTAL** | **100** | |

### Puntos extra disponibles

- **Despliegue en Linux + Nginx + balanceo** documentado en `DOCKER.md` sección 8.

---

## ⚠️ Notas importantes

1. **Generar migraciones es obligatorio** la primera vez (se eliminaron las migraciones del proyecto original porque el esquema cambió al añadir Identity):
   ```bash
   cd TiendaPromElec && dotnet ef migrations add InitialCreate
   ```

2. **No subas tus secrets al repositorio.** Usa `.gitignore` para excluir `.env` y NUNCA pongas credenciales en `appsettings.json`.

3. **Imagen de Docker Hub:** reemplaza `<tu-usuario>` por tu username real al construir y publicar la imagen.

4. **El admin por defecto** (`admin@promelec.com`) se crea automáticamente con la contraseña que configures en `Seed:AdminPassword` (User Secret o env var).
