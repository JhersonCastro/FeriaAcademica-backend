# FeriaAcademica-backend

Estructura base (concepto) del backend: **.NET 10 · DDD · CQRS · ASP.NET Core Identity (cookie) + login con Google**.
Solo muestra la arquitectura; no es el sistema completo.

## Capas
```
src/
├─ FeriaAcademica.Domain          Agregados, eventos de dominio, reglas de negocio (sin dependencias)
│    └─ Eventos/Evento.cs         Agregado raíz de ejemplo
├─ FeriaAcademica.Application     CQRS: comandos, consultas, handlers y puertos (interfaces)
│    ├─ Eventos/Commands          CrearEvento, AprobarEvento      (escritura)
│    ├─ Eventos/Queries           ListarEventos                   (lectura, read model aparte)
│    └─ Auth                      Puerto IAuthService + comandos de sesión
├─ FeriaAcademica.Infrastructure  EF Core (SQLite), repositorios, Identity (ApplicationUser), AuthService
└─ FeriaAcademica.Api             Controllers delgados que solo despachan comandos/consultas
```
Dependencias: `Api → Application, Infrastructure` · `Infrastructure → Application` · `Application → Domain`.

- **CQRS:** `IDispatcher.Send(command)` / `Ask(query)` con un despachador propio (sin MediatR). Escritura por repositorio + agregado; lectura por `IEventoReadModel` con proyecciones directas.
- **Identidad:** `ApplicationUser : IdentityUser` + `UserManager`/`SignInManager`. Sesión por **cookie** de Identity (`feria.auth`, HttpOnly).
- **Google (OAuth 2.0):** `GET /api/auth/google` → Google → `/api/auth/google/callback` → Identity crea/vincula el usuario, emite su cookie y redirige al frontend. Para Microsoft u otros: `AddMicrosoftAccount(...)` con el mismo patrón.

## Endpoints
| Método | Ruta | |
|---|---|---|
| POST | `/api/auth/registro` · `/api/auth/login` · `/api/auth/logout` | sesión |
| GET | `/api/auth/me` | usuario actual |
| GET | `/api/auth/google` | login con Google |
| GET/POST | `/api/eventos` · `/api/eventos/{id}/aprobar` | requiere sesión |

## Ejecutar
```bash
dotnet run --project src/FeriaAcademica.Api
```
Crea `feria.db` (SQLite) al arrancar en Development. Para Google, configura credenciales (no las subas al repo):
```bash
cd src/FeriaAcademica.Api
dotnet user-secrets init
dotnet user-secrets set "Authentication:Google:ClientId" "..."
dotnet user-secrets set "Authentication:Google:ClientSecret" "..."
```
URI de redirección autorizada en Google: `https://localhost:<puerto>/signin-google`.

Frontend Angular (`http://localhost:4200`): llamar con `withCredentials: true`; CORS ya permite credenciales para ese origen.

## Pendiente (a propósito)
Migraciones de EF, roles/políticas, validación de comandos, despacho de eventos de dominio, tests.
