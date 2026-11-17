namespace FeriaAcademica.Domain.Institutions;

/// <summary>Persistence of institutions; implemented in Infrastructure.</summary>
public interface IInstitutionRepository
{
    Task<Institution?> GetAsync(Guid id, CancellationToken ct);
    Task AddAsync(Institution institution, CancellationToken ct);
    /// <summary>Whether another institution already has this name, ignoring case. <paramref name="exceptId"/> is the one being edited.</summary>
    Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken ct);
}
