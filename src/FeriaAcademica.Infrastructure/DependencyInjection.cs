using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Features.Auth;
using FeriaAcademica.Application.Features.Events.Queries.ListEvents;
using FeriaAcademica.Application.Features.Works.Queries.GetWork;
using FeriaAcademica.Application.Features.Works.Queries.ListMyWorks;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Works;
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
        s.AddScoped<IWorkRepository, WorkRepository>();
        s.AddScoped<IWorkReadModel, WorkReadModel>();
        s.AddScoped<IMyWorkReadModel, MyWorkReadModel>();
        s.AddScoped<IParticipationRepository, ParticipationRepository>();

        s.AddIdentityCore<ApplicationUser>(o => { o.User.RequireUniqueEmail = true; o.Password.RequiredLength = 8; })
         .AddEntityFrameworkStores<AppDbContext>()
         .AddSignInManager();

        s.AddScoped<IAuthService, AuthService>();
        return s;
    }
}
