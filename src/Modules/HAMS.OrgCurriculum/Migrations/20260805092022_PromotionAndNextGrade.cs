using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HAMS.OrgCurriculum.Migrations
{
    /// <inheritdoc />
    public partial class PromotionAndNextGrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "NextGradeId",
                schema: "org",
                table: "Grades",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Grades_NextGradeId",
                schema: "org",
                table: "Grades",
                column: "NextGradeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_Grades_NextGradeId",
                schema: "org",
                table: "Grades",
                column: "NextGradeId",
                principalSchema: "org",
                principalTable: "Grades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Grades_Grades_NextGradeId",
                schema: "org",
                table: "Grades");

            migrationBuilder.DropIndex(
                name: "IX_Grades_NextGradeId",
                schema: "org",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "NextGradeId",
                schema: "org",
                table: "Grades");
        }
    }
}
