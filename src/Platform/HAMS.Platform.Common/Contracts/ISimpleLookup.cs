namespace HAMS.Platform.Common.Contracts;

/// <summary>
/// Shared shape for the ~19 "configurable, DB-backed reference data" lookup entities scattered
/// across every module (build plan §1.6: never a C# enum, so an admin can rename or add a value
/// without a redeploy) — <c>Role</c>, <c>AttendanceStatus</c>, <c>BehaviourCategory</c>,
/// <c>EvidenceType</c>, and so on. Purely structural: implementing it costs nothing (every one of
/// these entities already has exactly these four members) and lets the Blazor admin UI's one
/// reusable <c>SimpleLookupManager</c> component render/create/toggle any of them without a
/// bespoke page per lookup.
/// </summary>
public interface ISimpleLookup
{
    Guid Id { get; }

    string Code { get; }

    string Name { get; }

    int DisplayOrder { get; }

    bool IsActive { get; }
}
