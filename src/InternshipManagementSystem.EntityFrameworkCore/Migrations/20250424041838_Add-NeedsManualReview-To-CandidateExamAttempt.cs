using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddNeedsManualReviewToCandidateExamAttempt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NeedsManualReview",
                table: "AppCandidateExamAttempts",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "هل تحتوي المحاولة على أسئلة تحتاج مراجعة يدوية");

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrect",
                table: "AppCandidateExamAnswers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PartialScore",
                table: "AppCandidateExamAnswers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewComments",
                table: "AppCandidateExamAnswers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NeedsManualReview",
                table: "AppCandidateExamAttempts");

            migrationBuilder.DropColumn(
                name: "IsCorrect",
                table: "AppCandidateExamAnswers");

            migrationBuilder.DropColumn(
                name: "PartialScore",
                table: "AppCandidateExamAnswers");

            migrationBuilder.DropColumn(
                name: "ReviewComments",
                table: "AppCandidateExamAnswers");
        }
    }
}
