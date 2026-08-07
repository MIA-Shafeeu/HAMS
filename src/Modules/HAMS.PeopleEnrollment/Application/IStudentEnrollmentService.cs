using HAMS.PeopleEnrollment.Domain;

namespace HAMS.PeopleEnrollment.Application;

/// <summary>A roster row for staff-facing UI (attendance/homework/marks entry pages need a name to show, never a bare Guid).</summary>
public sealed record ClassRosterEntry(Guid StudentPersonId, string NameEn, string NameDv, string AdmissionNumber);

/// <summary>
/// Enforces ORG-FR-017 ("one active ordinary class per grade/year") at the application layer —
/// the DB's filtered unique index (<c>PeopleDbContext</c>) is a real, always-on backstop, but this
/// check is what gives a clean, provider-agnostic error path instead of surfacing a raw SQL
/// constraint violation to the caller.
/// </summary>
public interface IStudentEnrollmentService
{
    /// <exception cref="InvalidOperationException">The student already has an active ordinary enrolment for that academic year.</exception>
    Task<Guid> EnrollAsync(Guid studentPersonId, Guid gradeId, Guid classId, Guid academicYearId, DateOnly effectiveFrom, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the student's currently-active enrolment as of <paramref name="asOf"/> — the one
    /// sanctioned way for another module (e.g. AssessmentEvaluation's Phase 8 evaluation engine) to
    /// learn a student's <see cref="StudentEnrollment.GradeId"/>. Callers MUST resolve a grade this
    /// way, never from a <c>Class</c> — a combined-grade class must not let one grade's students
    /// inherit the other grade's key-stage policy (build plan §3/§12).
    /// </summary>
    /// <returns>Null if the student has no active enrolment for that academic year as of that date.</returns>
    Task<StudentEnrollment?> GetActiveEnrollmentAsync(Guid studentPersonId, Guid academicYearId, DateOnly asOf, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes an enrolment (Phase 11 promotion/progression — the first caller that ever needs to end
    /// one; every enrolment before this was open-ended). Never deletes the row, matching this
    /// codebase's standing "never delete, close+reopen" rule for effective-dated data — a new
    /// enrolment in the next grade/year is a separate <see cref="EnrollAsync"/> call, not implied here.
    /// </summary>
    /// <exception cref="InvalidOperationException">The enrolment doesn't exist or is already closed.</exception>
    Task EndEnrollmentAsync(Guid enrollmentId, DateOnly effectiveTo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every student with a currently-active enrolment in <paramref name="classId"/> as of
    /// <paramref name="asOf"/>, with enough of a name to actually render in a staff-facing UI — the
    /// first "resolve a class roster" capability anywhere in this codebase (Phase 9's topic-closure
    /// gap-detection and Phase 13's homework grading both deliberately left this to an explicit
    /// reviewer-supplied list rather than build it; the new staff operational UI is the first real
    /// consumer that actually needs one).
    /// </summary>
    Task<IReadOnlyList<ClassRosterEntry>> GetActiveRosterForClassAsync(Guid classId, DateOnly asOf, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every student with a currently-active enrolment in <paramref name="gradeId"/> (across every
    /// <c>Class</c> that grade's students are split into — a combined class holds more than one
    /// grade, so this deliberately does NOT filter by class at all). Assessment marks entry needs
    /// this, not <see cref="GetActiveRosterForClassAsync"/>: an <c>Assessment</c> is scoped to a
    /// grade, and the build plan's own combined-class warning (§12) says evaluation-adjacent reads
    /// must resolve from <c>StudentEnrollment.GradeId</c>, never from <c>Class</c>.
    /// </summary>
    Task<IReadOnlyList<ClassRosterEntry>> GetActiveRosterForGradeAsync(
        Guid gradeId, Guid academicYearId, DateOnly asOf, CancellationToken cancellationToken = default);
}
