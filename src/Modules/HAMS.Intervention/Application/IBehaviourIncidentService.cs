using HAMS.Intervention.Domain;

namespace HAMS.Intervention.Application;

public interface IBehaviourIncidentService
{
    Task<Guid> RecordAsync(
        Guid studentPersonId, Guid behaviourCategoryId, Guid? subjectId, Guid academicYearId, string description,
        string confidentialityTierCode, Guid recordedByPersonId, DateOnly occurredDate, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">The incident doesn't exist, or the transition is illegal from its current status.</exception>
    Task SubmitAsync(Guid incidentId, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">The incident doesn't exist, or the transition is illegal from its current status.</exception>
    Task BeginReviewAsync(Guid incidentId, Guid reviewedByPersonId, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">The incident doesn't exist, or the transition is illegal from its current status.</exception>
    Task ApproveAsync(Guid incidentId, Guid reviewedByPersonId, string? actionTaken, string? reviewNotes, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">The incident doesn't exist, or the transition is illegal from its current status.</exception>
    Task RejectAsync(Guid incidentId, Guid reviewedByPersonId, string? reviewNotes, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">The incident doesn't exist, or the transition is illegal from its current status.</exception>
    Task ReturnAsync(Guid incidentId, Guid reviewedByPersonId, string? reviewNotes, CancellationToken cancellationToken = default);

    Task<BehaviourIncident?> GetAsync(Guid incidentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BehaviourIncident>> GetForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default);
}
