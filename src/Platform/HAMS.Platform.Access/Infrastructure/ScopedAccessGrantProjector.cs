using HAMS.Platform.Access.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HAMS.Platform.Access.Infrastructure;

internal sealed class ScopedAccessGrantProjector : IScopedAccessGrantProjector
{
    public async Task ProjectAsync(
        DbContext sourceContext, Action stageSourceChanges, ScopedAccessGrant grant, CancellationToken cancellationToken = default)
    {
        stageSourceChanges();

        await using var transaction = await sourceContext.Database.BeginTransactionAsync(cancellationToken);
        await sourceContext.SaveChangesAsync(cancellationToken);

        // Share the exact connection + transaction sourceContext is already using — this is one
        // real SQL Server transaction over one connection, not a distributed transaction; EF Core
        // will not open/close/dispose a connection instance handed to it this way.
        var accessOptions = new DbContextOptionsBuilder<AccessDbContext>()
            .UseSqlServer(sourceContext.Database.GetDbConnection())
            .Options;
        await using var accessContext = new AccessDbContext(accessOptions);
        await accessContext.Database.UseTransactionAsync(transaction.GetDbTransaction(), cancellationToken);

        var existing = await accessContext.AccessGrants.SingleOrDefaultAsync(
            g => g.SourceType == grant.SourceType && g.SourceId == grant.SourceId, cancellationToken);

        if (existing is not null)
        {
            existing.EffectiveTo = grant.EffectiveTo;
        }
        else
        {
            accessContext.AccessGrants.Add(new AccessGrant
            {
                Id = Guid.NewGuid(),
                PersonId = grant.PersonId,
                RoleId = grant.RoleId,
                SchoolId = grant.SchoolId,
                CampusId = grant.CampusId,
                AcademicYearId = grant.AcademicYearId,
                KeyStageId = grant.KeyStageId,
                GradeId = grant.GradeId,
                ClassId = grant.ClassId,
                SubjectId = grant.SubjectId,
                StudentId = grant.StudentId,
                EffectiveFrom = grant.EffectiveFrom,
                EffectiveTo = grant.EffectiveTo,
                SourceType = grant.SourceType,
                SourceId = grant.SourceId,
            });
        }

        await accessContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
