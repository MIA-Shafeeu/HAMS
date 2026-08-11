using HAMS.AssessmentEvaluation.Domain;
using HAMS.AssessmentEvaluation.Infrastructure;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Audit.Infrastructure;
using HAMS.Platform.Common.Contracts;
using HAMS.Platform.Notifications.Application;
using HAMS.Platform.Notifications.Domain;
using HAMS.Platform.Workflow.Application;
using HAMS.Platform.Workflow.Domain;
using Microsoft.EntityFrameworkCore;

namespace HAMS.AssessmentEvaluation.Application;

internal sealed class AssessmentModerationService(
    AssessmentEvaluationDbContext dbContext, IWorkflowEngine workflowEngine, IGuardianContactResolver guardianContacts,
    INotificationOutboxWriter outboxWriter, IClock clock)
    : IAssessmentModerationService
{
    public async Task<Guid> RecordRawMarkAsync(
        Guid assessmentId, Guid studentPersonId, Guid keyStagePolicyId, decimal? rawMark, Guid? specialResultStateId,
        Guid recordedByPersonId, CancellationToken cancellationToken = default)
    {
        ValidateMarkOrSpecialState(rawMark, specialResultStateId);

        var result = new AssessmentResult
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            StudentPersonId = studentPersonId,
            KeyStagePolicyId = keyStagePolicyId,
            RawMark = rawMark,
            SpecialResultStateId = specialResultStateId,
            RecordedByPersonId = recordedByPersonId,
        };
        dbContext.AssessmentResults.Add(result);
        await dbContext.SaveChangesAsync(cancellationToken);

        return result.Id;
    }

    public async Task ReviseRawMarkAsync(
        Guid assessmentResultId, decimal? rawMark, Guid? specialResultStateId, CancellationToken cancellationToken = default)
    {
        var result = await GetRequiredAsync(assessmentResultId, cancellationToken);
        if (result.ModerationStatus is not (WorkflowStatus.Draft or WorkflowStatus.Returned))
        {
            throw new InvalidOperationException("The raw mark can only be revised while a result is Draft or has been Returned for correction.");
        }

        ValidateMarkOrSpecialState(rawMark, specialResultStateId);
        result.RawMark = rawMark;
        result.SpecialResultStateId = specialResultStateId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SubmitAsync(Guid assessmentResultId, CancellationToken cancellationToken = default)
    {
        var result = await GetRequiredAsync(assessmentResultId, cancellationToken);
        result.ModerationStatus = workflowEngine.Transition(result.ModerationStatus, WorkflowAction.Submit);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginReviewAsync(Guid assessmentResultId, decimal? adjustedMark, CancellationToken cancellationToken = default)
    {
        var result = await GetRequiredAsync(assessmentResultId, cancellationToken);
        result.ModerationStatus = workflowEngine.Transition(result.ModerationStatus, WorkflowAction.Review);

        if (adjustedMark is not null)
        {
            if (result.AdjustedMark is not null)
            {
                throw new InvalidOperationException("AdjustedMark has already been set once and cannot be overwritten.");
            }

            result.AdjustedMark = adjustedMark;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveAsync(Guid assessmentResultId, decimal? moderatedMark, CancellationToken cancellationToken = default)
    {
        var result = await GetRequiredAsync(assessmentResultId, cancellationToken);

        void StageChanges()
        {
            result.ModerationStatus = workflowEngine.Transition(result.ModerationStatus, WorkflowAction.Approve);

            if (moderatedMark is not null)
            {
                if (result.ModeratedMark is not null)
                {
                    throw new InvalidOperationException("ModeratedMark has already been set once and cannot be overwritten.");
                }

                result.ModeratedMark = moderatedMark;
            }

            // The settled mark for this attempt: whichever stage last touched it. Aggregating this
            // across an AssessmentScheme's weighted components into a student's overall subject
            // result is Phase 8's IEvaluationEngine scope, not this one attempt row's job.
            result.FinalMark = result.ModeratedMark ?? result.AdjustedMark ?? result.RawMark;

            // Approval is the one meaningful junction between "the human process finished" and "this
            // row is now structurally locked" (see AssessmentResult's remarks).
            result.Status = RecordStatus.Published;
        }

        // Publication is the one moment a guardian needs to hear about a result (Phase 10) — queued
        // via the same transactional outbox Attendance's absence alert already uses, never sent
        // synchronously in-request, so a slow/failing carrier can never block or roll back the
        // approval itself. Contrast with OTP delivery (GuardianAuthenticationService), which is the
        // one deliberate exception to this rule for a different, time-sensitive reason.
        var today = clock.TodayUtc;
        var contacts = await guardianContacts.ResolveNotifiableGuardianContactsAsync(result.StudentPersonId, today, cancellationToken);
        if (contacts.Count > 0)
        {
            var assessmentTitle = await dbContext.Assessments
                .Where(a => a.Id == result.AssessmentId)
                .Select(a => a.Title)
                .SingleOrDefaultAsync(cancellationToken) ?? "an assessment";

            var notifications = contacts
                .Select(c => c.PhoneNumber is not null
                    ? new OutboundNotification(NotificationChannelCodes.Sms, c.PhoneNumber, null, $"Your child's result for '{assessmentTitle}' has been published.")
                    : c.Email is not null
                        ? new OutboundNotification(NotificationChannelCodes.Email, c.Email, "Result published", $"Your child's result for '{assessmentTitle}' has been published.")
                        : null)
                .Where(n => n is not null)
                .Select(n => n!)
                .ToList();

            if (notifications.Count > 0)
            {
                await outboxWriter.EnqueueManyAsync(dbContext, StageChanges, notifications, cancellationToken);
                return;
            }
        }

        StageChanges();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid assessmentResultId, CancellationToken cancellationToken = default)
    {
        var result = await GetRequiredAsync(assessmentResultId, cancellationToken);
        result.ModerationStatus = workflowEngine.Transition(result.ModerationStatus, WorkflowAction.Reject);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReturnAsync(Guid assessmentResultId, CancellationToken cancellationToken = default)
    {
        var result = await GetRequiredAsync(assessmentResultId, cancellationToken);
        result.ModerationStatus = workflowEngine.Transition(result.ModerationStatus, WorkflowAction.Return);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task EscalateAsync(Guid assessmentResultId, Guid escalatedByPersonId, string escalationReason, CancellationToken cancellationToken = default)
    {
        var result = await GetRequiredAsync(assessmentResultId, cancellationToken);
        result.ModerationStatus = workflowEngine.Transition(result.ModerationStatus, WorkflowAction.Escalate);
        result.EscalatedByPersonId = escalatedByPersonId;
        result.EscalationReason = escalationReason;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> ReviseApprovedResultAsync(Guid assessmentResultId, decimal newFinalMark, CancellationToken cancellationToken = default)
    {
        var existing = await GetRequiredAsync(assessmentResultId, cancellationToken);
        if (existing.Status is not (RecordStatus.Published or RecordStatus.Locked))
        {
            throw new InvalidOperationException("Only an already-Published/Locked result needs this correction path — a Draft result can just be revised directly.");
        }

        var revised = new AssessmentResult
        {
            Id = Guid.NewGuid(),
            AssessmentId = existing.AssessmentId,
            StudentPersonId = existing.StudentPersonId,
            KeyStagePolicyId = existing.KeyStagePolicyId,
            RawMark = existing.RawMark,
            AdjustedMark = existing.AdjustedMark,
            ModeratedMark = existing.ModeratedMark,
            FinalMark = newFinalMark,
            SpecialResultStateId = existing.SpecialResultStateId,
            RecordedByPersonId = existing.RecordedByPersonId,
            ModerationStatus = WorkflowStatus.Approved,
            Version = existing.Version + 1,
            IsCurrent = true,
            SupersedesId = existing.Id,
            Status = RecordStatus.Published,
        };

        using (ImmutableRecordCorrectionScope.Enter())
        {
            existing.IsCurrent = false;
            existing.Status = RecordStatus.Superseded;
            existing.SupersededById = revised.Id;

            dbContext.AssessmentResults.Add(revised);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return revised.Id;
    }

    private async Task<AssessmentResult> GetRequiredAsync(Guid assessmentResultId, CancellationToken cancellationToken)
        => await dbContext.AssessmentResults.FindAsync([assessmentResultId], cancellationToken)
            ?? throw new InvalidOperationException("Assessment result not found.");

    public async Task<AssessmentResult?> GetAsync(Guid assessmentResultId, CancellationToken cancellationToken = default) =>
        await dbContext.AssessmentResults.FindAsync([assessmentResultId], cancellationToken);

    private static void ValidateMarkOrSpecialState(decimal? rawMark, Guid? specialResultStateId)
    {
        if (rawMark is null && specialResultStateId is null)
        {
            throw new InvalidOperationException("Either a raw mark or a special result state must be provided.");
        }
    }
}
