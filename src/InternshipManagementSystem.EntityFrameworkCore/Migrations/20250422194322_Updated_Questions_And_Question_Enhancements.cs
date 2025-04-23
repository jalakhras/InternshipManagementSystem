using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Updated_Questions_And_Question_Enhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaPath",
                table: "AppQuestions");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "AppQuestions",
                type: "int",
                nullable: false,
                comment: "نوع السؤال",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "نوع السؤال (اختيارات/صح وخطأ/نصي/برمجي)");

            migrationBuilder.AlterColumn<int>(
                name: "TimeLimitInSeconds",
                table: "AppQuestions",
                type: "int",
                nullable: true,
                comment: "الحد الزمني لهذا السؤال بالثواني (اختياري)",
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldComment: "الوقت المسموح لحل السؤال (بالثواني)");

            migrationBuilder.AlterColumn<double>(
                name: "Score",
                table: "AppQuestions",
                type: "float",
                nullable: false,
                comment: "عدد النقاط المخصصة لهذا السؤال",
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1,
                oldComment: "العلامة المخصصة لهذا السؤال");

            migrationBuilder.AlterColumn<string>(
                name: "OptionsJson",
                table: "AppQuestions",
                type: "nvarchar(max)",
                nullable: false,
                comment: "خيارات السؤال بصيغة JSON",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldComment: "خيارات السؤال (مخزنة بصيغة JSON)");

            migrationBuilder.AddColumn<bool>(
                name: "AllowPartialCredit",
                table: "AppQuestions",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "السماح بالحصول على درجات جزئية للأسئلة متعددة الخيارات");

            migrationBuilder.AddColumn<string>(
                name: "MediaUrl",
                table: "AppQuestions",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "",
                comment: "رابط وسائط السؤال (صورة، صوت، فيديو)");

            migrationBuilder.AddColumn<bool>(
                name: "IsSubmitted",
                table: "AppCandidateExamAttempts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowPartialCredit",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "MediaUrl",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "IsSubmitted",
                table: "AppCandidateExamAttempts");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "AppQuestions",
                type: "int",
                nullable: false,
                comment: "نوع السؤال (اختيارات/صح وخطأ/نصي/برمجي)",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "نوع السؤال");

            migrationBuilder.AlterColumn<int>(
                name: "TimeLimitInSeconds",
                table: "AppQuestions",
                type: "int",
                nullable: true,
                comment: "الوقت المسموح لحل السؤال (بالثواني)",
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldComment: "الحد الزمني لهذا السؤال بالثواني (اختياري)");

            migrationBuilder.AlterColumn<int>(
                name: "Score",
                table: "AppQuestions",
                type: "int",
                nullable: false,
                defaultValue: 1,
                comment: "العلامة المخصصة لهذا السؤال",
                oldClrType: typeof(double),
                oldType: "float",
                oldComment: "عدد النقاط المخصصة لهذا السؤال");

            migrationBuilder.AlterColumn<string>(
                name: "OptionsJson",
                table: "AppQuestions",
                type: "nvarchar(max)",
                nullable: false,
                comment: "خيارات السؤال (مخزنة بصيغة JSON)",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldComment: "خيارات السؤال بصيغة JSON");

            migrationBuilder.AddColumn<string>(
                name: "MediaPath",
                table: "AppQuestions",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "",
                comment: "رابط صورة أو فيديو أو ملف مرفق مع السؤال");
        }
    }
}