using Microsoft.AspNetCore.Identity;
using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Auth;
using FeriaAcademica.Application.Eventos.Queries;
using FeriaAcademica.Domain.Eventos;
using FeriaAcademica.Infrastructure.Auth;
using FeriaAcademica.Infrastructure.Identity;
using FeriaAcademica.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FeriaAcademica.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection s, IConfiguration config)
    {
        // SQLite para el concepto (cero configuración). Para producción: UseSqlServer / UseNpgsql.
        s.AddDbContext<AppDbContext>(o => o.UseSqlite(config.GetConnectionString("Default") ?? "Data Source=feria.db"));
        s.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        s.AddScoped<IEventoRepository, EventoRepository>();
        s.AddScoped<IEventoReadModel, EventoReadModel>();

        // ASP.NET Core Identity: ApplicationUser (extiende IdentityUser) + UserManager/SignInManager (sesión por cookie).
        s.AddIdentityCore<ApplicationUser>(o => { o.User.RequireUniqueEmail = true; o.Password.RequiredLength = 8; })
         .AddEntityFrameworkStores<AppDbContext>()
         .AddSignInManager();

        s.AddScoped<IAuthService, AuthService>();
        return s;
    }
}
