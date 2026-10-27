using FeriaAcademica.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace FeriaAcademica.Infrastructure.Identity;

/// <summary>The only user in the system: Identity owns credentials, this adds the platform's profile data.</summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianEmail { get; set; }
    public bool IsSuperAdmin { get; set; }   // the only global role; every other role belongs to an event
    public UserStatus Status { get; set; } = UserStatus.Active;   // decided by Domain.Users.UserAccount
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
