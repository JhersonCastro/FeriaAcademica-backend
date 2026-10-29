# FeriaAcademica Backend

API para la plataforma de gestión de ferias académicas. .NET 10, ASP.NET Core Identity (cookie), EF Core con PostgreSQL y CQRS con [Mediator](https://github.com/martinothamar/Mediator) (MIT).

## Requisitos

- .NET SDK 10
- PostgreSQL

## Cómo correrlo

```bash
dotnet tool restore

# La contraseña no vive en el repo: va en user-secrets (o en la variable ConnectionStrings__Default)
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=feria_academica;Username=postgres;Password=TU_PASSWORD" --project src/FeriaAcademica.Api

dotnet run --project src/FeriaAcademica.Api
```

En `Development` la API aplica las migraciones pendientes al arrancar.

### Primer súper admin

Al arrancar, si no existe ningún súper admin, la API lo crea con estos datos (van en user-secrets, nunca en `appsettings`):

```bash
dotnet user-secrets set "SuperAdmin:Email" "admin@tu-dominio.edu.co" --project src/FeriaAcademica.Api
dotnet user-secrets set "SuperAdmin:Password" "UnaClaveSegura1!" --project src/FeriaAcademica.Api
dotnet user-secrets set "SuperAdmin:FullName" "Administración" --project src/FeriaAcademica.Api   # opcional
```

### Correo (SMTP)

Los enlaces para definir o recuperar la contraseña y las invitaciones se envían por correo. Mientras no haya SMTP configurado, en `Development` el correo completo (con el enlace) se escribe en la consola de la API; en otros entornos solo se registra que no se pudo enviar.

```bash
dotnet user-secrets set "Smtp:Host" "smtp.gmail.com" --project src/FeriaAcademica.Api
dotnet user-secrets set "Smtp:Port" "587" --project src/FeriaAcademica.Api
dotnet user-secrets set "Smtp:User" "cuenta@gmail.com" --project src/FeriaAcademica.Api
dotnet user-secrets set "Smtp:Password" "contraseña-de-aplicación" --project src/FeriaAcademica.Api
dotnet user-secrets set "Smtp:From" "cuenta@gmail.com" --project src/FeriaAcademica.Api   # opcional, por defecto Smtp:User
```

Los enlaces apuntan a `Frontend:BaseUrl` (`appsettings.json`, por defecto `http://localhost:4200`).

## Usuarios y roles

- **Súper admin** (el administrador): único rol global. Crea usuarios, los edita, los activa o desactiva, crea eventos y asigna roles.
- **Roles por evento**: coordinador (`Coordinator`), profesor (`Teacher`), jurado (`Jury`), estudiante (`Participant`) y público (`Attendee`). Una persona puede tener varios roles en un mismo evento y roles distintos en eventos distintos; no quedan en el usuario.
- **Estados de la cuenta**: `Pending` (creada, sin contraseña), `Active` e `Inactive` (no puede iniciar sesión y sus sesiones abiertas se cierran en menos de un minuto).

Las cuentas nacen de dos formas:

1. El súper admin la crea (`POST /api/users`) y la persona recibe un enlace para definir su contraseña.
2. Un profesor de un evento invita por correo a un estudiante o a un invitado (`POST /api/events/{id}/invitations`). Si la persona ya tiene cuenta, acepta con su sesión iniciada; si no, crea la cuenta al aceptar.

### Endpoints

| Método y ruta | Quién | Para |
| --- | --- | --- |
| `POST /api/auth/login` · `POST /api/auth/logout` | Todos | Sesión con cookie. Tras 5 intentos fallidos la cuenta se bloquea 15 minutos. |
| `GET /api/auth/me` | Con sesión | Datos del usuario, si es súper admin y sus roles por evento (para armar el menú). |
| `POST /api/auth/forgot-password` | Todos | Envía el enlace de recuperación. Responde igual exista o no el correo. |
| `POST /api/auth/reset-password` | Todos | Define la contraseña con el enlace (recuperación o primera contraseña). |
| `PUT /api/users/me` | Con sesión | Editar mis datos personales. |
| `GET /api/users?search=&role=&status=&page=&pageSize=` | Súper admin | Listado paginado; busca por nombre o correo y filtra por rol o estado. |
| `GET /api/users/{id}` · `POST /api/users` · `PUT /api/users/{id}` | Súper admin | Ver, crear y editar usuarios. |
| `POST /api/users/{id}/activate` · `POST /api/users/{id}/deactivate` | Súper admin | Cambiar el estado. |
| `POST /api/events/{id}/roles` | Súper admin | Dar un rol a un usuario en un evento. |
| `POST /api/events/{id}/invitations` | Profesor del evento | Invitar a un estudiante (`Participant`) o invitado (`Attendee`). |
| `GET /api/invitations/{token}` · `POST /api/invitations/{token}/accept` | Todos | Ver y aceptar una invitación. |

Rutas del frontend que usan los correos: `/account/set-password?userId=&token=`, `/account/reset-password?userId=&token=` e `/invitations/{token}`.

### PostgreSQL local sin Docker

- **Windows:** doble clic en `scripts/windows/feria-db.cmd`. Detecta si PostgreSQL está instalado (si no, ofrece instalarlo con `winget`).
- **macOS:** doble clic en `scripts/macos/feria-db.command` (la primera vez, clic derecho → Abrir). Mismo menú que en Windows, usando Homebrew: instala PostgreSQL 17 si falta. El usuario es el de tu sistema y por defecto no lleva contraseña.

Ambos scripts dan el mismo menú: iniciar/parar el servicio, listar tablas, abrir `psql`, copiar el connection string, actualizar `dotnet user-secrets`, reiniciar la base e instalar PostgreSQL.

## Asistencia con código QR

El backend genera el QR de cada inscrito y registra la asistencia; el frontend dibuja el QR y usa la cámara.

1. La persona se inscribe (`POST /api/events/{id}/registrations`). La respuesta trae `qrToken`: **ese texto es exactamente lo que debe llevar el QR** (el frontend lo dibuja). Es único por persona y por evento, y no se puede adivinar. Inscribirse de nuevo devuelve el mismo QR. La respuesta incluye también los datos básicos del perfil (`fullName`, `email`).
2. En la puerta, el coordinador (o un profesor) del evento, o el súper admin, escanea el QR con la cámara y manda el texto leído a `POST /api/events/{id}/attendance/scan` con `{ "qrToken": "..." }`.
3. La respuesta siempre es clara para la pantalla de la puerta:

| `outcome` | Significa |
| --- | --- |
| `Valid` | Primera lectura: la asistencia quedó registrada (con fecha, hora y quién validó). |
| `AlreadyEntered` | Esa persona ya había ingresado: no se duplica y se devuelve la hora del primer ingreso. |
| `Invalid` | El QR no existe, es de otro evento o la inscripción fue cancelada. |

La respuesta incluye `attendeeName` y `enteredAt` para mostrar quién es. Dos lectores escaneando el mismo QR a la vez producen una sola asistencia (índice único en la base). Solo se inscribe y se toma asistencia mientras el evento está publicado o en curso.

**Cupos:** un evento puede tener `capacity` (opcional; sin valor es ilimitado). Si ya no quedan cupos, inscribirse responde 400 (`The event is full`). Solo las inscripciones no canceladas ocupan cupo. `GET /api/events` devuelve `capacity`, `registered` y `availableSeats`. El súper admin lo define al crear el evento o con `PUT /api/events/{id}/capacity` (`{ "capacity": 100 }`, `null` quita el límite; no puede ser menor que los inscritos actuales).

| Método y ruta | Quién | Para |
| --- | --- | --- |
| `POST /api/events/{id}/registrations` | Con sesión | Inscribirse y obtener el QR. |
| `GET /api/events/{id}/registrations/me` | Con sesión | Mi inscripción: `qrToken` y si ya ingresé (`enteredAt`). |
| `POST /api/events/{id}/attendance/scan` | Súper admin, coordinador o profesor del evento | Registrar asistencia con el QR leído. |
| `GET /api/events/{id}/attendance/summary` | Súper admin, coordinador o profesor del evento | `registered`, `entered`, `pending` y `lastEntryAt`; el frontend lo consulta cada pocos segundos para ver la afluencia. Las inscripciones canceladas no cuentan. |

## Estructura

```
src/
├── FeriaAcademica.Api             Controllers y Program.cs. Solo traducen HTTP a comandos/consultas.
├── FeriaAcademica.Application     Casos de uso (CQRS).
│   └── Features/<Dominio>/Commands|Queries/<Caso>/
├── FeriaAcademica.Domain          Entidades, enums y reglas de negocio. No depende de nadie.
└── FeriaAcademica.Infrastructure  EF Core, repositorios, Identity. Implementa las interfaces de Application y Domain.
tests/
└── FeriaAcademica.Domain.Tests    Pruebas de las reglas del dominio (xUnit).
```

### Dónde va cada cosa

| Quiero... | Lo pongo en... |
| --- | --- |
| Una regla de negocio | Método de la entidad en `Domain/<Dominio>/` (el handler nunca decide reglas) |
| Una acción que escribe | `Application/Features/<Dominio>/Commands/<Caso>/` con `<Caso>Command.cs` y `<Caso>Handler.cs` |
| Una lectura | `Application/Features/<Dominio>/Queries/<Caso>/` con Query, Handler, DTO e `I...ReadModel` |
| Su implementación con EF | `Infrastructure/Persistence/` |
| Un endpoint | `Api/Controllers/`, solo llama a `IMediator.Send(...)` |
| Una tabla o columna nueva | Entidad en Domain + configuración en `Infrastructure/Persistence/Configurations/` + migración |

Ejemplo, el caso `SubmitWork`:

```csharp
// Application/Features/Works/Commands/SubmitWork/SubmitWorkCommand.cs
public sealed record SubmitWorkCommand(Guid WorkId, Guid ActorId) : ICommand<Unit>;

// Application/Features/Works/Commands/SubmitWork/SubmitWorkHandler.cs
internal sealed class SubmitWorkHandler(IWorkRepository works, IUnitOfWork uow)
    : ICommandHandler<SubmitWorkCommand, Unit>
{
    public async ValueTask<Unit> Handle(SubmitWorkCommand c, CancellationToken ct)
    {
        var work = await works.GetAsync(c.WorkId, ct) ?? throw new DomainException("Work not found.");
        work.Submit(c.ActorId, DateTime.UtcNow);   // la regla vive en Domain/Works/Work.cs
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
```

Las `DomainException` se convierten en respuestas 400 en `Program.cs`.

## Base de datos y migraciones

Cuando cambies una entidad o su configuración:

```bash
dotnet ef migrations add NombreDelCambio \
  --project src/FeriaAcademica.Infrastructure \
  --startup-project src/FeriaAcademica.Api \
  --output-dir Persistence/Migrations
```

El CI falla si el modelo cambió y falta la migración.

## Pruebas

```bash
dotnet test
```

## Flujo de trabajo con Git

`main` está protegida: no se hace push directo, todo entra por Pull Request y el CI (`build-and-test`) debe pasar.

Las ramas se nombran `tipo/descripcion-corta` en minúsculas:

| Prefijo | Para |
| --- | --- |
| `feature/` | Funcionalidad nueva |
| `fix/` | Corrección de un bug |
| `hotfix/` | Corrección urgente |
| `refactor/` | Cambios internos sin cambiar comportamiento |
| `test/` | Pruebas |
| `docs/` | Documentación |
| `chore/` | Configuración, dependencias, CI |

Ejemplos: `feature/reject-work`, `fix/queue-order`, `chore/update-dependencies`.
