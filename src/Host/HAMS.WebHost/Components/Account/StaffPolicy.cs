namespace HAMS.WebHost.Components.Account;

/// <summary>
/// Gates the staff operational pages (attendance, homework, assessment moderation, behaviour,
/// intervention, lesson planning) to any authenticated STAFF principal — guardians/students hold
/// real JWTs/cookies too (one issuance path for every principal type, build plan §5), so a bare
/// <c>[Authorize]</c> isn't enough here either. Unlike <see cref="SystemOrSchoolAdminPolicy"/>, this
/// is deliberately a plain claims check (<c>RequireClaim</c>), not a live <c>IRoleMembershipQuery</c>
/// re-check: <c>hams:is_staff</c> records which LOGIN PATH authenticated this principal (staff vs.
/// guardian-OTP vs. student-PIN), not a revocable role membership — it can't go stale the way a
/// granted/revoked <c>Role</c> can, and every staff-facing HTTP endpoint already gates on the exact
/// same claim via <c>ICurrentUser.IsStaff</c>. Individual pages/actions that need a specific ROLE
/// (e.g. only System/School Administrators) still layer <see cref="SystemOrSchoolAdminPolicy"/> or a
/// live per-action check on top of this, the same way <c>AssessmentResultEndpoints</c>'s
/// escalated-approval gate does.
/// </summary>
public static class StaffPolicy
{
    public const string Name = "HAMS.Staff";
}
