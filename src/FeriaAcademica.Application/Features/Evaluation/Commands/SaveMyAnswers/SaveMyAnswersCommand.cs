using FeriaAcademica.Domain.Evaluation;
using Mediator;

namespace FeriaAcademica.Application.Features.Evaluation.Commands.SaveMyAnswers;

/// <summary>Replaces the answers the person gave before about the work.</summary>
public sealed record SaveMyAnswersCommand(Guid EventId, Guid WorkId, Guid UserId, IReadOnlyList<AnswerDraft> Answers) : ICommand<Unit>;
