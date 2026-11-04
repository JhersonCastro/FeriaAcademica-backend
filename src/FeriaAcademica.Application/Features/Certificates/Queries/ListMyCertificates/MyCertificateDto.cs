namespace FeriaAcademica.Application.Features.Certificates.Queries.ListMyCertificates;

/// <summary>A certificate the person qualifies for. <c>Available</c> turns true once the event has finished; <c>Code</c> is set once it was issued.</summary>
public sealed record MyCertificateDto(Guid EventId, string EventName, string Type, Guid? WorkId, string? WorkTitle, bool Available, string? Code);
