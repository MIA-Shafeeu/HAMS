using HAMS.Platform.Common.Contracts;

namespace HAMS.Platform.Access.Domain;

/// <summary>
/// A configurable, DB-backed named role (build plan §1.6 — deliberately not a C# enum, so an
/// administrator can add a school-specific role without a code change). Seeded with the ~20
/// named roles from the SRS's IAM/§5 access-scope model; <see cref="Code"/> is what application
/// code branches on (via <see cref="RoleCodes"/>), <see cref="Name"/> is what admins see and can
/// rename/translate.
/// </summary>
public sealed class Role : ISimpleLookup
{
    public Guid Id { get; init; }

    /// <summary>Stable, never shown to or renamed by users — see <see cref="RoleCodes"/>.</summary>
    public required string Code { get; init; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Stable role codes application code is allowed to branch on. These are seed data, not an
/// exhaustive enum — a school can define additional <see cref="Role"/> rows with codes unknown to
/// this list; such roles simply won't match any code-level special case, only generic
/// <c>AccessGrant</c> scope checks.
/// </summary>
public static class RoleCodes
{
    public const string SystemAdministrator = "SYSTEM_ADMINISTRATOR";
    public const string SchoolAdministrator = "SCHOOL_ADMINISTRATOR";
    public const string Principal = "PRINCIPAL";
    public const string DeputyPrincipal = "DEPUTY_PRINCIPAL";
    public const string ClassTeacher = "CLASS_TEACHER";
    public const string SubjectTeacher = "SUBJECT_TEACHER";
    public const string LeadingTeacher = "LEADING_TEACHER";
    public const string Student = "STUDENT";
    public const string Guardian = "GUARDIAN";
    public const string RegulatoryOfficer = "REGULATORY_OFFICER";
    public const string SchoolInspector = "SCHOOL_INSPECTOR";
    public const string Auditor = "AUDITOR";
}
