namespace FeriaAcademica.Application.Features.Works.Queries.ListMyWorks;

public interface IMyWorkReadModel { Task<IReadOnlyList<MyWorkDto>> ListAsync(Guid authorId, CancellationToken ct); }
