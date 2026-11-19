using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Institutions;
using Mediator;

namespace FeriaAcademica.Application.Features.Institutions.Commands.UpdateInstitution;

internal sealed class UpdateInstitutionHandler(IInstitutionRepository institutions, IUnitOfWork uow) : ICommandHandler<UpdateInstitutionCommand, bool>
{
    public async ValueTask<bool> Handle(UpdateInstitutionCommand c, CancellationToken ct)
    {
        var institution = await institutions.GetAsync(c.Id, ct);
        if (institution is null) return false;
        institution.Update(c.Name, c.Acronym);
        if (await institutions.NameExistsAsync(institution.Name, institution.Id, ct)) throw new DomainException("An institution with this name already exists.");
        await uow.SaveChangesAsync(ct);
        return true;
    }
}
