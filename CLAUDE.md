# FeriaAcademica backend: instructions for Claude

API for an academic fairs platform. .NET 10, ASP.NET Core Identity (cookie auth), EF Core + PostgreSQL, CQRS with the [Mediator](https://github.com/martinothamar/Mediator) library (MIT).

Read this before changing anything. The architecture is deliberate; do not reorganise it or introduce other patterns.

## Hard rules

- **Never add yourself as co-author.** No `Co-Authored-By` trailer in commits and no "Generated with Claude Code" line in pull requests. `.claude/settings.json` already disables the automatic attribution; do not add it by hand either.
- **Never push to `main`.** It is protected: every change goes through a pull request, and CI (`build-and-test`, `branch-name`) must pass. Do not merge pull requests unless the user asks.
- **Branch names** are `type/short-description`, lowercase: `feature/`, `fix/`, `hotfix/`, `refactor/`, `test/`, `docs/`, `chore/`. CI rejects anything else.
- Do not add libraries, layers or patterns that were not asked for. Do not use MediatR (commercial licence); Mediator is the one in use.
- Code, comments, commit messages and PR text are in **English**. README and user-facing explanations are in Spanish.
- Ask before doing anything destructive or outward-facing (deleting data, changing GitHub settings, force pushing).

## Architecture

```
Api  ──▶  Application  ──▶  Domain
 └──────▶  Infrastructure ──▶ Application, Domain
```

| Project | Contains | Must not |
| --- | --- | --- |
| `FeriaAcademica.Domain` | Entities, enums, business rules, repository interfaces | reference any other project or package |
| `FeriaAcademica.Application` | Use cases (commands/queries + handlers), DTOs, read-model interfaces | reference EF Core, Identity or HTTP types |
| `FeriaAcademica.Infrastructure` | EF Core (`AppDbContext`, configurations, migrations), repositories, read models, Identity | hold business rules |
| `FeriaAcademica.Api` | Controllers, `Program.cs` | hold logic: controllers only call `IMediator.Send(...)` |

### Where each thing goes

| I want to... | Put it in... |
| --- | --- |
| A business rule | A method on the entity in `Domain/<Area>/`. Handlers never decide business rules. |
| An action that writes | `Application/Features/<Domain>/Commands/<UseCase>/` with `<UseCase>Command.cs` and `<UseCase>Handler.cs` |
| A read | `Application/Features/<Domain>/Queries/<UseCase>/` with the Query, Handler, DTO and an `I...ReadModel` interface |
| The EF implementation of a repository or read model | `Infrastructure/Persistence/Repositories.cs` |
| A table or column | Entity in Domain + `IEntityTypeConfiguration` in `Infrastructure/Persistence/Configurations/` + a migration |
| An endpoint | `Api/Controllers/`, calling only `IMediator.Send(...)` |

Reference example: `Application/Features/Works/Commands/SubmitWork/` (command + handler) calling `Work.Submit(...)` in `Domain/Works/Work.cs`.

## CQRS with Mediator

- Namespace `Mediator`: `ICommand<T>`, `IQuery<T>`, `ICommandHandler<,>`, `IQueryHandler<,>`. Handlers return `ValueTask<T>`; use `Unit` when there is nothing to return. Dispatch with `IMediator.Send`.
- Commands and queries are `record`s. **Input validation goes inside the handler** (e.g. "observations are required"); business rules go in the entity.
- Handlers are `internal sealed`. The source generator (package referenced in `Application`) finds them and `AddApplication()` calls `AddMediator` once, with scoped lifetime because handlers use the scoped `DbContext`. Do not register handlers by hand.
- A query reads straight from the database into a DTO through its `I...ReadModel`, without loading the entity. A command loads the entity through its repository interface, calls a method on it and saves with `IUnitOfWork`.
- A rule violation throws `DomainException`; `Program.cs` turns it into a 400.

## Domain

- Entities inherit `Entity` (`Guid Id`), have private setters and a private parameterless constructor for EF, and are created through `static Create(...)`.
- **State changes only through entity methods.** There is no public setter for state. `Work.Status` changes only inside `Work`, via `MoveTo`, which also appends to the history.
- Statuses and kinds are enums, stored as text in the database.
- The model mirrors the original 24-table schema. `AspNetUsers` (Identity) is the table `usuario`: **Identity is the only user**. Domain entities reference users by `Guid` (no navigation to `ApplicationUser`); the foreign keys to `AspNetUsers` are configured in Infrastructure.
- Only `Work` (and a little of `AcademicEvent`) has behaviour so far. The other entities are mapped model with factories. Extend them the same way when a use case needs it.
- Work lifecycle: Draft → Submitted → InReview → Admitted | Draft (reject) | RejectedDefinitively; Reopen goes back to Draft; Withdraw goes Draft → Cancelled or Submitted → Draft. Rejection number `MaxRejections` closes the work automatically.

## Persistence

- PostgreSQL through Npgsql. Table and column names are PascalCase (EF default); enums are strings; JSON columns are `jsonb`.
- No cascading deletes (all foreign keys are `Restrict`).
- Migrations live in `Infrastructure/Persistence/Migrations`. **Whenever an entity or its configuration changes, add a migration**; CI fails otherwise. In Development the API applies pending migrations on startup.
- The four triggers of the original schema are not migrated (template only by superadmin, append-only history, jury needs the jury role, value within scale). The check constraints and unique indexes are.

```bash
dotnet ef migrations add <Name> \
  --project src/FeriaAcademica.Infrastructure \
  --startup-project src/FeriaAcademica.Api \
  --output-dir Persistence/Migrations
```

## Authentication

Cookie auth through ASP.NET Core Identity, email and password only. External providers (Google, etc.) were removed on purpose; do not re-add them.

## Commands

```bash
dotnet tool restore                  # installs dotnet-ef
dotnet build                         # warnings are errors
dotnet test
dotnet run --project src/FeriaAcademica.Api
```

The connection string is **not** in the repo. Set it with `dotnet user-secrets set "ConnectionStrings:Default" "..." --project src/FeriaAcademica.Api`. Never put passwords or secrets in `appsettings*.json`. `scripts/windows/feria-db.cmd` and `scripts/macos/feria-db.command` install and manage a local PostgreSQL and can update the user-secret. Docker is deliberately not used.

## Tests

`tests/FeriaAcademica.Domain.Tests` (xUnit) holds one or more tests per `Work` rule. When you add or change a domain rule, add or update its test. There are no handler or persistence tests yet.

## Known gaps

- No authorization checks yet beyond "only the assigned professor reviews a work"; roles per event are modelled but not enforced.
- There is no endpoint for the `Work` commands and query yet (`SubmitWork`, `RejectWork`, `ReopenWork`, `GetWork`).
- The Windows script has not been run on a real Windows machine.
