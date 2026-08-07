using HAMS.OrgCurriculum.Application;
using HAMS.OrgCurriculum.Domain;
using HAMS.OrgCurriculum.Infrastructure;
using HAMS.Platform.Common.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Tests;

public class KeyStagePolicyResolverTests
{
    private static readonly DateOnly Today = new(2026, 8, 4);
    private static readonly Guid AcademicYearId = Guid.NewGuid();
    private static readonly Guid MasteryModelId = Guid.NewGuid();
    private static readonly Guid HybridModelId = Guid.NewGuid();

    private static OrgDbContext CreateContext() => new(
        new DbContextOptionsBuilder<OrgDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task AssignAndPublishAsync(
        OrgDbContext db, Guid gradeId, Guid keyStageId, Guid evaluationModelId, RecordStatus status = RecordStatus.Published)
    {
        db.GradeKeyStageAssignments.Add(new GradeKeyStageAssignment
        {
            Id = Guid.NewGuid(), GradeId = gradeId, KeyStageId = keyStageId, AcademicYearId = AcademicYearId,
            EffectiveFrom = Today.AddMonths(-6),
        });
        db.KeyStagePolicies.Add(new KeyStagePolicy
        {
            Id = Guid.NewGuid(), KeyStageId = keyStageId, AcademicYearId = AcademicYearId,
            EvaluationModelId = evaluationModelId, IsCurrent = true, Status = status,
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Resolves_the_published_policy_for_a_grade_with_an_active_key_stage_assignment()
    {
        var gradeId = Guid.NewGuid();
        var keyStageId = Guid.NewGuid();
        await using var db = CreateContext();
        await AssignAndPublishAsync(db, gradeId, keyStageId, HybridModelId);

        var resolver = new KeyStagePolicyResolver(db);
        var policy = await resolver.ResolveAsync(gradeId, AcademicYearId, Today);

        Assert.NotNull(policy);
        Assert.Equal(HybridModelId, policy!.EvaluationModelId);
    }

    [Fact]
    public async Task Returns_null_when_the_grade_has_no_active_key_stage_assignment()
    {
        await using var db = CreateContext();
        var resolver = new KeyStagePolicyResolver(db);

        var policy = await resolver.ResolveAsync(Guid.NewGuid(), AcademicYearId, Today);

        Assert.Null(policy);
    }

    [Fact]
    public async Task Returns_null_when_the_key_stage_policy_is_still_Draft()
    {
        var gradeId = Guid.NewGuid();
        var keyStageId = Guid.NewGuid();
        await using var db = CreateContext();
        await AssignAndPublishAsync(db, gradeId, keyStageId, HybridModelId, status: RecordStatus.Draft);

        var resolver = new KeyStagePolicyResolver(db);
        var policy = await resolver.ResolveAsync(gradeId, AcademicYearId, Today);

        Assert.Null(policy);
    }

    [Fact]
    public async Task Returns_null_once_the_assignment_has_expired()
    {
        var gradeId = Guid.NewGuid();
        var keyStageId = Guid.NewGuid();
        await using var db = CreateContext();
        db.GradeKeyStageAssignments.Add(new GradeKeyStageAssignment
        {
            Id = Guid.NewGuid(), GradeId = gradeId, KeyStageId = keyStageId, AcademicYearId = AcademicYearId,
            EffectiveFrom = Today.AddYears(-1), EffectiveTo = Today.AddDays(-1),
        });
        db.KeyStagePolicies.Add(new KeyStagePolicy
        {
            Id = Guid.NewGuid(), KeyStageId = keyStageId, AcademicYearId = AcademicYearId,
            EvaluationModelId = HybridModelId, IsCurrent = true, Status = RecordStatus.Published,
        });
        await db.SaveChangesAsync();

        var resolver = new KeyStagePolicyResolver(db);
        var policy = await resolver.ResolveAsync(gradeId, AcademicYearId, Today);

        Assert.Null(policy);
    }

    [Fact]
    public async Task Only_returns_the_current_version_when_an_older_superseded_version_also_exists()
    {
        var gradeId = Guid.NewGuid();
        var keyStageId = Guid.NewGuid();
        await using var db = CreateContext();
        db.GradeKeyStageAssignments.Add(new GradeKeyStageAssignment
        {
            Id = Guid.NewGuid(), GradeId = gradeId, KeyStageId = keyStageId, AcademicYearId = AcademicYearId,
            EffectiveFrom = Today.AddYears(-1),
        });
        var supersededId = Guid.NewGuid();
        db.KeyStagePolicies.Add(new KeyStagePolicy
        {
            Id = supersededId, KeyStageId = keyStageId, AcademicYearId = AcademicYearId,
            EvaluationModelId = MasteryModelId, Version = 1, IsCurrent = false,
            Status = RecordStatus.Superseded, SupersededById = Guid.NewGuid(),
        });
        db.KeyStagePolicies.Add(new KeyStagePolicy
        {
            Id = Guid.NewGuid(), KeyStageId = keyStageId, AcademicYearId = AcademicYearId,
            EvaluationModelId = HybridModelId, Version = 2, IsCurrent = true,
            Status = RecordStatus.Published, SupersedesId = supersededId,
        });
        await db.SaveChangesAsync();

        var resolver = new KeyStagePolicyResolver(db);
        var policy = await resolver.ResolveAsync(gradeId, AcademicYearId, Today);

        Assert.NotNull(policy);
        Assert.Equal(HybridModelId, policy!.EvaluationModelId);
    }

    [Fact]
    public async Task Combined_class_scenario_resolves_each_grades_own_key_stage_policy_independently()
    {
        // Build plan §12: a class combining Grade 5 (Key Stage 2, Mastery) and Grade 6 (Key Stage 3,
        // Hybrid) must never let one grade's students inherit the other grade's evaluation model.
        // The resolver takes GradeId directly (never ClassId), so this proves that discipline holds.
        var grade5Id = Guid.NewGuid();
        var grade6Id = Guid.NewGuid();
        var keyStage2Id = Guid.NewGuid();
        var keyStage3Id = Guid.NewGuid();

        await using var db = CreateContext();
        await AssignAndPublishAsync(db, grade5Id, keyStage2Id, MasteryModelId);
        await AssignAndPublishAsync(db, grade6Id, keyStage3Id, HybridModelId);

        var resolver = new KeyStagePolicyResolver(db);

        var grade5Policy = await resolver.ResolveAsync(grade5Id, AcademicYearId, Today);
        var grade6Policy = await resolver.ResolveAsync(grade6Id, AcademicYearId, Today);

        Assert.Equal(MasteryModelId, grade5Policy!.EvaluationModelId);
        Assert.Equal(HybridModelId, grade6Policy!.EvaluationModelId);
    }
}
