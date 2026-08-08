using HAMS.OrgCurriculum.Domain;
using HAMS.OrgCurriculum.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Application;

internal sealed class CurriculumAdminService(OrgDbContext dbContext) : ICurriculumAdminService
{
    public async Task<Guid> CreateCurriculumFrameworkAsync(string code, string name, string? description, CancellationToken cancellationToken = default)
    {
        var framework = new CurriculumFramework { Id = Guid.NewGuid(), Code = code, Name = name, Description = description };
        dbContext.CurriculumFrameworks.Add(framework);
        await dbContext.SaveChangesAsync(cancellationToken);
        return framework.Id;
    }

    public Task<IReadOnlyList<CurriculumFramework>> GetCurriculumFrameworksAsync(CancellationToken cancellationToken = default) =>
        GetOrderedAsync(dbContext.CurriculumFrameworks.OrderBy(f => f.Name), cancellationToken);

    public async Task UpdateCurriculumFrameworkAsync(Guid id, string name, string? description, CancellationToken cancellationToken = default)
    {
        var framework = await dbContext.CurriculumFrameworks.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Curriculum framework not found.");

        framework.Name = name;
        framework.Description = description;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateLearningAreaAsync(Guid curriculumFrameworkId, string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var learningArea = new LearningArea { Id = Guid.NewGuid(), CurriculumFrameworkId = curriculumFrameworkId, Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.LearningAreas.Add(learningArea);
        await dbContext.SaveChangesAsync(cancellationToken);
        return learningArea.Id;
    }

    public Task<IReadOnlyList<LearningArea>> GetLearningAreasAsync(CancellationToken cancellationToken = default) =>
        GetOrderedAsync(dbContext.LearningAreas.OrderBy(a => a.DisplayOrder), cancellationToken);

    public async Task UpdateLearningAreaAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var learningArea = await dbContext.LearningAreas.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Learning area not found.");

        learningArea.Name = name;
        learningArea.DisplayOrder = displayOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<DeliveryMode>> GetDeliveryModesAsync(CancellationToken cancellationToken = default) =>
        GetOrderedAsync(dbContext.DeliveryModes.Where(m => m.IsActive).OrderBy(m => m.DisplayOrder), cancellationToken);

    public Task<IReadOnlyList<DeliveryMode>> GetAllDeliveryModesAsync(CancellationToken cancellationToken = default) =>
        GetOrderedAsync(dbContext.DeliveryModes.OrderBy(m => m.DisplayOrder), cancellationToken);

    public async Task<Guid> CreateDeliveryModeAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var deliveryMode = new DeliveryMode { Id = Guid.NewGuid(), Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.DeliveryModes.Add(deliveryMode);
        await dbContext.SaveChangesAsync(cancellationToken);
        return deliveryMode.Id;
    }

    public async Task SetDeliveryModeActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var deliveryMode = await dbContext.DeliveryModes.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Delivery mode not found.");

        deliveryMode.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDeliveryModeAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var deliveryMode = await dbContext.DeliveryModes.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Delivery mode not found.");

        deliveryMode.Name = name;
        deliveryMode.DisplayOrder = displayOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<MediumOfInstruction>> GetMediumsOfInstructionAsync(CancellationToken cancellationToken = default) =>
        GetOrderedAsync(dbContext.MediumsOfInstruction.Where(m => m.IsActive).OrderBy(m => m.DisplayOrder), cancellationToken);

    public Task<IReadOnlyList<MediumOfInstruction>> GetAllMediumsOfInstructionAsync(CancellationToken cancellationToken = default) =>
        GetOrderedAsync(dbContext.MediumsOfInstruction.OrderBy(m => m.DisplayOrder), cancellationToken);

    public async Task<Guid> CreateMediumOfInstructionAsync(string code, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var medium = new MediumOfInstruction { Id = Guid.NewGuid(), Code = code, Name = name, DisplayOrder = displayOrder };
        dbContext.MediumsOfInstruction.Add(medium);
        await dbContext.SaveChangesAsync(cancellationToken);
        return medium.Id;
    }

    public async Task SetMediumOfInstructionActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var medium = await dbContext.MediumsOfInstruction.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Medium of instruction not found.");

        medium.IsActive = isActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateMediumOfInstructionAsync(Guid id, string name, int displayOrder, CancellationToken cancellationToken = default)
    {
        var medium = await dbContext.MediumsOfInstruction.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Medium of instruction not found.");

        medium.Name = name;
        medium.DisplayOrder = displayOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateSubjectAsync(
        Guid schoolId, Guid learningAreaId, string code, string name,
        string deliveryModeCode, string defaultMediumOfInstructionCode, int displayOrder,
        CancellationToken cancellationToken = default)
    {
        var deliveryMode = await dbContext.DeliveryModes.SingleOrDefaultAsync(m => m.Code == deliveryModeCode && m.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"No active delivery mode with code '{deliveryModeCode}'.");

        var medium = await dbContext.MediumsOfInstruction.SingleOrDefaultAsync(m => m.Code == defaultMediumOfInstructionCode && m.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"No active medium of instruction with code '{defaultMediumOfInstructionCode}'.");

        var subject = new Subject
        {
            Id = Guid.NewGuid(), SchoolId = schoolId, LearningAreaId = learningAreaId,
            Code = code, Name = name, DeliveryModeId = deliveryMode.Id,
            DefaultMediumOfInstructionId = medium.Id, DisplayOrder = displayOrder,
        };
        dbContext.Subjects.Add(subject);
        await dbContext.SaveChangesAsync(cancellationToken);
        return subject.Id;
    }

    public Task<IReadOnlyList<Subject>> GetSubjectsAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        GetOrderedAsync(dbContext.Subjects.Where(s => s.SchoolId == schoolId).OrderBy(s => s.DisplayOrder), cancellationToken);

    public async Task UpdateSubjectAsync(Guid id, string name, string deliveryModeCode, string defaultMediumOfInstructionCode, int displayOrder, CancellationToken cancellationToken = default)
    {
        var subject = await dbContext.Subjects.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Subject not found.");

        var deliveryMode = await dbContext.DeliveryModes.SingleOrDefaultAsync(m => m.Code == deliveryModeCode && m.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"No active delivery mode with code '{deliveryModeCode}'.");

        var medium = await dbContext.MediumsOfInstruction.SingleOrDefaultAsync(m => m.Code == defaultMediumOfInstructionCode && m.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"No active medium of instruction with code '{defaultMediumOfInstructionCode}'.");

        subject.Name = name;
        subject.DeliveryModeId = deliveryMode.Id;
        subject.DefaultMediumOfInstructionId = medium.Id;
        subject.DisplayOrder = displayOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<Syllabus>> GetSyllabusesForSubjectAsync(Guid subjectId, CancellationToken cancellationToken = default) =>
        GetOrderedAsync(dbContext.Syllabuses.Where(s => s.SubjectId == subjectId).OrderByDescending(s => s.Version), cancellationToken);

    public async Task AddSyllabusGradeApplicabilityAsync(Guid syllabusId, Guid gradeId, CancellationToken cancellationToken = default)
    {
        dbContext.SyllabusGradeApplicabilities.Add(new SyllabusGradeApplicability { Id = Guid.NewGuid(), SyllabusId = syllabusId, GradeId = gradeId });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<SyllabusGradeApplicability>> GetSyllabusGradeApplicabilitiesAsync(Guid syllabusId, CancellationToken cancellationToken = default) =>
        GetOrderedAsync(dbContext.SyllabusGradeApplicabilities.Where(a => a.SyllabusId == syllabusId), cancellationToken);

    private static async Task<IReadOnlyList<T>> GetOrderedAsync<T>(IQueryable<T> query, CancellationToken cancellationToken) =>
        await query.ToListAsync(cancellationToken);
}
