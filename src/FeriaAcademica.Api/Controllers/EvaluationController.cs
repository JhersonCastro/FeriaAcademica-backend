using System.Text.Json.Serialization;
using FeriaAcademica.Application.Features.Evaluation;
using FeriaAcademica.Application.Features.Evaluation.Commands.SaveEventEvaluation;
using FeriaAcademica.Application.Features.Evaluation.Commands.SaveMyAnswers;
using FeriaAcademica.Application.Features.Evaluation.Queries.GetEventEvaluation;
using FeriaAcademica.Application.Features.Evaluation.Queries.GetMyWorkAnswers;
using FeriaAcademica.Application.Features.Evaluation.Queries.ListPendingEvaluations;
using FeriaAcademica.Domain.Evaluation;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeriaAcademica.Api.Controllers;

/// <summary>HU-023: the questions an event defines, and the answers of those who attended about each work once the event is closed.</summary>
[ApiController, Route("api"), Authorize]
public class EvaluationController(IMediator mediator) : ControllerBase
{
    /// <summary>Id is null for a new question; ScaleMin and ScaleMax only matter for Scale questions.</summary>
    public sealed record QuestionRequest(Guid? Id, [property: JsonConverter(typeof(JsonStringEnumConverter))] CriterionType Type, string? Text, double? ScaleMin, double? ScaleMax);
    public sealed record SaveEvaluationRequest(IReadOnlyList<QuestionRequest>? Questions);
    public sealed record AnswerRequest(Guid QuestionId, double? Value, string? Text);
    public sealed record SaveAnswersRequest(IReadOnlyList<AnswerRequest>? Answers);

    /// <summary>The event's questions (an empty list when none were defined). Super admin, coordinators and teachers of the event.</summary>
    [HttpGet("events/{eventId:guid}/evaluation")]
    public async Task<IActionResult> Get(Guid eventId, CancellationToken ct) =>
        await mediator.Send(new GetEventEvaluationQuery(eventId, User.UserId(), User.IsSuperAdmin()), ct) is { } evaluation ? Ok(evaluation) : NotFound();

    /// <summary>Replaces the whole list of questions, in order. Allowed until somebody answers.</summary>
    [HttpPut("events/{eventId:guid}/evaluation")]
    public async Task<IActionResult> Save(Guid eventId, SaveEvaluationRequest r, CancellationToken ct)
    {
        var questions = (r.Questions ?? []).Select(q => new QuestionDraft(q.Id, q.Type, q.Text ?? "", q.ScaleMin, q.ScaleMax)).ToList();
        await mediator.Send(new SaveEventEvaluationCommand(eventId, User.UserId(), User.IsSuperAdmin(), questions), ct);
        return NoContent();
    }

    /// <summary>Finished events the signed-in person attended that have questions, with the works they can evaluate.</summary>
    [HttpGet("evaluations/pending")]
    public async Task<IReadOnlyList<PendingEvaluationDto>> Pending(CancellationToken ct) =>
        await mediator.Send(new ListPendingEvaluationsQuery(User.UserId()), ct);

    /// <summary>The questions about a work and the answers the signed-in person already gave.</summary>
    [HttpGet("events/{eventId:guid}/evaluation/works/{workId:guid}/answers/me")]
    public async Task<IActionResult> MyAnswers(Guid eventId, Guid workId, CancellationToken ct) =>
        await mediator.Send(new GetMyWorkAnswersQuery(eventId, workId, User.UserId()), ct) is { } answers ? Ok(answers) : NotFound();

    /// <summary>Replaces the signed-in person's answers about the work.</summary>
    [HttpPut("events/{eventId:guid}/evaluation/works/{workId:guid}/answers/me")]
    public async Task<IActionResult> SaveMyAnswers(Guid eventId, Guid workId, SaveAnswersRequest r, CancellationToken ct)
    {
        var answers = (r.Answers ?? []).Select(a => new AnswerDraft(a.QuestionId, a.Value, a.Text)).ToList();
        await mediator.Send(new SaveMyAnswersCommand(eventId, workId, User.UserId(), answers), ct);
        return NoContent();
    }
}
