using FeriaAcademica.Application.Features.Users.Commands.ActivateUser;
using FeriaAcademica.Application.Features.Users.Commands.CreateUser;
using FeriaAcademica.Application.Features.Users.Commands.DeactivateUser;
using FeriaAcademica.Application.Features.Users.Commands.UpdateMyProfile;
using FeriaAcademica.Application.Features.Users.Commands.UpdateUser;
using FeriaAcademica.Application.Features.Users.Queries.GetUser;
using FeriaAcademica.Application.Features.Users.Queries.ListUsers;
using FeriaAcademica.Infrastructure.Identity;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeriaAcademica.Api.Controllers;

[ApiController, Route("api/users"), Authorize]
public class UsersController(IMediator mediator) : ControllerBase
{
    public sealed record ProfileRequest(string FullName, string? PhoneNumber, DateOnly? BirthDate, string? GuardianName, string? GuardianEmail);
    public sealed record CreateUserRequest(string Email, string FullName, string? PhoneNumber);
    public sealed record UpdateUserRequest(string Email, string FullName, string? PhoneNumber, DateOnly? BirthDate, string? GuardianName, string? GuardianEmail);

    // ---- The signed-in user, any role ----

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(ProfileRequest r, CancellationToken ct)
    {
        await mediator.Send(new UpdateMyProfileCommand(User.UserId(), r.FullName, r.PhoneNumber, r.BirthDate, r.GuardianName, r.GuardianEmail), ct);
        return NoContent();
    }

    // ---- Super admin ----

    /// <summary><paramref name="role"/>: SuperAdmin or an event role. <paramref name="status"/>: Pending, Active or Inactive.</summary>
    [HttpGet, Authorize(Policy = AppClaims.SuperAdminPolicy)]
    public async Task<PagedResult<UserSummaryDto>> List(string? search, string? role, string? status, CancellationToken ct, int page = 1, int pageSize = 20) =>
        await mediator.Send(new ListUsersQuery(search, role, status, page, pageSize), ct);

    [HttpGet("{id:guid}"), Authorize(Policy = AppClaims.SuperAdminPolicy)]
    public async Task<UserDto> Get(Guid id, CancellationToken ct) => await mediator.Send(new GetUserQuery(id), ct);

    [HttpPost, Authorize(Policy = AppClaims.SuperAdminPolicy)]
    public async Task<IActionResult> Create(CreateUserRequest r, CancellationToken ct)
    {
        var id = await mediator.Send(new CreateUserCommand(r.Email, r.FullName, r.PhoneNumber), ct);
        return Created($"/api/users/{id}", new { id });
    }

    [HttpPut("{id:guid}"), Authorize(Policy = AppClaims.SuperAdminPolicy)]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest r, CancellationToken ct)
    {
        await mediator.Send(new UpdateUserCommand(id, r.Email, r.FullName, r.PhoneNumber, r.BirthDate, r.GuardianName, r.GuardianEmail), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate"), Authorize(Policy = AppClaims.SuperAdminPolicy)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await mediator.Send(new ActivateUserCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate"), Authorize(Policy = AppClaims.SuperAdminPolicy)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeactivateUserCommand(id, User.UserId()), ct);
        return NoContent();
    }
}
