using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Auth;
using FeriaAcademica.Application.Eventos.Commands;
using FeriaAcademica.Application.Eventos.Queries;
using FeriaAcademica.Domain.Eventos;
using Microsoft.Extensions.DependencyInjection;

namespace FeriaAcademica.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection s)
    {
        s.AddScoped<IDispatcher, Dispatcher>();
        // Registro explícito de handlers (para el concepto basta; luego puede automatizarse con escaneo de ensamblado).
        s.AddScoped<ICommandHandler<CrearEventoCommand, Guid>, CrearEventoHandler>();
        s.AddScoped<ICommandHandler<AprobarEventoCommand, bool>, AprobarEventoHandler>();
        s.AddScoped<IQueryHandler<ListarEventosQuery, IReadOnlyList<EventoDto>>, ListarEventosHandler>();
        s.AddScoped<ICommandHandler<RegistrarUsuarioCommand, UsuarioDto>, AuthHandlers>();
        s.AddScoped<ICommandHandler<IniciarSesionCommand, UsuarioDto>, AuthHandlers>();
        s.AddScoped<ICommandHandler<IniciarSesionExternaCommand, UsuarioDto>, AuthHandlers>();
        s.AddScoped<ICommandHandler<CerrarSesionCommand, bool>, AuthHandlers>();
        return s;
    }
}
