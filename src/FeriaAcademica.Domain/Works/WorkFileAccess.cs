using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Domain.Works;

/// <summary>
/// Who may read the files uploaded to a work: its author, the professor who reviews it, the coordinators of its event, the jury members
/// assigned to it and the super admin. Being a coordinator or a jury member of the event is not enough on its own for the jury:
/// they need to be assigned to that work.
/// </summary>
public static class WorkFileAccess
{
    public static bool CanRead(Guid userId, Guid authorId, Guid reviewerId, bool isSuperAdmin, IReadOnlyCollection<EventRole> eventRoles, bool isAssignedJury) =>
        isSuperAdmin || userId == authorId || userId == reviewerId || eventRoles.Contains(EventRole.Coordinator) || isAssignedJury;
}
