namespace FeriaAcademica.Domain.Works;

/// <summary>Who may read the files uploaded to a work: its author, the professor who reviews it and the super admin.</summary>
public static class WorkFileAccess
{
    public static bool CanRead(Guid userId, Guid authorId, Guid reviewerId, bool isSuperAdmin) =>
        isSuperAdmin || userId == authorId || userId == reviewerId;
}
