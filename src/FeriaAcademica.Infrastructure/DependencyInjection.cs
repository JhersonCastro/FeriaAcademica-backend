using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Features.Auth;
using FeriaAcademica.Application.Features.Attendance.Queries.GetAttendanceSummary;
using FeriaAcademica.Application.Features.Events.Queries.ListEvents;
using FeriaAcademica.Application.Features.Invitations.Queries.GetInvitation;
using FeriaAcademica.Application.Features.Registrations.Queries.GetMyRegistration;
using FeriaAcademica.Application.Features.Users;
using FeriaAcademica.Application.Features.Users.Queries.GetUser;
using FeriaAcademica.Application.Features.Users.Queries.ListUsers;
using FeriaAcademica.Application.Features.Works.Queries.GetWork;
using FeriaAcademica.Application.Features.Works.Queries.ListMyWorks;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Registrations;
using FeriaAcademica.Domain.Works;
using FeriaAcademica.Infrastructure.Auth;
using FeriaAcademica.Infrastructure.Email;
using FeriaAcademica.Infrastructure.Identity;
using FeriaAcademica.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FeriaAcademica.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection s, IConfiguration config)
    {
        s.AddDbContext<AppDbContext>(o => o.UseNpgsql(config.GetConnectionString("Default")));
        s.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        s.AddScoped<IAcademicEventRepository, AcademicEventRepository>();
        s.AddScoped<IEventReadModel, EventReadModel>();
        s.AddScoped<IRoleAssignmentRepository, RoleAssignmentRepository>();
        s.AddScoped<IInvitationRepository, InvitationRepository>();
        s.AddScoped<IInvitationReadModel, InvitationReadModel>();
        s.AddScoped<UserReadModel>();
        s.AddScoped<IUserReadModel>(sp => sp.GetRequiredService<UserReadModel>());
        s.AddScoped<IUserListReadModel>(sp => sp.GetRequiredService<UserReadModel>());
        s.AddScoped<IRegistrationRepository, RegistrationRepository>();
        s.AddScoped<IRegistrationReadModel, RegistrationReadModel>();
        s.AddScoped<IAttendanceRepository, AttendanceRepository>();
        s.AddScoped<IAttendanceReadModel, AttendanceReadModel>();
        s.AddScoped<IWorkRepository, WorkRepository>();
        s.AddScoped<IWorkReadModel, WorkReadModel>();
        s.AddScoped<IMyWorkReadModel, MyWorkReadModel>();
        s.AddScoped<IParticipationRepository, ParticipationRepository>();

        s.AddIdentityCore<ApplicationUser>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 8;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

        // Password links (set password, recovery) expire after a day.
        s.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(24));
        // The cookie is re-checked against the security stamp every minute, so a deactivated user loses open sessions quickly.
        s.TryAddScoped<ISecurityStampValidator, SecurityStampValidator<ApplicationUser>>();
        s.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

        s.AddScoped<IAuthService, AuthService>();
        s.AddScoped<IUserAccounts, UserAccounts>();
        s.AddScoped<IAccountEmails, AccountEmails>();
        return s;
    }
}
