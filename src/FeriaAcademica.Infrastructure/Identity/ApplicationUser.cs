using Microsoft.AspNetCore.Identity;

namespace FeriaAcademica.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
