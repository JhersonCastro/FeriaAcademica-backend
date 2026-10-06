using System.Security.Claims;
using FeriaAcademica.Application.Auth;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace FeriaAcademica.Infrastructure.Auth;

/// <summary>Autenticación basada en cookie de sesión de ASP.NET Core Identity (SignInManager).</summary>
internal sealed class AuthService(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) : IAuthService
{
    public async Task<UsuarioDto> RegistrarAsync(string email, string password, string nombre, CancellationToken ct)
    {
        var user = new ApplicationUser { UserName = email, Email = email, NombreCompleto = nombre };
        Validar(await users.CreateAsync(user, password));
        await signIn.SignInAsync(user, isPersistent: false);
        return ADto(user);
    }

    public async Task<UsuarioDto> LoginAsync(string email, string password, bool recordar, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(email) ?? throw new DomainException("Correo o contraseña incorrectos.");
        var r = await signIn.PasswordSignInAsync(user, password, recordar, lockoutOnFailure: true);
        if (r.IsLockedOut) throw new DomainException("Cuenta bloqueada temporalmente por intentos fallidos.");
        if (!r.Succeeded) throw new DomainException("Correo o contraseña incorrectos.");
        return ADto(user);
    }

    public async Task<UsuarioDto> LoginExternoAsync(CancellationToken ct)
    {
        var info = await signIn.GetExternalLoginInfoAsync() ?? throw new DomainException("No se pudo completar el inicio de sesión externo.");
        var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? throw new DomainException("El proveedor no entregó un correo.");

        var user = await users.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (user is null)
        {
            user = await users.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email, Email = email, EmailConfirmed = true,
                    NombreCompleto = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email,
                };
                Validar(await users.CreateAsync(user));
            }
            Validar(await users.AddLoginAsync(user, info)); // vincula Google al usuario local
        }
        await signIn.SignInAsync(user, isPersistent: false, info.LoginProvider);
        return ADto(user);
    }

    public Task CerrarSesionAsync() => signIn.SignOutAsync();

    private static UsuarioDto ADto(ApplicationUser u) => new(u.Id, u.Email!, u.NombreCompleto);
    private static void Validar(IdentityResult r)
    {
        if (!r.Succeeded) throw new DomainException(string.Join(" ", r.Errors.Select(e => e.Description)));
    }
}
