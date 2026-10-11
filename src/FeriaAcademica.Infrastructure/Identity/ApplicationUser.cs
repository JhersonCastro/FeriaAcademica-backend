using Microsoft.AspNetCore.Identity;

namespace FeriaAcademica.Infrastructure.Identity;

public enum UserStatus { Pending, Active, Blocked }

/// <summary>The only user in the system: Identity owns credentials, this adds the platform's profile data.</summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianEmail { get; set; }
    public bool IsSuperAdmin { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
