using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Institutions;
using Mediator;

namespace FeriaAcademica.Application.Features.Institutions.Commands.DeactivateInstitution;

internal sealed class DeactivateInstitutionHandler(IInstitutionRepository institutions, IUnitOfWork uow) : ICommandHandler<DeactivateInstitutionCommand, bool>
{
    public async ValueTask<bool> Handle(DeactivateInstitutionCommand c, CancellationToken ct)
    {
        var institution = await institutions.GetAsync(c.Id, ct);
        if (institution is null) return false;
        institution.Deactivate();
        await uow.SaveChangesAsync(ct);
        return true;
    }
}
