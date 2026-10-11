using FeriaAcademica.Domain.Common;

namespace FeriaAcademica.Domain.Evaluation;

public enum InstrumentType { Rubric, Rating, Survey }
public enum EvaluatorGroup { Jury, Attendees }

public sealed class Instrument : Entity
{
    private readonly List<Criterion> _criteria = [];

    public Guid EventId { get; private set; }
    public InstrumentType Type { get; private set; }
    public EvaluatorGroup Group { get; private set; }
    public string Name { get; private set; } = default!;
    public int Version { get; private set; } = 1;
    public bool IsCurrent { get; private set; } = true;
    public IReadOnlyCollection<Criterion> Criteria => _criteria;

    private Instrument() { } // EF Core

    public static Instrument Create(Guid eventId, InstrumentType type, EvaluatorGroup group, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
        return new Instrument { EventId = eventId, Type = type, Group = group, Name = name.Trim() };
    }

    public Criterion AddCriterion(string text, double weight = 1, double scaleMin = 1, double scaleMax = 5, string? optionsJson = null)
    {
        var criterion = Criterion.Create(Id, _criteria.Count + 1, text, weight, scaleMin, scaleMax, optionsJson);
        _criteria.Add(criterion);
        return criterion;
    }
}

public sealed class Criterion : Entity
{
    public Guid InstrumentId { get; private set; }
    public int Order { get; private set; }
    public string Text { get; private set; } = default!;
    public double Weight { get; private set; }
    public double ScaleMin { get; private set; }
    public double ScaleMax { get; private set; }
    public string? OptionsJson { get; private set; }

    private Criterion() { } // EF Core

    internal static Criterion Create(Guid instrumentId, int order, string text, double weight, double scaleMin, double scaleMax, string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new DomainException("Criterion text is required.");
        if (weight <= 0) throw new DomainException("Criterion weight must be greater than zero.");
        if (scaleMax <= scaleMin) throw new DomainException("The scale maximum must be greater than the minimum.");
        return new Criterion
        {
            InstrumentId = instrumentId, Order = order, Text = text.Trim(), Weight = weight,
            ScaleMin = scaleMin, ScaleMax = scaleMax, OptionsJson = optionsJson,
        };
    }
}

public sealed class Response : Entity
{
    public Guid CriterionId { get; private set; }
    public Guid WorkId { get; private set; }
    public Guid EvaluatorId { get; private set; }
    public double Value { get; private set; }
    public string? Comment { get; private set; }
    public DateTime At { get; private set; }

    private Response() { } // EF Core

    public static Response Create(Criterion criterion, Guid workId, Guid evaluatorId, double value, string? comment, DateTime now)
    {
        if (value < criterion.ScaleMin || value > criterion.ScaleMax) throw new DomainException("The value is outside the criterion scale.");
        return new Response { CriterionId = criterion.Id, WorkId = workId, EvaluatorId = evaluatorId, Value = value, Comment = comment, At = now };
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
