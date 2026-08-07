using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HAMS.LearningDelivery.Migrations
{
    /// <inheritdoc />
    public partial class HomeworkAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Homeworks",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeachingTopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitleDv = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InstructionsEn = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    InstructionsDv = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AssignedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MaxScore = table.Column<int>(type: "int", nullable: true),
                    AssignedByPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Homeworks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Homeworks_TeachingTopics_TeachingTopicId",
                        column: x => x.TeachingTopicId,
                        principalSchema: "learning",
                        principalTable: "TeachingTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HomeworkSubmissions",
                schema: "learning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HomeworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmissionText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FileReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: true),
                    FeedbackText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    GradedByPersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GradedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeworkSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeworkSubmissions_Homeworks_HomeworkId",
                        column: x => x.HomeworkId,
                        principalSchema: "learning",
                        principalTable: "Homeworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Homeworks_ClassId_DueDate",
                schema: "learning",
                table: "Homeworks",
                columns: new[] { "ClassId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Homeworks_TeachingTopicId",
                schema: "learning",
                table: "Homeworks",
                column: "TeachingTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeworkSubmissions_HomeworkId_StudentPersonId",
                schema: "learning",
                table: "HomeworkSubmissions",
                columns: new[] { "HomeworkId", "StudentPersonId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeworkSubmissions",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "Homeworks",
                schema: "learning");
        }
    }
}
