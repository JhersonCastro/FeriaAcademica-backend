using Microsoft.AspNetCore.Identity;

namespace FeriaAcademica.Infrastructure.Identity;

/// <summary>Usuario de ASP.NET Core Identity. Se extiende aquí con los datos propios de la plataforma.</summary>
public class ApplicationUser : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;
}
