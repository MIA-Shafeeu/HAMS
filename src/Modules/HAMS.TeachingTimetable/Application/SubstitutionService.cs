using HAMS.TeachingTimetable.Domain;
using HAMS.TeachingTimetable.Infrastructure;

namespace HAMS.TeachingTimetable.Application;

internal sealed class SubstitutionService(TeachingTimetableDbContext dbContext, SubjectTeachingAssignmentService assignmentService)
    : ISubstitutionService
{
    public async Task<Guid> CreateSubstitutionAsync(
        Guid originalAssignmentId, Guid substituteStaffPersonId, DateOnly substitutionDate, Guid? schoolId,
        string? reason, CancellationToken cancellationToken = default)
    {
        var original = await dbContext.SubjectTeachingAssignments.FindAsync([originalAssignmentId], cancellationToken)
            ?? throw new InvalidOperationException("Original teaching assignment not found.");

        var generatedAssignmentId = Guid.NewGuid();
        var substitutionRecord = new SubstitutionRecord
        {
            Id = Guid.NewGuid(),
            OriginalAssignmentId = originalAssignmentId,
            SubstituteStaffPersonId = substituteStaffPersonId,
            SubstitutionDate = substitutionDate,
            GeneratedAssignmentId = generatedAssignmentId,
            Reason = reason,
        };
        // Staged now, saved as part of the same transaction AssignWithRoleAsync opens below.
        dbContext.SubstitutionRecords.Add(substitutionRecord);

        await assignmentService.AssignWithRoleAsync(
            substituteStaffPersonId, original.SubjectId, original.ClassId, original.AcademicYearId, schoolId,
            AssignmentRoleCodes.Substitute, substitutionDate, substitutionDate, cancellationToken, generatedAssignmentId);

        return substitutionRecord.Id;
    }
}
