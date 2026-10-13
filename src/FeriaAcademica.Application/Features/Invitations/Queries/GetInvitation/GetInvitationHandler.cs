using FeriaAcademica.Domain.Common;
using Mediator;

namespace FeriaAcademica.Application.Features.Invitations.Queries.GetInvitation;

internal sealed class GetInvitationHandler(IInvitationReadModel invitations) : IQueryHandler<GetInvitationQuery, InvitationDto>
{
    public async ValueTask<InvitationDto> Handle(GetInvitationQuery q, CancellationToken ct) =>
        await invitations.GetByTokenAsync(q.Token, DateTime.UtcNow, ct) ?? throw new DomainException("Invitation not found.");
}
