using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Institutions;
using Mediator;

namespace FeriaAcademica.Application.Features.Institutions.Commands.ActivateInstitution;

internal sealed class ActivateInstitutionHandler(IInstitutionRepository institutions, IUnitOfWork uow) : ICommandHandler<ActivateInstitutionCommand, bool>
{
    public async ValueTask<bool> Handle(ActivateInstitutionCommand c, CancellationToken ct)
    {
        var institution = await institutions.GetAsync(c.Id, ct);
        if (institution is null) return false;
        institution.Activate();
        await uow.SaveChangesAsync(ct);
        return true;
    }
}
