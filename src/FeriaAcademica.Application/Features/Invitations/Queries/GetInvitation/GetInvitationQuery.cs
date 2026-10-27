using Mediator;

namespace FeriaAcademica.Application.Features.Invitations.Queries.GetInvitation;

public sealed record GetInvitationQuery(string Token) : IQuery<InvitationDto>;
