using HAMS.OrgCurriculum.Application;
using HAMS.OrgCurriculum.Domain;
using HAMS.OrgCurriculum.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Tests;

public class SyllabusResolverTests
{
    private static OrgDbContext CreateContext() => new(
        new DbContextOptionsBuilder<OrgDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Resolves_the_current_published_syllabus_for_an_applicable_grade()
    {
        await using var db = CreateContext();
        var publishing = new SyllabusPublishingService(db);
        var subjectId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();

        var syllabusId = await publishing.CreateInitialDraftAsync(subjectId);
        db.SyllabusGradeApplicabilities.Add(new SyllabusGradeApplicability { Id = Guid.NewGuid(), SyllabusId = syllabusId, GradeId = gradeId });
        await db.SaveChangesAsync();
        await publishing.PublishAsync(syllabusId);

        var resolver = new SyllabusResolver(db);
        var resolved = await resolver.ResolveAsync(subjectId, gradeId);

        Assert.NotNull(resolved);
        Assert.Equal(syllabusId, resolved!.Id);
    }

    [Fact]
    public async Task Returns_null_when_no_applicability_row_links_the_syllabus_to_that_grade()
    {
        await using var db = CreateContext();
        var publishing = new SyllabusPublishingService(db);
        var subjectId = Guid.NewGuid();

        var syllabusId = await publishing.CreateInitialDraftAsync(subjectId);
        await publishing.PublishAsync(syllabusId);
        // No SyllabusGradeApplicability row added.

        var resolver = new SyllabusResolver(db);
        var resolved = await resolver.ResolveAsync(subjectId, Guid.NewGuid());

        Assert.Null(resolved);
    }

    [Fact]
    public async Task Returns_null_while_the_syllabus_is_still_Draft()
    {
        await using var db = CreateContext();
        var publishing = new SyllabusPublishingService(db);
        var subjectId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();

        var syllabusId = await publishing.CreateInitialDraftAsync(subjectId);
        db.SyllabusGradeApplicabilities.Add(new SyllabusGradeApplicability { Id = Guid.NewGuid(), SyllabusId = syllabusId, GradeId = gradeId });
        await db.SaveChangesAsync();
        // Deliberately not published.

        var resolver = new SyllabusResolver(db);
        var resolved = await resolver.ResolveAsync(subjectId, gradeId);

        Assert.Null(resolved);
    }

    [Fact]
    public async Task After_a_revision_is_published_resolution_returns_the_new_revision_not_the_old_one()
    {
        await using var db = CreateContext();
        var publishing = new SyllabusPublishingService(db);
        var subjectId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();

        var originalId = await publishing.CreateInitialDraftAsync(subjectId);
        db.SyllabusGradeApplicabilities.Add(new SyllabusGradeApplicability { Id = Guid.NewGuid(), SyllabusId = originalId, GradeId = gradeId });
        await db.SaveChangesAsync();
        await publishing.PublishAsync(originalId);

        var revisionId = await publishing.CreateDraftRevisionAsync(originalId);
        // Applicability doesn't carry over automatically — re-declare it for the new revision.
        db.SyllabusGradeApplicabilities.Add(new SyllabusGradeApplicability { Id = Guid.NewGuid(), SyllabusId = revisionId, GradeId = gradeId });
        await db.SaveChangesAsync();
        await publishing.PublishAsync(revisionId);

        var resolver = new SyllabusResolver(db);
        var resolved = await resolver.ResolveAsync(subjectId, gradeId);

        Assert.NotNull(resolved);
        Assert.Equal(revisionId, resolved!.Id);
    }
}
