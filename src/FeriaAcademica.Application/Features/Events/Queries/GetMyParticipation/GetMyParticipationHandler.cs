using Mediator;

namespace FeriaAcademica.Application.Features.Events.Queries.GetMyParticipation;

internal sealed class GetMyParticipationHandler(IParticipationReadModel readModel) : IQueryHandler<GetMyParticipationQuery, ParticipationDto?>
{
    public async ValueTask<ParticipationDto?> Handle(GetMyParticipationQuery q, CancellationToken ct) => await readModel.GetAsync(q.EventId, q.UserId, ct);
}
