using FeriaAcademica.Domain.Works;
using Mediator;

namespace FeriaAcademica.Application.Features.Works.Commands.DecideReview;

public sealed record DecideReviewCommand(Guid WorkId, Guid ReviewerId, ReviewDecision Decision, string? Feedback) : ICommand<Unit>;
