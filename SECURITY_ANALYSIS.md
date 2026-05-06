# Análisis de Seguridad — Tienda PromElec

**Proyecto:** Reto Escudo PromElec
**Curso:** SC1.3 Back-End Avanzado con .NET Core 8.0 LTS
**Tecnologías:** ASP.NET Core 8 · ASP.NET Core Identity · JWT Bearer · EF Core 8

---

## 1. Resumen ejecutivo

El código original de Tienda PromElec era una API funcional pero presentaba **siete vulnerabilidades críticas** alineadas con el **OWASP Top 10 (edición 2021)**. Este documento mapea cada vulnerabilidad detectada con su mitigación implementada en la versión final.

| # | OWASP | Riesgo identificado en el código original | Mitigación aplicada |
|---|------|--------------------------------------------|----------------------|
| 1 | A01 — Broken Access Control | Todos los endpoints eran anónimos; cualquiera podía crear/borrar productos y pedidos | JWT + `[Authorize]` + políticas `AdminOnly` y `AuthenticatedUser` |
| 2 | A02 — Cryptographic Failures | Connection string en texto plano dentro de `appsettings.json` | `appsettings.json` sin secretos. Se usan **User Secrets** (dev) y **variables de entorno** (prod) |
| 3 | A03 — Injection | Ya se usaba EF Core (parametrizado). Faltaba validación de input | `DataAnnotations` en modelos y DTOs (Required, StringLength, Range, EmailAddress, Phone, Url) |
| 4 | A05 — Security Misconfiguration | Sin HTTPS forzado, sin HSTS, CORS no configurado, cabeceras de seguridad ausentes | HSTS 365 días + Redirect 308 + CORS con `WithOrigins` + middleware de cabeceras |
| 5 | A07 — Identification & Authentication Failures | Sin sistema de usuarios, sin hashing, sin políticas de password | ASP.NET Core Identity con PBKDF2 + políticas estrictas + lockout |
| 6 | A08 — Software & Data Integrity Failures | Versiones de paquetes flotantes (`*` en Identity) | Versiones fijadas (`8.0.10` y `2.9.2`) en `.csproj` |
| 7 | A09 — Security Logging & Monitoring | Sin logging de eventos de seguridad | `ILogger` inyectado en `AuthService`/`ProductService` con eventos de login fallido y registro |

---

## 2. Detalle por vulnerabilidad

### 2.1 — A01: Broken Access Control

**Antes**

```csharp
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteProduct(long id) { ... }
```
Cualquier cliente HTTP podía eliminar productos. **No había autenticación ni autorización.**

**Después**

```csharp
[HttpDelete("{id:long}")]
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> DeleteProduct(long id) { ... }
```
- Token **JWT firmado HMAC-SHA256** obligatorio para escrituras.
- Política `AdminOnly` (declarada en `Program.cs`) que requiere el claim `role=Admin`.
- `[AllowAnonymous]` explícito en GET de catálogo (modelo de e-commerce).
- `OrderController` requiere autenticación global; listar/eliminar pedidos requiere rol Admin.

**Verificación:** ver `SecurityTests.NoToken_PostProduct_Returns401`, `NormalUserToken_PostProduct_Returns403` y `MalformedToken_PostProduct_Returns401`.

---

### 2.2 — A02: Cryptographic Failures (secretos en config)

**Antes**

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=...;User Id=sa;Password=Texto-Plano-1234;..."
}
```

**Después**

`appsettings.json`:

```json
"ConnectionStrings": {
  "_comment": "NO almacenar credenciales aquí. Configurar en User Secrets (dev) o variables de entorno (prod)."
}
```

- En **desarrollo** se usa `dotnet user-secrets`:
  ```bash
  cd TiendaPromElec
  dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;..."
  dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 48)"
  dotnet user-secrets set "Seed:AdminPassword" "Admin#2025!"
  ```
- En **producción / Docker** se inyectan como variables de entorno (`docker-compose.yml`).
- El código valida que `Jwt:Secret` exista y tenga ≥ 32 caracteres antes de firmar tokens (`TokenService`).

---

### 2.3 — A03: Injection

EF Core ya usaba consultas parametrizadas, pero el modelo aceptaba cualquier valor. Se añadió validación declarativa en modelos y DTOs:

```csharp
[Required, StringLength(200, MinimumLength = 2)]
public required string Name { get; set; }

[Range(0.01, 9999999)]
public decimal Price { get; set; }
```

ASP.NET Core devuelve **400 Bad Request automáticamente** cuando el `ModelState` es inválido — el filtro está activado por default.

**Verificación:** `ProductFunctionalityTests.CreateProduct_InvalidModel_Returns400`.

---

### 2.4 — A05: Security Misconfiguration

| Configuración | Implementación |
|---------------|----------------|
| **HTTPS forzado** | `app.UseHttpsRedirection()` con `Status308PermanentRedirect` |
| **HSTS** | 1 año, `Preload`, `IncludeSubDomains` |
| **CORS restrictivo** | `WithOrigins(...)` desde configuración. **Sin** `AllowAnyOrigin` |
| **Cookies seguras** | `SameSite=Strict`, `HttpOnly=Always`, `Secure=Always` |
| **Cabeceras HTTP** | `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `X-XSS-Protection: 1; mode=block`, `Permissions-Policy` |
| **Anti-Forgery** | Token con `SameSite=Strict` y `Secure=Always` |

**Verificación:** `SecurityTests.Response_IncludesSecurityHeaders` valida `X-Content-Type-Options` y `X-Frame-Options`.

---

### 2.5 — A07: Identification & Authentication Failures

Se integró **ASP.NET Core Identity** con `ApplicationUser : IdentityUser`. Esto da:

- **Hashing de contraseñas con PBKDF2** (configurable a Argon2 si se quiere).
- **Políticas de password fuertes:**
  - `RequiredLength = 8`
  - `RequireDigit`, `RequireLowercase`, `RequireUppercase`, `RequireNonAlphanumeric`
- **Lockout** automático tras 5 intentos fallidos (15 min).
- **Mensajes de login genéricos** (`"Credenciales inválidas."`) para no filtrar si un email existe.
- **`User.RequireUniqueEmail = true`**.

**Verificación:** `ProductFunctionalityTests.Login_WrongPassword_Returns401`, `Register_DuplicateEmail_Returns400`.

---

### 2.6 — A08: Software & Data Integrity Failures

Todas las dependencias del `.csproj` están **pinneadas a versiones específicas** (no `*` ni rangos abiertos):

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.10" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.10" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.10" />
```

Esto garantiza que un nuevo `dotnet restore` no introduzca paquetes maliciosos vía typosquatting o dependencias transitivas inesperadas.

---

### 2.7 — A09: Security Logging & Monitoring

`AuthService` registra eventos relevantes:

```csharp
_logger.LogWarning("Intento de registro con email ya existente: {Email}", dto.Email);
_logger.LogWarning("Login fallido (password incorrecto): {Email}", dto.Email);
_logger.LogInformation("Usuario registrado: {Email}", dto.Email);
_logger.LogInformation("Login exitoso: {Email}", dto.Email);
```

Estos logs se canalizan al sistema de logging estándar de ASP.NET Core. En producción pueden enviarse a Application Insights, ELK, Seq o cualquier proveedor compatible.

---

## 3. Tabla de cumplimiento de la rúbrica (sección 1)

| Criterio | Estado | Evidencia |
|----------|--------|-----------|
| Documento de análisis identifica al menos **3 vulnerabilidades** | ✅ Identifica **7** | Sección 2 |
| Implementación de **políticas de autorización** | ✅ `AdminOnly`, `AuthenticatedUser` | `Program.cs` líneas 75-79 |
| **CORS configurado** | ✅ Lista de orígenes restrictiva, sin AllowAnyOrigin | `Program.cs` líneas 92-101 |
| **HTTPS + HSTS** | ✅ Redirect 308 + HSTS 1 año | `Program.cs` líneas 105-115 |
| **Secretos fuera de config** | ✅ User Secrets / env vars | `appsettings.json` + `Program.cs` |
| **Validación de modelos** | ✅ DataAnnotations en todas las entidades + DTOs | `Models/` y `DTOs/` |
| **Hashing de contraseñas** | ✅ PBKDF2 vía Identity | `Program.cs` línea 35 |
| **Logging de eventos de seguridad** | ✅ `ILogger` en `AuthService` | `Services/AuthService.cs` |

---

## 4. Vectores de ataque mitigados (resumen)

- **Inyección SQL:** EF Core parametrizado + validación de input.
- **Robo de credenciales:** Hash PBKDF2 + sin secretos en config + HTTPS forzado.
- **Acceso no autorizado:** JWT + políticas de roles + lockout.
- **Clickjacking:** `X-Frame-Options: DENY`.
- **MIME sniffing:** `X-Content-Type-Options: nosniff`.
- **CSRF:** Cookies `SameSite=Strict` + token anti-forgery.
- **Information disclosure:** Mensajes genéricos en login.
- **Replay de tokens:** Tokens con expiración de 60 min y `ClockSkew=Zero`.
