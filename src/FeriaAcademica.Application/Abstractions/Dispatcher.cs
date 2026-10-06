using Microsoft.Extensions.DependencyInjection;

namespace FeriaAcademica.Application.Abstractions;

internal sealed class Dispatcher(IServiceProvider services) : IDispatcher
{
    public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
    {
        dynamic handler = services.GetRequiredService(typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse)));
        return handler.Handle((dynamic)command, ct);
    }

    public Task<TResponse> Ask<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
    {
        dynamic handler = services.GetRequiredService(typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse)));
        return handler.Handle((dynamic)query, ct);
    }
}
