
namespace FeriaAcademica.Application.Features.Evaluation;

public sealed record EvaluationQuestionDto(Guid Id, int Order, string Type, string Text, double? ScaleMin, double? ScaleMax);

/// <summary>The questions of an event; <c>CanEdit</c> turns false once somebody answered.</summary>
public sealed record EventEvaluationDto(
    Guid EventId, string EventName, string EventStatus, bool CanEdit, int AnswersCount, DateTime? UpdatedAt, IReadOnlyList<EvaluationQuestionDto> Questions);

public sealed record EvaluableWorkDto(Guid WorkId, string Title, string? Summary, bool Answered);

/// <summary>A finished event the person attended that has questions, with the works they can evaluate.</summary>
public sealed record PendingEvaluationDto(Guid EventId, string EventName, int QuestionsCount, IReadOnlyList<EvaluableWorkDto> Works);

public sealed record EvaluationAnswerDto(Guid QuestionId, double? Value, string? Text);

public sealed record MyWorkAnswersDto(
    Guid EventId, string EventName, Guid WorkId, string WorkTitle, string? WorkSummary,
    IReadOnlyList<EvaluationQuestionDto> Questions, IReadOnlyList<EvaluationAnswerDto> Answers, DateTime? AnsweredAt);
