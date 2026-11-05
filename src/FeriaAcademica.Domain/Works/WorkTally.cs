namespace FeriaAcademica.Domain.Works;

/// <summary>
/// How the works an event received add up for its report. A work counts as received once it has been submitted at least once;
/// every received work falls in exactly one group, so the groups always add up to <see cref="Received"/>.
/// </summary>
public sealed record WorkTally(int Received, int Approved, int Rejected, int InReview, int ReturnedForChanges, int Withdrawn)
{
    /// <param name="receivedByStatus">How many works that were submitted at least once are in each status now.</param>
    public static WorkTally From(IReadOnlyDictionary<WorkStatus, int> receivedByStatus)
    {
        int Count(params WorkStatus[] statuses) => statuses.Sum(s => receivedByStatus.GetValueOrDefault(s));

        return new WorkTally(
            Received: receivedByStatus.Values.Sum(),
            Approved: Count(WorkStatus.Admitted, WorkStatus.Evaluated),
            Rejected: Count(WorkStatus.RejectedDefinitively),
            InReview: Count(WorkStatus.Submitted, WorkStatus.InReview),
            ReturnedForChanges: Count(WorkStatus.Draft),
            Withdrawn: Count(WorkStatus.Cancelled));
    }
}
