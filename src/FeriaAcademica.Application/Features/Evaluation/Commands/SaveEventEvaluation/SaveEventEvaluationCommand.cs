using FeriaAcademica.Domain.Evaluation;
using Mediator;

namespace FeriaAcademica.Application.Features.Evaluation.Commands.SaveEventEvaluation;

/// <summary>Replaces the whole list of questions of the event; the list order is the question order.</summary>
public sealed record SaveEventEvaluationCommand(Guid EventId, Guid ActorId, bool ActorIsSuperAdmin, IReadOnlyList<QuestionDraft> Questions) : ICommand<Unit>;
