namespace FeriaAcademica.Application.Abstractions;

public interface ICommand<TResponse>;
public interface IQuery<TResponse>;

public interface ICommandHandler<TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<TResponse> Handle(TCommand command, CancellationToken ct);
}

public interface IQueryHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<TResponse> Handle(TQuery query, CancellationToken ct);
}

public interface IDispatcher
{
    Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default);
    Task<TResponse> Ask<TResponse>(IQuery<TResponse> query, CancellationToken ct = default);
}

public interface IUnitOfWork { Task<int> SaveChangesAsync(CancellationToken ct); }
