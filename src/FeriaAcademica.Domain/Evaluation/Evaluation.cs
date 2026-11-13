using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Events;
using FeriaAcademica.Domain.Works;

namespace FeriaAcademica.Domain.Evaluation;

public enum InstrumentType { Rubric, Rating, Survey }
public enum EvaluatorGroup { Jury, Attendees }
/// <summary>How a question is answered: a number on its scale, yes (1) or no (0), or a written answer.</summary>
public enum CriterionType { Scale, YesNo, Text }

/// <summary>A question as the administrator sends it: <paramref name="Id"/> is null for a new one.</summary>
public sealed record QuestionDraft(Guid? Id, CriterionType Type, string Text, double? ScaleMin, double? ScaleMax);

/// <summary>An answer as the evaluator sends it: a number for scale and yes/no questions, a text for written ones.</summary>
public sealed record AnswerDraft(Guid QuestionId, double? Value, string? Text);

/// <summary>A scoring instrument of an event (rubric, rating or survey) for one group of evaluators, made of criteria.</summary>
public sealed class Instrument : Entity
{
    public const int MaxQuestions = 20;

    private readonly List<Criterion> _criteria = [];

    public Guid EventId { get; private set; }
    public InstrumentType Type { get; private set; }
    public EvaluatorGroup Group { get; private set; }
    public string Name { get; private set; } = default!;
    public int Version { get; private set; } = 1;
    public bool IsCurrent { get; private set; } = true;
    public DateTime? UpdatedAt { get; private set; }   // last time its questions were saved
    public IReadOnlyCollection<Criterion> Criteria => _criteria;

    private Instrument() { } // EF Core

    public static Instrument Create(Guid eventId, InstrumentType type, EvaluatorGroup group, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
        return new Instrument { EventId = eventId, Type = type, Group = group, Name = name.Trim() };
    }

    public Criterion AddCriterion(string text, double weight = 1, double scaleMin = 1, double scaleMax = 5, string? optionsJson = null)
    {
        var criterion = Criterion.Create(Id, _criteria.Count + 1, CriterionType.Scale, text, weight, scaleMin, scaleMax, optionsJson);
        _criteria.Add(criterion);
        return criterion;
    }

    /// <summary>
    /// Replaces the whole list of questions: the order of <paramref name="questions"/> is the question order, the ones with an id are edited in
    /// place and the rest are new. Once somebody has answered the questions are fixed. Returns the questions that are no longer in the list;
    /// the caller deletes them (they stay in <see cref="Criteria"/> until the change is saved).
    /// </summary>
    public IReadOnlyList<Criterion> ReplaceQuestions(IReadOnlyList<QuestionDraft> questions, bool hasAnswers, DateTime now)
    {
        if (hasAnswers) throw new DomainException("The evaluation can no longer be changed.");
        if (questions.Count > MaxQuestions) throw new DomainException($"An evaluation can have at most {MaxQuestions} questions.");

        var current = _criteria.ToDictionary(c => c.Id);
        var kept = new HashSet<Guid>();
        for (var i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            if (q.Id is { } id)
            {
                if (!current.TryGetValue(id, out var existing)) throw new DomainException("A question does not belong to this evaluation.");
                if (!kept.Add(id)) throw new DomainException("A question appears twice in the list.");
                existing.Update(i + 1, q.Type, q.Text, q.ScaleMin, q.ScaleMax);
            }
            else
            {
                _criteria.Add(Criterion.Create(Id, i + 1, q.Type, q.Text, 1, q.ScaleMin, q.ScaleMax, null));
            }
        }

        UpdatedAt = now;
        return _criteria.Where(c => current.ContainsKey(c.Id) && !kept.Contains(c.Id)).ToList();
    }

    /// <summary>One response per answered question; every answer must belong to this instrument, appear once and fit its question.</summary>
    public IReadOnlyList<Response> BuildResponses(Guid workId, Guid evaluatorId, IReadOnlyList<AnswerDraft> answers, DateTime now)
    {
        if (answers.Count == 0) throw new DomainException("Answer at least one question.");
        if (answers.Select(a => a.QuestionId).Distinct().Count() != answers.Count) throw new DomainException("A question appears twice in the list.");
        return answers.Select(a =>
        {
            var criterion = _criteria.FirstOrDefault(c => c.Id == a.QuestionId) ?? throw new DomainException("A question does not belong to this evaluation.");
            return Response.Create(criterion, workId, evaluatorId, a.Value, a.Text, now);
        }).ToList();
    }
}

/// <summary>One question of an instrument, with its weight and scale.</summary>
public sealed class Criterion : Entity
{
    public const int MaxTextLength = 300;
    public const int MaxAnswerLength = 1000;

    public Guid InstrumentId { get; private set; }
    public int Order { get; private set; }
    public CriterionType Type { get; private set; }
    public string Text { get; private set; } = default!;
    public double Weight { get; private set; }
    public double ScaleMin { get; private set; }
    public double ScaleMax { get; private set; }
    public string? OptionsJson { get; private set; }

    private Criterion() { } // EF Core

    internal static Criterion Create(Guid instrumentId, int order, CriterionType type, string text, double weight, double? scaleMin, double? scaleMax, string? optionsJson)
    {
        if (weight <= 0) throw new DomainException("Criterion weight must be greater than zero.");
        var criterion = new Criterion { InstrumentId = instrumentId, Weight = weight, OptionsJson = optionsJson };
        criterion.Update(order, type, text, scaleMin, scaleMax);
        return criterion;
    }

    /// <summary>Only a scale question has a scale of its own; a yes/no question is 0 or 1 and a written one does not use it.</summary>
    internal void Update(int order, CriterionType type, string text, double? scaleMin, double? scaleMax)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new DomainException("Criterion text is required.");
        if (text.Trim().Length > MaxTextLength) throw new DomainException($"A question can have at most {MaxTextLength} characters.");
        if (type == CriterionType.Scale)
        {
            if (scaleMin is null || scaleMax is null) throw new DomainException("A scale question needs its minimum and maximum.");
            if (scaleMax <= scaleMin) throw new DomainException("The scale maximum must be greater than the minimum.");
        }

        Order = order;
        Type = type;
        Text = text.Trim();
        (ScaleMin, ScaleMax) = type == CriterionType.Scale ? (scaleMin!.Value, scaleMax!.Value) : (0, 1);
    }

    /// <summary>The answer must fit the question: a number inside the scale, 1 or 0, or a written text (and nothing else).</summary>
    internal void EnsureAccepts(double? value, string? text)
    {
        if (Type == CriterionType.Text)
        {
            if (value is not null) throw new DomainException("The answer does not match the question type.");
            if (string.IsNullOrWhiteSpace(text)) throw new DomainException("The answer does not match the question type.");
            if (text.Trim().Length > MaxAnswerLength) throw new DomainException($"A written answer can have at most {MaxAnswerLength} characters.");
            return;
        }

        if (value is null || text is not null) throw new DomainException("The answer does not match the question type.");
        var fits = Type == CriterionType.YesNo ? value is 0 or 1 : value >= ScaleMin && value <= ScaleMax && value == Math.Floor(value.Value);
        if (!fits) throw new DomainException("The value is outside the criterion scale.");
    }
}

/// <summary>An evaluator's answer to one question about a work; the number (or the written text) must fit the question.</summary>
public sealed class Response : Entity
{
    public Guid CriterionId { get; private set; }
    public Guid WorkId { get; private set; }
    public Guid EvaluatorId { get; private set; }
    public double? Value { get; private set; }   // null for a written answer
    public string? Comment { get; private set; } // the written answer
    public DateTime At { get; private set; }

    private Response() { } // EF Core

    public static Response Create(Criterion criterion, Guid workId, Guid evaluatorId, double? value, string? text, DateTime now)
    {
        criterion.EnsureAccepts(value, text);
        return new Response { CriterionId = criterion.Id, WorkId = workId, EvaluatorId = evaluatorId, Value = value, Comment = text?.Trim(), At = now };
    }
}

/// <summary>Final result of a work; its key is the work itself.</summary>
public sealed class WorkResult
{
    public Guid WorkId { get; private set; }
    public double? JuryScore { get; private set; }
    public double? AttendeeScore { get; private set; }
    public double FinalScore { get; private set; }
    public int? Position { get; private set; }
    public DateTime CalculatedAt { get; private set; }

    private WorkResult() { } // EF Core

    public static WorkResult Create(Guid workId, double? juryScore, double? attendeeScore, double finalScore, int? position, DateTime now) =>
        new() { WorkId = workId, JuryScore = juryScore, AttendeeScore = attendeeScore, FinalScore = finalScore, Position = position, CalculatedAt = now };
}

/// <summary>Who defines the evaluation of an event and who may answer it.</summary>
public static class EvaluationPolicy
{
    public static void EnsureCanManage(bool isSuperAdmin, IReadOnlyCollection<EventRole> eventRoles)
    {
        if (isSuperAdmin || eventRoles.Contains(EventRole.Coordinator) || eventRoles.Contains(EventRole.Teacher)) return;
        throw new DomainException("Only the super admin or a coordinator or teacher of the event can manage its evaluation.");
    }

    /// <summary>Only people who attended can evaluate, once the event has finished, the works of others that reached the fair.</summary>
    public static void EnsureCanAnswer(AcademicEvent academicEvent, bool attended, Work work, bool isOwnWork)
    {
        if (!academicEvent.IsFinished) throw new DomainException("The evaluation opens when the event is closed.");
        if (!attended) throw new DomainException("Only people who attended the event can evaluate its works.");
        if (isOwnWork) throw new DomainException("You cannot evaluate your own work.");
        if (!work.IsEvaluable) throw new DomainException("This work cannot be evaluated.");
    }
}

/// <summary>Persistence of the evaluation of an event; implemented in Infrastructure.</summary>
public interface IEvaluationRepository
{
    /// <summary>The event's questionnaire with its questions, or null when none was defined.</summary>
    Task<Instrument?> GetByEventAsync(Guid eventId, CancellationToken ct);
    Task AddAsync(Instrument instrument, CancellationToken ct);
    Task<bool> HasResponsesAsync(Guid instrumentId, CancellationToken ct);
    /// <summary>The evaluator's previous answers about the work, to be replaced.</summary>
    Task<IReadOnlyList<Response>> GetResponsesAsync(Guid instrumentId, Guid workId, Guid evaluatorId, CancellationToken ct);
    Task AddResponsesAsync(IEnumerable<Response> responses, CancellationToken ct);
    void RemoveResponses(IEnumerable<Response> responses);
    void RemoveCriteria(IEnumerable<Criterion> criteria);
}
