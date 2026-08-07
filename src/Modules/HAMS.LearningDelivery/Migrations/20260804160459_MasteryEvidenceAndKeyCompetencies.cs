using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HAMS.LearningDelivery.Migrations
{
    /// <inheritdoc />
    public partial class MasteryEvidenceAndKeyCompetencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AchievementScales",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MinimumEvidenceCount = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchievementScales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceTypes",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KeyCompetencies",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameDv = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyCompetencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AchievementLevels",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AchievementScaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchievementLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchievementLevels_AchievementScales_AchievementScaleId",
                        column: x => x.AchievementScaleId,
                        principalSchema: "learning",
                        principalTable: "AchievementScales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KeyCompetencyIndicators",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyCompetencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DescriptionEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DescriptionDv = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyCompetencyIndicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeyCompetencyIndicators_KeyCompetencies_KeyCompetencyId",
                        column: x => x.KeyCompetencyId,
                        principalSchema: "learning",
                        principalTable: "KeyCompetencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearningEvidences",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EvidenceTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AchievementLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedByPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningEvidences_AchievementLevels_AchievementLevelId",
                        column: x => x.AchievementLevelId,
                        principalSchema: "learning",
                        principalTable: "AchievementLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningEvidences_EvidenceTypes_EvidenceTypeId",
                        column: x => x.EvidenceTypeId,
                        principalSchema: "learning",
                        principalTable: "EvidenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningEvidences_LessonSessions_LessonSessionId",
                        column: x => x.LessonSessionId,
                        principalSchema: "learning",
                        principalTable: "LessonSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MasteryEvaluations",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyStagePolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AchievementScaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AchievementLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WasManuallyOverridden = table.Column<bool>(type: "bit", nullable: false),
                    EvidenceCountAtEvaluation = table.Column<int>(type: "int", nullable: false),
                    RecordedByPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasteryEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasteryEvaluations_AchievementLevels_AchievementLevelId",
                        column: x => x.AchievementLevelId,
                        principalSchema: "learning",
                        principalTable: "AchievementLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MasteryEvaluations_AchievementScales_AchievementScaleId",
                        column: x => x.AchievementScaleId,
                        principalSchema: "learning",
                        principalTable: "AchievementScales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KeyCompetencyEvidences",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyCompetencyIndicatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RatingScore = table.Column<int>(type: "int", nullable: true),
                    RecordedByPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyCompetencyEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeyCompetencyEvidences_EvidenceTypes_EvidenceTypeId",
                        column: x => x.EvidenceTypeId,
                        principalSchema: "learning",
                        principalTable: "EvidenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KeyCompetencyEvidences_KeyCompetencyIndicators_KeyCompetencyIndicatorId",
                        column: x => x.KeyCompetencyIndicatorId,
                        principalSchema: "learning",
                        principalTable: "KeyCompetencyIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "learning",
                table: "EvidenceTypes",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0018-000000000001"), "OBSERVATION", 1, true, "Observation" },
                    { new Guid("00000000-0000-0000-0018-000000000002"), "WORK_SAMPLE", 2, true, "Work Sample" },
                    { new Guid("00000000-0000-0000-0018-000000000003"), "QUIZ", 3, true, "Quiz" },
                    { new Guid("00000000-0000-0000-0018-000000000004"), "ANECDOTAL_NOTE", 4, true, "Anecdotal Note" },
                    { new Guid("00000000-0000-0000-0018-000000000005"), "RATING_SCALE", 5, true, "Rating Scale" },
                    { new Guid("00000000-0000-0000-0018-000000000006"), "CHECKLIST", 6, true, "Checklist" },
                    { new Guid("00000000-0000-0000-0018-000000000007"), "PORTFOLIO_REFERENCE", 7, true, "Portfolio Reference" },
                    { new Guid("00000000-0000-0000-0018-000000000008"), "OTHER", 8, true, "Other" }
                });

            migrationBuilder.InsertData(
                schema: "learning",
                table: "KeyCompetencies",
                columns: new[] { "Id", "Code", "DisplayOrder", "NameDv", "NameEn" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0019-000000000001"), "PRACTISING_ISLAM", 1, null, "Practising Islam" },
                    { new Guid("00000000-0000-0000-0019-000000000002"), "UNDERSTANDING_MANAGING_SELF", 2, null, "Understanding & Managing Self" },
                    { new Guid("00000000-0000-0000-0019-000000000003"), "THINKING_CRITICALLY_CREATIVELY", 3, null, "Thinking Critically & Creatively" },
                    { new Guid("00000000-0000-0000-0019-000000000004"), "RELATING_TO_PEOPLE", 4, null, "Relating to People" },
                    { new Guid("00000000-0000-0000-0019-000000000005"), "MAKING_MEANING", 5, null, "Making Meaning" },
                    { new Guid("00000000-0000-0000-0019-000000000006"), "LIVING_HEALTHY_LIFE", 6, null, "Living a Healthy Life" },
                    { new Guid("00000000-0000-0000-0019-000000000007"), "USING_SUSTAINABLE_PRACTICES", 7, null, "Using Sustainable Practices" },
                    { new Guid("00000000-0000-0000-0019-000000000008"), "USING_TECHNOLOGY_MEDIA", 8, null, "Using Technology & the Media" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchievementLevels_AchievementScaleId_Code",
                schema: "learning",
                table: "AchievementLevels",
                columns: new[] { "AchievementScaleId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchievementScales_Code",
                schema: "learning",
                table: "AchievementScales",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceTypes_Code",
                schema: "learning",
                table: "EvidenceTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KeyCompetencies_Code",
                schema: "learning",
                table: "KeyCompetencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KeyCompetencyEvidences_EvidenceTypeId",
                schema: "learning",
                table: "KeyCompetencyEvidences",
                column: "EvidenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyCompetencyEvidences_KeyCompetencyIndicatorId",
                schema: "learning",
                table: "KeyCompetencyEvidences",
                column: "KeyCompetencyIndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyCompetencyEvidences_StudentPersonId_KeyCompetencyIndicatorId",
                schema: "learning",
                table: "KeyCompetencyEvidences",
                columns: new[] { "StudentPersonId", "KeyCompetencyIndicatorId" });

            migrationBuilder.CreateIndex(
                name: "IX_KeyCompetencyIndicators_Code",
                schema: "learning",
                table: "KeyCompetencyIndicators",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KeyCompetencyIndicators_KeyCompetencyId_KeyStageId",
                schema: "learning",
                table: "KeyCompetencyIndicators",
                columns: new[] { "KeyCompetencyId", "KeyStageId" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningEvidences_AchievementLevelId",
                schema: "learning",
                table: "LearningEvidences",
                column: "AchievementLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningEvidences_EvidenceTypeId",
                schema: "learning",
                table: "LearningEvidences",
                column: "EvidenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningEvidences_LessonSessionId",
                schema: "learning",
                table: "LearningEvidences",
                column: "LessonSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningEvidences_StudentPersonId_LearningOutcomeId",
                schema: "learning",
                table: "LearningEvidences",
                columns: new[] { "StudentPersonId", "LearningOutcomeId" });

            migrationBuilder.CreateIndex(
                name: "IX_MasteryEvaluations_AchievementLevelId",
                schema: "learning",
                table: "MasteryEvaluations",
                column: "AchievementLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_MasteryEvaluations_AchievementScaleId",
                schema: "learning",
                table: "MasteryEvaluations",
                column: "AchievementScaleId");

            migrationBuilder.CreateIndex(
                name: "IX_MasteryEvaluations_StudentPersonId_LearningOutcomeId_RecordedAtUtc",
                schema: "learning",
                table: "MasteryEvaluations",
                columns: new[] { "StudentPersonId", "LearningOutcomeId", "RecordedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KeyCompetencyEvidences",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "LearningEvidences",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "MasteryEvaluations",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "KeyCompetencyIndicators",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "EvidenceTypes",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "AchievementLevels",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "KeyCompetencies",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "AchievementScales",
                schema: "learning");
        }
    }
}
