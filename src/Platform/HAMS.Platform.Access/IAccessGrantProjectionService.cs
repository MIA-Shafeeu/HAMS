namespace HAMS.Platform.Access;

/// <summary>
/// Projects a source-table change into <c>AccessGrant</c> rows. Methods stage changes on the
/// same <c>AccessDbContext</c> instance but deliberately do <b>not</b> call <c>SaveChangesAsync</c>
/// themselves — the caller (e.g. <see cref="IPersonRoleAssignmentService"/>) commits once, after
/// staging both its own source-row change and the projection, so both land in a single
/// transaction (build plan §4: "upserted synchronously in the same transaction as whatever
/// source table changed").
/// </summary>
public interface IAccessGrantProjectionService
{
    Task UpsertRoleGrantAsync(
        Guid personId, Guid roleId, Guid? schoolId, DateOnly effectiveFrom, DateOnly? effectiveTo, Guid sourceId,
        CancellationToken cancellationToken = default);

    /// <summary>Closes out (never deletes) every grant projected from the given source row, preserving history.</summary>
    Task CloseAsync(string sourceType, Guid sourceId, DateOnly effectiveTo, CancellationToken cancellationToken = default);
}
