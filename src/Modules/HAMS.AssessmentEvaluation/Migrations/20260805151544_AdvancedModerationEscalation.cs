using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HAMS.AssessmentEvaluation.Migrations
{
    /// <inheritdoc />
    public partial class AdvancedModerationEscalation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EscalatedByPersonId",
                schema: "assessment",
                table: "AssessmentResults",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EscalationReason",
                schema: "assessment",
                table: "AssessmentResults",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EscalatedByPersonId",
                schema: "assessment",
                table: "AssessmentResults");

            migrationBuilder.DropColumn(
                name: "EscalationReason",
                schema: "assessment",
                table: "AssessmentResults");
        }
    }
}
