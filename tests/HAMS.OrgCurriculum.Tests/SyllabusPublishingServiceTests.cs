using HAMS.OrgCurriculum.Application;
using HAMS.OrgCurriculum.Domain;
using HAMS.OrgCurriculum.Infrastructure;
using HAMS.Platform.Audit.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Tests;

public class SyllabusPublishingServiceTests
{
    private static OrgDbContext CreateContext() => new(
        new DbContextOptionsBuilder<OrgDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new SaveChangesGuardInterceptor())
            .Options);

    [Fact]
    public async Task CreateInitialDraftAsync_creates_a_Draft_that_is_not_current()
    {
        await using var db = CreateContext();
        var service = new SyllabusPublishingService(db);

        var syllabusId = await service.CreateInitialDraftAsync(Guid.NewGuid());
        var syllabus = await db.Syllabuses.FindAsync(syllabusId);

        Assert.NotNull(syllabus);
        Assert.Equal(RecordStatus.Draft, syllabus!.Status);
        Assert.False(syllabus.IsCurrent);
        Assert.Equal(1, syllabus.Version);
    }

    [Fact]
    public async Task PublishAsync_makes_a_fresh_draft_current_and_published()
    {
        await using var db = CreateContext();
        var service = new SyllabusPublishingService(db);
        var syllabusId = await service.CreateInitialDraftAsync(Guid.NewGuid());

        await service.PublishAsync(syllabusId);
        var syllabus = await db.Syllabuses.FindAsync(syllabusId);

        Assert.Equal(RecordStatus.Published, syllabus!.Status);
        Assert.True(syllabus.IsCurrent);
    }

    [Fact]
    public async Task PublishAsync_throws_when_the_syllabus_is_not_Draft()
    {
        await using var db = CreateContext();
        var service = new SyllabusPublishingService(db);
        var syllabusId = await service.CreateInitialDraftAsync(Guid.NewGuid());
        await service.PublishAsync(syllabusId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PublishAsync(syllabusId));
    }

    private static async Task<(Guid subjectId, Guid syllabusId)> SeedPublishedSyllabusWithContentAsync(OrgDbContext db, SyllabusPublishingService service)
    {
        var subjectId = Guid.NewGuid();
        var syllabusId = await service.CreateInitialDraftAsync(subjectId);

        var strand = new Strand { Id = Guid.NewGuid(), SyllabusId = syllabusId, Code = "S1", Name = "Number", DisplayOrder = 1 };
        db.Strands.Add(strand);
        var subStrand = new SubStrand { Id = Guid.NewGuid(), StrandId = strand.Id, Code = "SS1", Name = "Fractions", DisplayOrder = 1 };
        db.SubStrands.Add(subStrand);
        var outcomeA = new LearningOutcome { Id = Guid.NewGuid(), SubStrandId = subStrand.Id, Code = "LO1", Description = "Add fractions", DisplayOrder = 1 };
        var outcomeB = new LearningOutcome { Id = Guid.NewGuid(), SubStrandId = subStrand.Id, Code = "LO2", Description = "Multiply fractions", DisplayOrder = 2 };
        db.LearningOutcomes.AddRange(outcomeA, outcomeB);
        db.Indicators.Add(new Indicator { Id = Guid.NewGuid(), LearningOutcomeId = outcomeA.Id, Code = "IND1", Description = "Adds like fractions", DisplayOrder = 1 });
        db.LearningOutcomePrerequisites.Add(new LearningOutcomePrerequisite { Id = Guid.NewGuid(), LearningOutcomeId = outcomeB.Id, PrerequisiteLearningOutcomeId = outcomeA.Id });
        await db.SaveChangesAsync();

        await service.PublishAsync(syllabusId);

        return (subjectId, syllabusId);
    }

    [Fact]
    public async Task CreateDraftRevisionAsync_deep_clones_the_entire_tree_with_new_ids()
    {
        await using var db = CreateContext();
        var service = new SyllabusPublishingService(db);
        var (_, originalSyllabusId) = await SeedPublishedSyllabusWithContentAsync(db, service);

        var revisionId = await service.CreateDraftRevisionAsync(originalSyllabusId);

        var originalStrand = await db.Strands.SingleAsync(s => s.SyllabusId == originalSyllabusId);
        var revisionStrand = await db.Strands.SingleAsync(s => s.SyllabusId == revisionId);
        Assert.NotEqual(originalStrand.Id, revisionStrand.Id);
        Assert.Equal(originalStrand.Code, revisionStrand.Code);

        var revisionSubStrand = await db.SubStrands.SingleAsync(ss => ss.StrandId == revisionStrand.Id);
        var revisionOutcomes = await db.LearningOutcomes.Where(o => o.SubStrandId == revisionSubStrand.Id).ToListAsync();
        Assert.Equal(2, revisionOutcomes.Count);

        var revisionOutcomeA = revisionOutcomes.Single(o => o.Code == "LO1");
        var revisionOutcomeB = revisionOutcomes.Single(o => o.Code == "LO2");

        var revisionIndicator = await db.Indicators.SingleAsync(i => i.LearningOutcomeId == revisionOutcomeA.Id);
        Assert.Equal("IND1", revisionIndicator.Code);

        // The prerequisite link must be remapped to the *new* outcome ids, not the originals'.
        var revisionPrerequisite = await db.LearningOutcomePrerequisites.SingleAsync(p => p.LearningOutcomeId == revisionOutcomeB.Id);
        Assert.Equal(revisionOutcomeA.Id, revisionPrerequisite.PrerequisiteLearningOutcomeId);
    }

    [Fact]
    public async Task Publishing_a_revision_supersedes_the_original_without_altering_its_content()
    {
        await using var db = CreateContext();
        var service = new SyllabusPublishingService(db);
        var (_, originalSyllabusId) = await SeedPublishedSyllabusWithContentAsync(db, service);
        var originalStrandBefore = await db.Strands.AsNoTracking().SingleAsync(s => s.SyllabusId == originalSyllabusId);

        var revisionId = await service.CreateDraftRevisionAsync(originalSyllabusId);
        await service.PublishAsync(revisionId);

        var original = await db.Syllabuses.AsNoTracking().SingleAsync(s => s.Id == originalSyllabusId);
        var revision = await db.Syllabuses.AsNoTracking().SingleAsync(s => s.Id == revisionId);

        Assert.Equal(RecordStatus.Superseded, original.Status);
        Assert.False(original.IsCurrent);
        Assert.Equal(revisionId, original.SupersededById);

        Assert.Equal(RecordStatus.Published, revision.Status);
        Assert.True(revision.IsCurrent);
        Assert.Equal(originalSyllabusId, revision.SupersedesId);

        // The old tree really is frozen — same row, same content, untouched.
        var originalStrandAfter = await db.Strands.AsNoTracking().SingleAsync(s => s.SyllabusId == originalSyllabusId);
        Assert.Equal(originalStrandBefore.Id, originalStrandAfter.Id);
        Assert.Equal(originalStrandBefore.Name, originalStrandAfter.Name);
    }

    [Fact]
    public async Task Directly_modifying_a_published_syllabus_outside_the_service_still_throws()
    {
        // Regression guard: publishing must go through ImmutableRecordCorrectionScope internally,
        // but nothing else gets a free pass at mutating an already-published Syllabus row.
        await using var db = CreateContext();
        var service = new SyllabusPublishingService(db);
        var (_, syllabusId) = await SeedPublishedSyllabusWithContentAsync(db, service);

        var syllabus = await db.Syllabuses.SingleAsync(s => s.Id == syllabusId);
        syllabus.Status = RecordStatus.Locked;

        await Assert.ThrowsAsync<ImmutableRecordMutationException>(() => db.SaveChangesAsync());
    }
}
