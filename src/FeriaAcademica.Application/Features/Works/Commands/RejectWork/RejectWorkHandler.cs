using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.RejectWork;

internal sealed class RejectWorkHandler(IWorkRepository works, IUnitOfWork uow) : ICommandHandler<RejectWorkCommand, Unit>
{
    public async ValueTask<Unit> Handle(RejectWorkCommand c, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(c.Observations)) throw new DomainException("Observations are required to reject a work.");

        var work = await works.GetAsync(c.WorkId, ct) ?? throw new DomainException("Work not found.");
        work.Reject(c.ReviewerId, c.Observations.Trim(), DateTime.UtcNow);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
