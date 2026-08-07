using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HAMS.ReportingAnalyticsAudit.Migrations
{
    /// <inheritdoc />
    public partial class InitialReportingAnalyticsAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reporting");

            migrationBuilder.CreateTable(
                name: "ReportCards",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NarrativeEn = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    NarrativeDv = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    NextStepsEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    NextStepsDv = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PreparedByPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreparedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    SupersedesId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersededById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportCardKeyCompetencySummaries",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyCompetencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceCount = table.Column<int>(type: "int", nullable: false),
                    AverageRatingScore = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportCardKeyCompetencySummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportCardKeyCompetencySummaries_ReportCards_ReportCardId",
                        column: x => x.ReportCardId,
                        principalSchema: "reporting",
                        principalTable: "ReportCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportCardSubjectResults",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceKeyStageEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AchievementLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    GradeBandId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportCardSubjectResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportCardSubjectResults_ReportCards_ReportCardId",
                        column: x => x.ReportCardId,
                        principalSchema: "reporting",
                        principalTable: "ReportCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportCardKeyCompetencySummaries_ReportCardId",
                schema: "reporting",
                table: "ReportCardKeyCompetencySummaries",
                column: "ReportCardId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportCards_StudentPersonId_EvaluationPeriodId_IsCurrent",
                schema: "reporting",
                table: "ReportCards",
                columns: new[] { "StudentPersonId", "EvaluationPeriodId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportCardSubjectResults_ReportCardId",
                schema: "reporting",
                table: "ReportCardSubjectResults",
                column: "ReportCardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportCardKeyCompetencySummaries",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "ReportCardSubjectResults",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "ReportCards",
                schema: "reporting");
        }
    }
}
