using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Attempt_SingleActivePerLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AppAttempts_ExamLinkId",
                table: "AppAttempts",
                column: "ExamLinkId",
                unique: true,
                filter: "[IsSubmitted] = 0 AND [ExamLinkId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppAttempts_ExamLinkId",
                table: "AppAttempts");
        }
    }
}
