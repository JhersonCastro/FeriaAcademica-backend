using FeriaAcademica.Application.Abstractions;
using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.StartReview;

internal sealed class StartReviewHandler(IWorkRepository works, IUnitOfWork uow) : ICommandHandler<StartReviewCommand, Unit>
{
    public async ValueTask<Unit> Handle(StartReviewCommand c, CancellationToken ct)
    {
        var work = await works.GetAsync(c.WorkId, ct) ?? throw new DomainException("Work not found.");
        work.StartReview(c.ReviewerId, DateTime.UtcNow);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
