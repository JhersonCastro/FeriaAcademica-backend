namespace FeriaAcademica.Application.Features.Events.Queries.GetMyParticipation;

public interface IParticipationReadModel { Task<ParticipationDto?> GetAsync(Guid eventId, Guid userId, CancellationToken ct); }
