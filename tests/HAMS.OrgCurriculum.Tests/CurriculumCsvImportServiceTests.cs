using System.Text;
using HAMS.OrgCurriculum.Application;
using HAMS.OrgCurriculum.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HAMS.OrgCurriculum.Tests;

public class CurriculumCsvImportServiceTests
{
    private static OrgDbContext CreateContext() => new(
        new DbContextOptionsBuilder<OrgDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Stream ToStream(string csv) => new MemoryStream(Encoding.UTF8.GetBytes(csv));

    [Fact]
    public async Task Imports_a_flattened_tree_reusing_shared_ancestor_codes()
    {
        await using var db = CreateContext();
        var publishing = new SyllabusPublishingService(db);
        var syllabusId = await publishing.CreateInitialDraftAsync(Guid.NewGuid());

        const string csv =
            "StrandCode,StrandName,SubStrandCode,SubStrandName,OutcomeCode,OutcomeDescription,IndicatorCode,IndicatorDescription\n" +
            "S1,Number,SS1,Fractions,LO1,Add fractions,IND1,Adds like fractions\n" +
            "S1,Number,SS1,Fractions,LO1,Add fractions,IND2,Adds unlike fractions\n" +
            "S1,Number,SS1,Fractions,LO2,Multiply fractions,IND3,Multiplies simple fractions\n";

        var importService = new CurriculumCsvImportService(db);
        var result = await importService.ImportAsync(syllabusId, ToStream(csv));

        Assert.Equal(1, result.StrandsCreated);
        Assert.Equal(1, result.SubStrandsCreated);
        Assert.Equal(2, result.OutcomesCreated);
        Assert.Equal(3, result.IndicatorsCreated);

        var outcome1 = await db.LearningOutcomes.SingleAsync(o => o.Code == "LO1");
        var indicatorsUnderLo1 = await db.Indicators.Where(i => i.LearningOutcomeId == outcome1.Id).ToListAsync();
        Assert.Equal(2, indicatorsUnderLo1.Count);
    }

    [Fact]
    public async Task Throws_when_importing_into_a_non_Draft_syllabus()
    {
        await using var db = CreateContext();
        var publishing = new SyllabusPublishingService(db);
        var syllabusId = await publishing.CreateInitialDraftAsync(Guid.NewGuid());
        await publishing.PublishAsync(syllabusId);

        const string csv = "StrandCode,StrandName,SubStrandCode,SubStrandName,OutcomeCode,OutcomeDescription,IndicatorCode,IndicatorDescription\n" +
                            "S1,Number,SS1,Fractions,LO1,Add fractions,IND1,Adds like fractions\n";

        var importService = new CurriculumCsvImportService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => importService.ImportAsync(syllabusId, ToStream(csv)));
    }

    [Fact]
    public async Task Handles_quoted_fields_containing_embedded_commas()
    {
        await using var db = CreateContext();
        var publishing = new SyllabusPublishingService(db);
        var syllabusId = await publishing.CreateInitialDraftAsync(Guid.NewGuid());

        const string csv = "StrandCode,StrandName,SubStrandCode,SubStrandName,OutcomeCode,OutcomeDescription,IndicatorCode,IndicatorDescription\n" +
                            "S1,Number,SS1,Fractions,LO1,\"Identify, describe and classify fractions\",IND1,\"Names, orders and compares fractions\"\n";

        var importService = new CurriculumCsvImportService(db);
        await importService.ImportAsync(syllabusId, ToStream(csv));

        var outcome = await db.LearningOutcomes.SingleAsync();
        Assert.Equal("Identify, describe and classify fractions", outcome.Description);

        var indicator = await db.Indicators.SingleAsync();
        Assert.Equal("Names, orders and compares fractions", indicator.Description);
    }

    [Fact]
    public async Task Throws_on_a_row_with_the_wrong_number_of_columns()
    {
        await using var db = CreateContext();
        var publishing = new SyllabusPublishingService(db);
        var syllabusId = await publishing.CreateInitialDraftAsync(Guid.NewGuid());

        const string csv = "StrandCode,StrandName,SubStrandCode,SubStrandName,OutcomeCode,OutcomeDescription,IndicatorCode,IndicatorDescription\n" +
                            "S1,Number,SS1,Fractions,LO1,Add fractions,IND1\n"; // missing the last column

        var importService = new CurriculumCsvImportService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => importService.ImportAsync(syllabusId, ToStream(csv)));
    }
}
