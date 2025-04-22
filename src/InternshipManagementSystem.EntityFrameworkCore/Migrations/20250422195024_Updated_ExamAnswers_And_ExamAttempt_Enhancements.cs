using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Updated_ExamAnswers_And_ExamAttempt_Enhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGraded",
                table: "AppExamAttempts",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "هل تم تصحيح المحاولة تلقائيًا أو يدويًا");

            migrationBuilder.AddColumn<bool>(
                name: "NeedsManualReview",
                table: "AppExamAttempts",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "هل تحتوي المحاولة على أسئلة تحتاج مراجعة يدوية");

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrect",
                table: "AppExamAnswers",
                type: "bit",
                nullable: true,
                comment: "هل الإجابة صحيحة (مراجعة تلقائية أو يدوية)");

            migrationBuilder.AddColumn<double>(
                name: "PartialScore",
                table: "AppExamAnswers",
                type: "float",
                nullable: true,
                comment: "الدرجة الجزئية لهذا الجواب إن وجدت");

            migrationBuilder.AddColumn<string>(
                name: "ReviewComments",
                table: "AppExamAnswers",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "",
                comment: "ملاحظات المدقق اليدوي للإجابة");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGraded",
                table: "AppExamAttempts");

            migrationBuilder.DropColumn(
                name: "NeedsManualReview",
                table: "AppExamAttempts");

            migrationBuilder.DropColumn(
                name: "IsCorrect",
                table: "AppExamAnswers");

            migrationBuilder.DropColumn(
                name: "PartialScore",
                table: "AppExamAnswers");

            migrationBuilder.DropColumn(
                name: "ReviewComments",
                table: "AppExamAnswers");
        }
    }
}
