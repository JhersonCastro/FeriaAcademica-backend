using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.StartReview;

public sealed record StartReviewCommand(Guid WorkId, Guid ReviewerId) : ICommand<Unit>;
