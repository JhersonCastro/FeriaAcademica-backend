# Arquitectura del backend

Documento técnico de HU-082 (SCRUM-122 y SCRUM-124): cómo está organizado el código por módulos, cómo viaja una petición, dónde vive cada regla y qué cubren las pruebas. Las reglas para cambiar el código están en [CLAUDE.md](../CLAUDE.md); la puesta en marcha, en el [README](../README.md).

## Capas y dependencias

```
Api  ──▶  Application  ──▶  Domain
 └──────▶  Infrastructure ──▶ Application, Domain
```

| Proyecto | Qué contiene | Referencias reales (`.csproj`) |
| --- | --- | --- |
| `FeriaAcademica.Domain` | Entidades, enums, reglas de negocio e interfaces de repositorio | Ninguna: ni proyectos ni paquetes |
| `FeriaAcademica.Application` | Casos de uso (comandos y consultas con Mediator), DTO e interfaces de lectura | `Domain`, `Mediator` y `Microsoft.Extensions.DependencyInjection.Abstractions` |
| `FeriaAcademica.Infrastructure` | EF Core, repositorios, modelos de lectura, Identity, correo y PDF | `Application` (y con ella `Domain`) |
| `FeriaAcademica.Api` | Controladores y `Program.cs` | `Application` e `Infrastructure` |

## Mapa de módulos

Cada módulo tiene el mismo recorrido: entidad en el dominio, casos de uso en `Application/Features/<Módulo>/`, y un controlador que solo llama a `IMediator.Send(...)`.

| Módulo | Dominio (`Domain/`) | Casos de uso (`Application/Features/`) | Controlador y ruta |
| --- | --- | --- | --- |
| Eventos | `Events/`: `AcademicEvent`, `EventStage`, `EventTemplate`, `EventReport`, `RoleAssignment` | `Events/`: crear, publicar, cerrar, cupo, etapas, roles, informe | `EventsController` · `api/events` |
| Invitaciones | `Events/`: `Invitation`, `Participation` | `Invitations/`: enviar, ver y aceptar | `InvitationsController` · `api/invitations` |
| Trabajos y revisión | `Works/`: `Work`, `Artifact`, `ReviewQueueItem`, `WorkHistoryEntry` | `Works/`: crear, adjuntar, enviar, retirar, revisar, rechazar, reabrir, versión final | `WorksController` · `api/works`, `ReviewsController` · `api/reviews` |
| Inscripción y asistencia | `Registrations/`: `Registration`, `Attendance`, `AttendancePolicy` | `Registrations/`, `Attendance/` | `AttendanceController` · `api/events/{eventId}` |
| Certificados | `Registrations/`: `Certificate`, `CertificateEligibility` | `Certificates/` | `CertificatesController` · `api/certificates` |
| Usuarios y acceso | `Users/`: `UserAccount` (reglas del ciclo de vida) | `Users/`, `Auth/` | `UsersController` · `api/users`, `AuthController` · `api/auth` |
| Notificaciones | `Notifications/` | `Notifications/` | `NotificationsController` · `api/notifications` |

Hay entidades y tablas que todavía no tienen casos de uso: evaluación (`Instrument`, `Criterion`, `Response`, `WorkResult`), instituciones, políticas de datos y consentimientos, y `JuryAssignment`.

En `Infrastructure/` el código se reparte por responsabilidad: `Persistence/` (contexto, configuraciones, migraciones y `Repositories.cs`), `Identity/`, `Auth/`, `Email/` y `Pdf/`.

## Cómo viaja una petición

1. El controlador recibe la petición HTTP y envía un comando o una consulta con `IMediator.Send(...)`. No tiene lógica.
2. El handler (`internal sealed`, encontrado por el generador de Mediator) valida la entrada (por ejemplo, «las observaciones son obligatorias») y carga lo que necesita.
3. **Comando:** carga la entidad por su repositorio, llama a un método de la entidad y guarda con `IUnitOfWork`. **Consulta:** lee de la base de datos hacia un DTO por su `I...ReadModel`, sin cargar la entidad.
4. Si se rompe una regla se lanza `DomainException`; `Program.cs` la convierte en una respuesta 400 (`ProblemDetails`).

Ejemplo de referencia: `Application/Features/Works/Commands/SubmitWork/` llama a `Work.Submit(...)` en `Domain/Works/Work.cs`.

## Dónde vive cada regla

- **Reglas de negocio:** métodos de la entidad. Los estados cambian solo por métodos de la entidad (en `Work`, con `MoveTo`, que además guarda el historial). No hay setters públicos en el dominio.
- **Validación de entrada y «no encontrado»:** en el handler.
- **Quién puede hacer qué dentro de un evento:** el rol sale de `RoleAssignment` (los roles pertenecen al evento, no al usuario) y la regla está en el dominio, por ejemplo `AttendancePolicy` y `CertificateEligibility`. El único rol global, el súper admin, es la política `SuperAdmin` de `Program.cs` (claim creado por `AppClaimsPrincipalFactory`).
- **Identity:** el usuario es `ApplicationUser` (Infrastructure). El dominio no lo ve: las entidades lo referencian por `Guid`, y las reglas de la cuenta (`UserAccount`) reciben el estado y devuelven el siguiente.

## Persistencia

PostgreSQL con Npgsql. Las implementaciones de repositorios y modelos de lectura están en `Infrastructure/Persistence/Repositories.cs`, y cada tabla se configura en `Persistence/Configurations/`. Todas las claves foráneas son `Restrict` (sin borrado en cascada). Cada cambio del modelo lleva su migración; el CI falla si falta.

## Pruebas

```bash
dotnet test
```

| Proyecto | Qué prueba | Cómo |
| --- | --- | --- |
| `FeriaAcademica.Domain.Tests` | Una prueba por regla del dominio: eventos, trabajos y su revisión, invitaciones, asistencia, certificados, versión final, notificaciones, roles y cuentas | Sin base de datos |
| `FeriaAcademica.Api.Tests` | La API real (controladores, Mediator, Identity, EF Core) sobre SQLite en memoria, con `ApiFactory`. Hoy cubre autenticación, recuperación de contraseña, administración de usuarios, versión final, informe del evento y certificados | No necesita PostgreSQL |

Los flujos que todavía no tienen prueba de API son la creación y el envío de trabajos (EP-02), su revisión, las invitaciones y la inscripción con QR; su cobertura es la subtarea SCRUM-123.

Las pruebas del informe y del certificado en PDF necesitan una fuente sans-serif instalada (Arial en Windows; Liberation o DejaVu en Linux).

## Revisión de la organización por módulos (SCRUM-122)

Qué se comprobó y cómo repetirlo:

| Comprobación | Resultado | Cómo verlo |
| --- | --- | --- |
| `Domain` no depende de nada | Cumple: ningún `ProjectReference` ni paquete, y solo `using` de `System` y del propio dominio | `grep -rh "^using " src/FeriaAcademica.Domain` |
| `Application` no usa EF Core, Identity ni HTTP | Cumple: ningún archivo importa `Microsoft.EntityFrameworkCore` ni `Microsoft.AspNetCore` | `grep -rln "Microsoft.EntityFrameworkCore\|Microsoft.AspNetCore" src/FeriaAcademica.Application` |
| Los controladores no tienen lógica | Cumple: los nueve reciben solo `IMediator` y ninguno usa el `DbContext` | `grep -h "Controller(" src/FeriaAcademica.Api/Controllers/*.cs` |
| Los handlers son `internal sealed` | Cumple: todos | `grep -rL "internal sealed class" src/FeriaAcademica.Application --include=*Handler.cs` |
| El dominio no tiene setters públicos | Cumple: ninguno | `grep -rn "{ get; set; }" src/FeriaAcademica.Domain` |

Observaciones (no rompen ninguna regla; se anotan por si el equipo quiere ordenarlas más adelante, sin reorganizar nada ahora):

- **Nombres de módulo distintos entre capas.** `Domain/Registrations/` guarda también asistencia y certificados, e `Invitation` vive en `Domain/Events/`, mientras `Application` los separa en `Attendance`, `Certificates` e `Invitations`. Los archivos agrupan varias entidades (`Participants.cs`, `Registrations.cs`, `WorkParts.cs`).
- **`DomainException` en Infrastructure.** `AuthService` y `UserAccounts` la lanzan al traducir resultados de Identity (credenciales inválidas, enlace vencido, correo duplicado). Es traducción de errores, no una regla de negocio nueva, pero es la única excepción a «Infrastructure no tiene reglas».
- **`Repositories.cs` concentra todas las implementaciones** (unas 360 líneas), como indica `CLAUDE.md`. Si sigue creciendo conviene dividirlo, lo que exigiría actualizar esa regla.
- **`Api` usa `Infrastructure.Identity`** (`AppClaims`) para las políticas y los claims. Está permitido por el esquema de dependencias, pero esos nombres podrían vivir en `Application` si algún día `Api` deja de referenciar `Infrastructure`.
