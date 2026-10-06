using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Application.Auth;
using FeriaAcademica.Application.Events.Commands;
using FeriaAcademica.Application.Events.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace FeriaAcademica.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection s)
    {
        s.AddScoped<IDispatcher, Dispatcher>();
        s.AddScoped<ICommandHandler<CreateEventCommand, Guid>, CreateEventHandler>();
        s.AddScoped<ICommandHandler<ApproveEventCommand, bool>, ApproveEventHandler>();
        s.AddScoped<IQueryHandler<ListEventsQuery, IReadOnlyList<EventDto>>, ListEventsHandler>();
        s.AddScoped<ICommandHandler<RegisterUserCommand, UserDto>, AuthHandlers>();
        s.AddScoped<ICommandHandler<SignInCommand, UserDto>, AuthHandlers>();
        s.AddScoped<ICommandHandler<ExternalSignInCommand, UserDto>, AuthHandlers>();
        s.AddScoped<ICommandHandler<SignOutCommand, bool>, AuthHandlers>();
        return s;
    }
}
