using HAMS.Platform.Common.Contracts;

namespace HAMS.Platform.Access.Domain;

/// <summary>
/// A separate, always-explicit grant of confidential access — never implied by ordinary role
/// membership (build plan §4). <see cref="ConfidentialityAuthorizationHandler"/> is the only code
/// path that reads this table, and it is always AND-ed on top of (never a substitute for) the
/// generic <c>ScopeAuthorizationHandler</c> check.
/// </summary>
public sealed class ConfidentialAccessGrant : IEffectiveDated
{
    public Guid Id { get; init; }

    public Guid PersonId { get; init; }

    /// <summary>Null = this grant applies to every student's records at the given tier (e.g. a Safeguarding Lead), not just one.</summary>
    public Guid? StudentId { get; init; }

    public Guid ConfidentialityTierId { get; init; }

    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; set; }
}
