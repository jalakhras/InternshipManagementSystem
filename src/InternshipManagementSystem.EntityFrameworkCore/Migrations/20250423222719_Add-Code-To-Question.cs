using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeToQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeExpectedOutput",
                table: "AppQuestions",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true,
                comment: "المخرجات المتوقعة من تنفيذ الكود");

            migrationBuilder.AddColumn<string>(
                name: "CodeLanguage",
                table: "AppQuestions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                comment: "لغة البرمجة المطلوبة");

            migrationBuilder.AddColumn<string>(
                name: "CodeStarterTemplate",
                table: "AppQuestions",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true,
                comment: "نص الكود الابتدائي الذي يظهر للطالب (Code Starter)");

            migrationBuilder.AddColumn<string>(
                name: "MediaFileName",
                table: "AppQuestions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodeExpectedOutput",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "CodeLanguage",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "CodeStarterTemplate",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "MediaFileName",
                table: "AppQuestions");
        }
    }
}