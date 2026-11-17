using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Institutions;
using Mediator;

namespace FeriaAcademica.Application.Features.Institutions.Commands.CreateInstitution;

internal sealed class CreateInstitutionHandler(IInstitutionRepository institutions, IUnitOfWork uow) : ICommandHandler<CreateInstitutionCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateInstitutionCommand c, CancellationToken ct)
    {
        var institution = Institution.Create(c.Name, c.Acronym);
        if (await institutions.NameExistsAsync(institution.Name, null, ct)) throw new DomainException("An institution with this name already exists.");
        await institutions.AddAsync(institution, ct);
        await uow.SaveChangesAsync(ct);
        return institution.Id;
    }
}
