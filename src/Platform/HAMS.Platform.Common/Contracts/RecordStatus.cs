namespace HAMS.Platform.Common.Contracts;

/// <summary>
/// The shared status lifecycle for every <see cref="IVersionedRecord{TKey}"/> entity. A genuine
/// C# enum by deliberate exception to the no-enums-for-business-data principle (build plan §1.6):
/// this is a purely structural/technical state — "Published" always means the same thing to the
/// code regardless of school configuration, and adding a new status would require new code paths
/// (a new branch in <c>SaveChangesGuardInterceptor</c>/<c>CorrectionService</c>) no matter how it
/// were stored.
/// </summary>
public enum RecordStatus
{
    Draft = 0,
    Published = 1,
    Locked = 2,
    Superseded = 3,
}
