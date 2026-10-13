namespace FeriaAcademica.Application.Features.Invitations.Queries.GetInvitation;

public interface IInvitationReadModel
{
    Task<InvitationDto?> GetByTokenAsync(string token, DateTime now, CancellationToken ct);
}
