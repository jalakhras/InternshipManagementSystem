using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Updated_Answerasafile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnswerFileName",
                table: "AppExamAnswers",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true,
                comment: "مرفق الاجابه للإجابة");

            migrationBuilder.AddColumn<string>(
                name: "AnswerFileUrl",
                table: "AppExamAnswers",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true,
                comment: "رابط مرفق الاجابه للإجابة");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnswerFileName",
                table: "AppExamAnswers");

            migrationBuilder.DropColumn(
                name: "AnswerFileUrl",
                table: "AppExamAnswers");
        }
    }
}