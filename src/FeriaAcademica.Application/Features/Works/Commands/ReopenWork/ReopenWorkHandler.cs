using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.ReopenWork;

internal sealed class ReopenWorkHandler(IWorkRepository works, IUnitOfWork uow) : ICommandHandler<ReopenWorkCommand, Unit>
{
    public async ValueTask<Unit> Handle(ReopenWorkCommand c, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(c.Reason)) throw new DomainException("A reason is required to reopen a work.");

        var work = await works.GetAsync(c.WorkId, ct) ?? throw new DomainException("Work not found.");
        work.Reopen(c.ActorId, c.Reason.Trim(), DateTime.UtcNow);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
