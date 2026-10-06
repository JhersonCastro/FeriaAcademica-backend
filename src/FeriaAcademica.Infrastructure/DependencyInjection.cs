using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Auth;
using FeriaAcademica.Application.Events.Queries;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Infrastructure.Auth;
using FeriaAcademica.Infrastructure.Identity;
using FeriaAcademica.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FeriaAcademica.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection s, IConfiguration config)
    {
        s.AddDbContext<AppDbContext>(o => o.UseNpgsql(config.GetConnectionString("Default")));
        s.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        s.AddScoped<IAcademicEventRepository, AcademicEventRepository>();
        s.AddScoped<IEventReadModel, EventReadModel>();

        s.AddIdentityCore<ApplicationUser>(o => { o.User.RequireUniqueEmail = true; o.Password.RequiredLength = 8; })
         .AddEntityFrameworkStores<AppDbContext>()
         .AddSignInManager();

        s.AddScoped<IAuthService, AuthService>();
        return s;
    }
}
