namespace FeriaAcademica.Application.Features.Events.Queries.ListEvents;

/// <param name="SubmissionDeadline">End of the reception stage: works cannot be created, attached, submitted or withdrawn after it. Null when none is set.</param>
public sealed record EventDto(Guid Id, Guid InstitutionId, string Name, string Status, DateTime? StartDate, DateTime? EndDate, DateTime? SubmissionDeadline);
