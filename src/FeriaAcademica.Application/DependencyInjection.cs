using Microsoft.Extensions.DependencyInjection;

namespace FeriaAcademica.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection s)
    {
        // Mediator (martinothamar/Mediator, MIT): the source generator discovers every handler in this assembly.
        // Scoped, because the handlers depend on the scoped DbContext.
        s.AddMediator(o => o.ServiceLifetime = ServiceLifetime.Scoped);
        return s;
    }
}
