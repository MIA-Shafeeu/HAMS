using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HAMS.Intervention.Migrations
{
    /// <inheritdoc />
    public partial class InitialIntervention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "intervention");

            migrationBuilder.CreateTable(
                name: "InterventionTypes",
                schema: "intervention",
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
                    table.PrimaryKey("PK_InterventionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TopicClosures",
                schema: "intervention",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeachingTopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestedByPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewedByPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicClosures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterventionCases",
                schema: "intervention",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TriggeringKeyStageEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CarriedForwardGapId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InterventionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfidentialityTierCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OpenedByPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpenedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClosedDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterventionCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterventionCases_InterventionTypes_InterventionTypeId",
                        column: x => x.InterventionTypeId,
                        principalSchema: "intervention",
                        principalTable: "InterventionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CarriedForwardGaps",
                schema: "intervention",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicClosureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterventionCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdentifiedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarriedForwardGaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarriedForwardGaps_InterventionCases_InterventionCaseId",
                        column: x => x.InterventionCaseId,
                        principalSchema: "intervention",
                        principalTable: "InterventionCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CarriedForwardGaps_TopicClosures_TopicClosureId",
                        column: x => x.TopicClosureId,
                        principalSchema: "intervention",
                        principalTable: "TopicClosures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterventionPlans",
                schema: "intervention",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterventionCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AssignedStaffPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterventionPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterventionPlans_InterventionCases_InterventionCaseId",
                        column: x => x.InterventionCaseId,
                        principalSchema: "intervention",
                        principalTable: "InterventionCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReassessmentAttempts",
                schema: "intervention",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterventionCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyStageEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedByPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReassessmentAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReassessmentAttempts_InterventionCases_InterventionCaseId",
                        column: x => x.InterventionCaseId,
                        principalSchema: "intervention",
                        principalTable: "InterventionCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "intervention",
                table: "InterventionTypes",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0024-000000000001"), "ADDITIONAL_PRACTICE", 1, true, "Additional Practice" },
                    { new Guid("00000000-0000-0000-0024-000000000002"), "ONE_ON_ONE_SUPPORT", 2, true, "One-on-One Support" },
                    { new Guid("00000000-0000-0000-0024-000000000003"), "PEER_TUTORING", 3, true, "Peer Tutoring" },
                    { new Guid("00000000-0000-0000-0024-000000000004"), "PARENT_CONFERENCE", 4, true, "Parent Conference" },
                    { new Guid("00000000-0000-0000-0024-000000000005"), "LEARNING_SUPPORT_REFERRAL", 5, true, "Learning Support Referral" },
                    { new Guid("00000000-0000-0000-0024-000000000006"), "OTHER", 6, true, "Other" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarriedForwardGaps_InterventionCaseId",
                schema: "intervention",
                table: "CarriedForwardGaps",
                column: "InterventionCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CarriedForwardGaps_StudentPersonId_LearningOutcomeId",
                schema: "intervention",
                table: "CarriedForwardGaps",
                columns: new[] { "StudentPersonId", "LearningOutcomeId" });

            migrationBuilder.CreateIndex(
                name: "IX_CarriedForwardGaps_TopicClosureId",
                schema: "intervention",
                table: "CarriedForwardGaps",
                column: "TopicClosureId");

            migrationBuilder.CreateIndex(
                name: "IX_InterventionCases_InterventionTypeId",
                schema: "intervention",
                table: "InterventionCases",
                column: "InterventionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_InterventionCases_StudentPersonId_SubjectId_Status",
                schema: "intervention",
                table: "InterventionCases",
                columns: new[] { "StudentPersonId", "SubjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InterventionPlans_InterventionCaseId",
                schema: "intervention",
                table: "InterventionPlans",
                column: "InterventionCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_InterventionTypes_Code",
                schema: "intervention",
                table: "InterventionTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReassessmentAttempts_InterventionCaseId",
                schema: "intervention",
                table: "ReassessmentAttempts",
                column: "InterventionCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicClosures_TeachingTopicId_CreatedAtUtc",
                schema: "intervention",
                table: "TopicClosures",
                columns: new[] { "TeachingTopicId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarriedForwardGaps",
                schema: "intervention");

            migrationBuilder.DropTable(
                name: "InterventionPlans",
                schema: "intervention");

            migrationBuilder.DropTable(
                name: "ReassessmentAttempts",
                schema: "intervention");

            migrationBuilder.DropTable(
                name: "TopicClosures",
                schema: "intervention");

            migrationBuilder.DropTable(
                name: "InterventionCases",
                schema: "intervention");

            migrationBuilder.DropTable(
                name: "InterventionTypes",
                schema: "intervention");
        }
    }
}
