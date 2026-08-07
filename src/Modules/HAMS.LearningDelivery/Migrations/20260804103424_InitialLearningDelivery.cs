using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HAMS.LearningDelivery.Migrations
{
    /// <inheritdoc />
    public partial class InitialLearningDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "learning");

            migrationBuilder.CreateTable(
                name: "ResourceTypes",
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
                    table.PrimaryKey("PK_ResourceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchemeOfWorks",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchemeOfWorks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchemeOfWorkItems",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchemeOfWorkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannedWeekNumber = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchemeOfWorkItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchemeOfWorkItems_SchemeOfWorks_SchemeOfWorkId",
                        column: x => x.SchemeOfWorkId,
                        principalSchema: "learning",
                        principalTable: "SchemeOfWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeachingTopics",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchemeOfWorkItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameDv = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeachingTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeachingTopics_SchemeOfWorkItems_SchemeOfWorkItemId",
                        column: x => x.SchemeOfWorkItemId,
                        principalSchema: "learning",
                        principalTable: "SchemeOfWorkItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonPlans",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeachingTopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Objectives = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonPlans_TeachingTopics_TeachingTopicId",
                        column: x => x.TeachingTopicId,
                        principalSchema: "learning",
                        principalTable: "TeachingTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeachingTopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitleDv = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ResourceTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UploadedByPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resources_ResourceTypes_ResourceTypeId",
                        column: x => x.ResourceTypeId,
                        principalSchema: "learning",
                        principalTable: "ResourceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Resources_TeachingTopics_TeachingTopicId",
                        column: x => x.TeachingTopicId,
                        principalSchema: "learning",
                        principalTable: "TeachingTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonSessions",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActualDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonSessions_LessonPlans_LessonPlanId",
                        column: x => x.LessonPlanId,
                        principalSchema: "learning",
                        principalTable: "LessonPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonSessionOutcomeCoverages",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonSessionOutcomeCoverages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonSessionOutcomeCoverages_LessonSessions_LessonSessionId",
                        column: x => x.LessonSessionId,
                        principalSchema: "learning",
                        principalTable: "LessonSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "learning",
                table: "ResourceTypes",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0016-000000000001"), "DOCUMENT", 1, true, "Document" },
                    { new Guid("00000000-0000-0000-0016-000000000002"), "VIDEO", 2, true, "Video" },
                    { new Guid("00000000-0000-0000-0016-000000000003"), "LINK", 3, true, "Link" },
                    { new Guid("00000000-0000-0000-0016-000000000004"), "OTHER", 4, true, "Other" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonPlans_StaffPersonId",
                schema: "learning",
                table: "LessonPlans",
                column: "StaffPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonPlans_TeachingTopicId",
                schema: "learning",
                table: "LessonPlans",
                column: "TeachingTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonSessionOutcomeCoverages_LessonSessionId_LearningOutcomeId",
                schema: "learning",
                table: "LessonSessionOutcomeCoverages",
                columns: new[] { "LessonSessionId", "LearningOutcomeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonSessions_ClassId_ActualDate_PeriodId",
                schema: "learning",
                table: "LessonSessions",
                columns: new[] { "ClassId", "ActualDate", "PeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonSessions_LessonPlanId",
                schema: "learning",
                table: "LessonSessions",
                column: "LessonPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_ResourceTypeId",
                schema: "learning",
                table: "Resources",
                column: "ResourceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_TeachingTopicId",
                schema: "learning",
                table: "Resources",
                column: "TeachingTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTypes_Code",
                schema: "learning",
                table: "ResourceTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchemeOfWorkItems_LearningOutcomeId",
                schema: "learning",
                table: "SchemeOfWorkItems",
                column: "LearningOutcomeId");

            migrationBuilder.CreateIndex(
                name: "IX_SchemeOfWorkItems_SchemeOfWorkId",
                schema: "learning",
                table: "SchemeOfWorkItems",
                column: "SchemeOfWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_SchemeOfWorks_SubjectId_GradeId_AcademicYearId",
                schema: "learning",
                table: "SchemeOfWorks",
                columns: new[] { "SubjectId", "GradeId", "AcademicYearId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeachingTopics_SchemeOfWorkItemId",
                schema: "learning",
                table: "TeachingTopics",
                column: "SchemeOfWorkItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonSessionOutcomeCoverages",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "Resources",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "LessonSessions",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "ResourceTypes",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "LessonPlans",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "TeachingTopics",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "SchemeOfWorkItems",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "SchemeOfWorks",
                schema: "learning");
        }
    }
}
