namespace FeriaAcademica.Application.Features.Events.Queries.ListEvents;

public sealed record EventDto(Guid Id, Guid InstitutionId, string Name, string Status, DateTime? StartDate, DateTime? EndDate, int? Capacity, int Registered, int? AvailableSeats);
