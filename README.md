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

Los enlaces de definir o recuperar la contraseña vencen a las 24 horas y solo sirven una vez; cambiar la contraseña con uno de ellos también levanta un bloqueo por intentos fallidos. Las cuentas desactivadas no reciben enlace de recuperación.

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
| `Invalid` | El QR no existe, es de otro evento, la inscripción fue cancelada o es el propio QR de quien escanea ("You cannot scan your own QR code."): nadie puede registrar su propia asistencia, tampoco el súper admin; otra persona sí puede escanearlo. |

La respuesta incluye `attendeeName` y `enteredAt` para mostrar quién es. Dos lectores escaneando el mismo QR a la vez producen una sola asistencia (índice único en la base). Solo se inscribe y se toma asistencia mientras el evento está publicado o en curso.

**Cupos:** un evento puede tener `capacity` (opcional; sin valor es ilimitado). Si ya no quedan cupos, inscribirse responde 400 (`The event is full`). Solo las inscripciones no canceladas ocupan cupo. `GET /api/events` devuelve `capacity`, `registered` y `availableSeats`. El súper admin lo define al crear el evento o con `PUT /api/events/{id}/capacity` (`{ "capacity": 100 }`, `null` quita el límite; no puede ser menor que los inscritos actuales).

| Método y ruta | Quién | Para |
| --- | --- | --- |
| `POST /api/events/{id}/registrations` | Con sesión | Inscribirse y obtener el QR. |
| `GET /api/events/{id}/registrations/me` | Con sesión | Mi inscripción: `qrToken` y si ya ingresé (`enteredAt`). |
| `POST /api/events/{id}/attendance/scan` | Súper admin, coordinador o profesor del evento | Registrar asistencia con el QR leído. |
| `GET /api/events/{id}/attendance/summary` | Súper admin, coordinador o profesor del evento | `registered`, `entered`, `pending` y `lastEntryAt`; el frontend lo consulta cada pocos segundos para ver la afluencia. Las inscripciones canceladas no cuentan. |

## Envío de trabajos (EP-02)

**Participación.** Al aceptar una invitación de estudiante (`Participant`) se crea su participación en el evento, enlazada a esa invitación. El `participationId` que pide `POST /api/works` es el de esa participación: `GET /api/events/{id}/participations/me` lo devuelve junto con `invitedBy`, el profesor que invitó (404 si la persona no participa en el evento).

**Revisor.** El estudiante no elige quién revisa su trabajo: es el profesor que lo invitó. `POST /api/works` recibe solo `participationId`, `title` y `summary`; el revisor (`professorId` del trabajo) sale de la invitación de la que nació la participación. Si se envía un `professorId`, se ignora. Una participación que no viene de una invitación no puede crear trabajos (400, no tiene profesor que lo revise). El trabajo aparece en `GET /api/reviews/assigned` de ese profesor y de nadie más. `GET /api/events/{id}/professors` sigue disponible para mostrar quiénes son los profesores del evento.

**Archivos (HU-010).** `POST /api/works/{id}/artifacts/file` recibe un formulario `multipart/form-data` con `type` y `file`. Se valida el formato (PDF), el tamaño (10 MB), que el trabajo sea borrador y que no haya pasado el plazo; el archivo se guarda en disco y queda como una versión nueva del artefacto. La carpeta se define con `Storage:Path` (por defecto `uploads/`, ignorada por git). Los enlaces siguen entrando por `POST /api/works/{id}/artifacts`.

**Descargar un archivo.** El `fileUrl` de un artefacto subido (`/api/works/{id}/files/{fileId}`) se abre con `GET` y descarga el PDF. Pueden hacerlo el autor, el profesor que revisa el trabajo, los coordinadores del evento al que pertenece, el jurado asignado a ese trabajo y el súper admin. Ser jurado del evento no basta: tiene que estar asignado al trabajo. Cualquier otra persona recibe 404, igual que si el archivo no existiera, y sin sesión responde 401.

**Etapas de la feria.** Solo el súper admin. `GET /api/events/{id}/stages` lista las etapas de la línea de tiempo, ordenadas por fecha de inicio (las que no tienen fechas van al final): `type` (`Configuration`, `Call`, `Nomination`, `Reception`, `Admission`, `Registration`, `Evaluation`, `Results`, `Publication`), `status` (`Scheduled`, `Open` o `Closed`), `startDate` y `endDate` en UTC (`null` si no tiene). Las etapas quitadas no aparecen; 404 si la feria no existe. `PUT /api/events/{id}/stages/{type}` agrega o reprograma una etapa (`{ "startDate": ..., "endDate": ... }`; envíe siempre la zona, `Z` o un desfase como `-05:00`, porque una fecha sin zona se toma como hora del servidor) y vuelve a poner en la línea de tiempo una que se quitó. `DELETE /api/events/{id}/stages/{type}` la quita de la línea de tiempo: no la borra, queda deshabilitada y pierde sus fechas (400 si no está en la línea de tiempo). Una feria publicada conserva al menos una etapa activa, y las de una feria cerrada no se cambian. Quitar la recepción elimina el plazo de entrega. El backend no valida que las etapas no se solapen: eso lo muestra el frontend.

**Plazo (HU-013).** `GET /api/events` devuelve `submissionDeadline`: el fin de la etapa de recepción (`null` si no hay plazo). Después de esa fecha no se puede crear, adjuntar, enviar ni retirar un trabajo enviado.

| Método y ruta | Quién | Para |
| --- | --- | --- |
| `GET /api/events/{id}/participations/me` | Con sesión | Mi participación en el evento (`id`, `invitedBy`). |
| `GET /api/events/{id}/professors` | Con sesión | Profesores del evento (`id`, `fullName`). |
| `POST /api/works/{id}/artifacts/file` | Autor del trabajo | Subir un archivo como nueva versión de un artefacto. |
| `GET /api/works/{id}/files/{fileId}` | Autor, profesor que revisa, coordinador del evento, jurado asignado y súper admin | Descargar un archivo subido (`fileUrl` del artefacto). |

## Versión final, informe del evento y certificados

**Versión final (HU-030).** Cuando el profesor aprueba un trabajo (`Admitted`), su autor sube la versión definitiva con `POST /api/works/{id}/final-version` y `{ "fileUrl", "fileName", "fileSizeBytes" }`. Se valida el formato (PDF) y el tamaño (10 MB) y el documento queda marcado como definitivo (`isFinal` en `GET /api/works/{id}`). Solo hay una versión final por trabajo.

**Informe del evento (HU-031, HU-032).** `GET /api/events/{id}/report/summary` calcula las cifras en el momento de pedirlas. El informe es HTML libre que arma el súper admin (texto, estilos, imágenes en línea con `data:`), y el frontend inserta allí esas cifras. El PDF es esa misma página; el servidor no descarga imágenes ni hojas de estilo externas.

| Campo | Cuenta |
| --- | --- |
| `worksReceived` | Trabajos enviados al menos una vez. Es la suma de los cinco campos siguientes. |
| `worksApproved` | Aprobados (`Admitted` o `Evaluated`). |
| `worksRejected` | Rechazados definitivamente. |
| `worksInReview` | Enviados o en revisión. |
| `worksReturnedForChanges` | Devueltos a borrador para corregir o retirados antes de la revisión. |
| `worksWithdrawn` | Cancelados por su autor. |
| `registered` · `attended` | Inscritos activos y cuántos de ellos ingresaron. |

**Certificados (HU-060, HU-061).** Solo se descargan cuando el evento terminó (`Closed` o `Archived`). Certificado de asistencia: la persona tiene asistencia registrada por QR. Certificado de participación: es autora de un trabajo con versión final. Certificado de reconocimiento: tiene rol de coordinador, profesor o jurado en el evento. Se emite la primera vez que se descarga y después se devuelve el mismo código.

| Método y ruta | Quién | Para |
| --- | --- | --- |
| `POST /api/works/{id}/final-version` | Autor del trabajo | Subir la versión final. |
| `GET /api/events/{id}/report/summary` | Súper admin | Cifras consolidadas del evento, actualizadas al pedirlas. |
| `GET` · `PUT /api/events/{id}/report` | Súper admin | Leer o guardar el HTML del informe. |
| `GET /api/events/{id}/report/pdf` | Súper admin | Descargar el informe en PDF. |
| `POST /api/events/{id}/close` | Súper admin | Terminar el evento (habilita los certificados y cierra inscripciones). |
| `GET /api/certificates/mine` | Con sesión | «Mis certificados»: `type`, `available` y `code` si ya se emitió. |
| `GET /api/certificates/events/{eventId}/{Attendance\|Participation\|Recognition}/pdf` | Con sesión | Descargar el certificado en PDF. |

Los PDF usan PDFsharp y HtmlRenderer (MIT/BSD) y necesitan una fuente sans-serif instalada (Arial en Windows; Liberation o DejaVu en Linux).

## Seguridad (HU-080)

- **Contraseñas:** Identity las guarda como hash PBKDF2 con sal aleatoria; nunca se guardan tal como se escriben.
- **HTTPS:** fuera de `Development`, la API redirige HTTP a HTTPS, envía HSTS y marca la cookie como `Secure`.
- **Sesión:** cookie `feria.auth`, `HttpOnly`, con un ticket cifrado que vence tras 8 horas sin uso.
- **CORS:** solo acepta llamadas con cookie desde `Frontend:BaseUrl`. En producción, esa dirección debe ser la HTTPS del frontend.

En `Development` el perfil `http` sigue siendo el predeterminado: un frontend en `http://localhost:4200` no enviaría la cookie `SameSite=Lax` a una API en `https://`. Si la API se despliega detrás de un proxy que termina el HTTPS, hay que añadir encabezados reenviados (forwarded headers) y definir `ASPNETCORE_HTTPS_PORT`.

## Estructura

```
src/
├── FeriaAcademica.Api             Controllers y Program.cs. Solo traducen HTTP a comandos/consultas.
├── FeriaAcademica.Application     Casos de uso (CQRS).
│   └── Features/<Dominio>/Commands|Queries/<Caso>/
├── FeriaAcademica.Domain          Entidades, enums y reglas de negocio. No depende de nadie.
└── FeriaAcademica.Infrastructure  EF Core, repositorios, Identity. Implementa las interfaces de Application y Domain.
tests/
├── FeriaAcademica.Domain.Tests    Pruebas de las reglas del dominio (xUnit).
└── FeriaAcademica.Api.Tests       Pruebas de integración: la API real sobre SQLite en memoria (no necesitan PostgreSQL).
```

El mapa de módulos, cómo viaja una petición, dónde vive cada regla y qué cubren las pruebas están en [docs/arquitectura.md](docs/arquitectura.md).

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
