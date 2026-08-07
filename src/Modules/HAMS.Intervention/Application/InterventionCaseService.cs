using HAMS.AssessmentEvaluation.Application.Evaluation;
using HAMS.Intervention.Domain;
using HAMS.Intervention.Infrastructure;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Notifications.Application;
using HAMS.Platform.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.Intervention.Application;

internal sealed class InterventionCaseService(
    InterventionDbContext dbContext, IKeyStageEvaluationService keyStageEvaluationService,
    IGuardianContactResolver guardianContacts, INotificationOutboxWriter outboxWriter, IClock clock)
    : IInterventionCaseService
{
    public async Task<Guid> OpenCaseAsync(
        Guid studentPersonId, Guid subjectId, Guid academicYearId, Guid interventionTypeId, string confidentialityTierCode,
        Guid? learningOutcomeId, Guid? triggeringKeyStageEvaluationId, Guid? carriedForwardGapId,
        Guid openedByPersonId, DateOnly openedDate, CancellationToken cancellationToken = default)
    {
        var interventionCase = new InterventionCase
        {
            Id = Guid.NewGuid(),
            StudentPersonId = studentPersonId,
            SubjectId = subjectId,
            AcademicYearId = academicYearId,
            LearningOutcomeId = learningOutcomeId,
            TriggeringKeyStageEvaluationId = triggeringKeyStageEvaluationId,
            CarriedForwardGapId = carriedForwardGapId,
            InterventionTypeId = interventionTypeId,
            ConfidentialityTierCode = confidentialityTierCode,
            OpenedByPersonId = openedByPersonId,
            OpenedDate = openedDate,
        };

        void StageChanges() => dbContext.InterventionCases.Add(interventionCase);

        // A guardian needs to know support has started (Phase 10) — deliberately a generic,
        // non-sensitive body regardless of confidentiality tier: the tier gates STAFF access to
        // case detail (Platform.Access's confidentiality kernel), not a guardian's basic awareness
        // that their own child is receiving support, and SMS is a low-trust channel this codebase
        // never puts sensitive content on (see Attendance's identically terse absence notice).
        var contacts = await guardianContacts.ResolveNotifiableGuardianContactsAsync(studentPersonId, openedDate, cancellationToken);
        var notifications = contacts
            .Select(c => c.PhoneNumber is not null
                ? new OutboundNotification(NotificationChannelCodes.Sms, c.PhoneNumber, null, "Your child has started receiving additional learning support at school.")
                : c.Email is not null
                    ? new OutboundNotification(NotificationChannelCodes.Email, c.Email, "Support update", "Your child has started receiving additional learning support at school.")
                    : null)
            .Where(n => n is not null)
            .Select(n => n!)
            .ToList();

        if (notifications.Count > 0)
        {
            await outboxWriter.EnqueueManyAsync(dbContext, StageChanges, notifications, cancellationToken);
            return interventionCase.Id;
        }

        StageChanges();
        await dbContext.SaveChangesAsync(cancellationToken);

        return interventionCase.Id;
    }

    public async Task<Guid> CreatePlanAsync(
        Guid interventionCaseId, string description, Guid assignedStaffPersonId, DateOnly startDate, DateOnly targetDate,
        string? notes, CancellationToken cancellationToken = default)
    {
        await GetRequiredCaseAsync(interventionCaseId, cancellationToken);

        var plan = new InterventionPlan
        {
            Id = Guid.NewGuid(),
            InterventionCaseId = interventionCaseId,
            Description = description,
            AssignedStaffPersonId = assignedStaffPersonId,
            StartDate = startDate,
            TargetDate = targetDate,
            Notes = notes,
            CreatedAtUtc = clock.UtcNow,
        };
        dbContext.InterventionPlans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);

        return plan.Id;
    }

    public async Task<Guid> RecordReassessmentAttemptAsync(
        Guid interventionCaseId, Guid academicYearId, Guid evaluationPeriodId, DateOnly asOf, Guid recordedByPersonId,
        CancellationToken cancellationToken = default)
    {
        var interventionCase = await GetRequiredCaseAsync(interventionCaseId, cancellationToken);
        if (interventionCase.Status is InterventionCaseStatus.Closed)
        {
            throw new InvalidOperationException("Cannot record a reassessment attempt against a closed intervention case.");
        }

        var evaluationId = await keyStageEvaluationService.EvaluateAsync(
            interventionCase.StudentPersonId, interventionCase.SubjectId, academicYearId, evaluationPeriodId, asOf, cancellationToken);

        var attempt = new ReassessmentAttempt
        {
            Id = Guid.NewGuid(),
            InterventionCaseId = interventionCaseId,
            KeyStageEvaluationId = evaluationId,
            RecordedByPersonId = recordedByPersonId,
            RecordedAtUtc = clock.UtcNow,
        };
        dbContext.ReassessmentAttempts.Add(attempt);
        await dbContext.SaveChangesAsync(cancellationToken);

        return attempt.Id;
    }

    public async Task CloseCaseAsync(Guid interventionCaseId, DateOnly closedDate, CancellationToken cancellationToken = default)
    {
        var interventionCase = await GetRequiredCaseAsync(interventionCaseId, cancellationToken);
        interventionCase.Status = InterventionCaseStatus.Closed;
        interventionCase.ClosedDate = closedDate;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<InterventionCase?> GetAsync(Guid interventionCaseId, CancellationToken cancellationToken = default)
        => await dbContext.InterventionCases.FindAsync([interventionCaseId], cancellationToken);

    public async Task<IReadOnlyList<InterventionCase>> GetCasesForStudentAsync(Guid studentPersonId, CancellationToken cancellationToken = default)
        => await dbContext.InterventionCases
            .Where(c => c.StudentPersonId == studentPersonId)
            .OrderByDescending(c => c.OpenedDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InterventionPlan>> GetPlansAsync(Guid interventionCaseId, CancellationToken cancellationToken = default)
        => await dbContext.InterventionPlans.Where(p => p.InterventionCaseId == interventionCaseId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReassessmentAttempt>> GetReassessmentAttemptsAsync(Guid interventionCaseId, CancellationToken cancellationToken = default)
        => await dbContext.ReassessmentAttempts.Where(a => a.InterventionCaseId == interventionCaseId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InterventionTypeOption>> GetActiveInterventionTypesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.InterventionTypes.Where(t => t.IsActive).OrderBy(t => t.DisplayOrder)
            .Select(t => new InterventionTypeOption(t.Id, t.Code, t.Name)).ToListAsync(cancellationToken);

    private async Task<InterventionCase> GetRequiredCaseAsync(Guid interventionCaseId, CancellationToken cancellationToken)
        => await dbContext.InterventionCases.FindAsync([interventionCaseId], cancellationToken)
            ?? throw new InvalidOperationException("Intervention case not found.");
}
