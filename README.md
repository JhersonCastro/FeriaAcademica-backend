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

### PostgreSQL local sin Docker

- **Windows:** doble clic en `scripts/windows/feria-db.cmd`. Detecta si PostgreSQL está instalado (si no, ofrece instalarlo con `winget`).
- **macOS:** doble clic en `scripts/macos/feria-db.command` (la primera vez, clic derecho → Abrir). Mismo menú que en Windows, usando Homebrew: instala PostgreSQL 17 si falta. El usuario es el de tu sistema y por defecto no lleva contraseña.

Ambos scripts dan el mismo menú: iniciar/parar el servicio, listar tablas, abrir `psql`, copiar el connection string, actualizar `dotnet user-secrets`, reiniciar la base e instalar PostgreSQL.

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
