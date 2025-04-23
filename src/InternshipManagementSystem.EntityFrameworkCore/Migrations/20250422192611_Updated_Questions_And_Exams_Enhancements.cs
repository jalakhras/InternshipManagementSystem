using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Updated_Questions_And_Exams_Enhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "AppQuestions",
                type: "int",
                nullable: false,
                comment: "نوع السؤال (اختيارات/صح وخطأ/نصي/برمجي)",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "نوع السؤال (اختياري/صح وخطأ/نصي)");

            migrationBuilder.AlterColumn<string>(
                name: "OptionsJson",
                table: "AppQuestions",
                type: "nvarchar(max)",
                nullable: false,
                comment: "خيارات السؤال (مخزنة بصيغة JSON)",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldComment: "خيارات السؤال (مخزنة كـ JSON)");

            migrationBuilder.AddColumn<string>(
                name: "MediaPath",
                table: "AppQuestions",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "",
                comment: "رابط صورة أو فيديو أو ملف مرفق مع السؤال");

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "AppQuestions",
                type: "int",
                nullable: false,
                defaultValue: 1,
                comment: "العلامة المخصصة لهذا السؤال");

            migrationBuilder.AddColumn<int>(
                name: "TimeLimitInSeconds",
                table: "AppQuestions",
                type: "int",
                nullable: true,
                comment: "الوقت المسموح لحل السؤال (بالثواني)");

            migrationBuilder.AlterColumn<int>(
                name: "TotalQuestions",
                table: "AppExams",
                type: "int",
                nullable: false,
                comment: "عدد الأسئلة الكلي",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "عدد الأسئلة بالاختبار");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "AppExams",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                comment: "عنوان الامتحان",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldComment: "عنوان الاختبار");

            migrationBuilder.AlterColumn<int>(
                name: "TimeLimitInMinutes",
                table: "AppExams",
                type: "int",
                nullable: false,
                comment: "المدة الإجمالية المسموح بها لحل الامتحان",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "الوقت المحدد بالدقائق للامتحان");

            migrationBuilder.AlterColumn<Guid>(
                name: "SpecializationId",
                table: "AppExams",
                type: "uniqueidentifier",
                nullable: false,
                comment: "التخصص المرتبط بالامتحان",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "التخصص المرتبط بالاختبار");

            migrationBuilder.AlterColumn<string>(
                name: "Level",
                table: "AppExams",
                type: "nvarchar(max)",
                nullable: false,
                comment: "مستوى الامتحان (مبتدئ/متوسط/متقدم)",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldComment: "مستوى الاختبار (مبتدئ/متوسط/متقدم)");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "AppExams",
                type: "bit",
                nullable: false,
                comment: "هل الامتحان مفعل أم لا",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComment: "هل الاختبار مفعل؟");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AppExams",
                type: "nvarchar(max)",
                nullable: false,
                comment: "وصف مختصر للامتحان",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldComment: "وصف الاختبار");

            migrationBuilder.AddColumn<bool>(
                name: "AllowQuestionTimeLimit",
                table: "AppExams",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "هل يُسمح بتحديد وقت لكل سؤال بشكل مستقل؟");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaPath",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "TimeLimitInSeconds",
                table: "AppQuestions");

            migrationBuilder.DropColumn(
                name: "AllowQuestionTimeLimit",
                table: "AppExams");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "AppQuestions",
                type: "int",
                nullable: false,
                comment: "نوع السؤال (اختياري/صح وخطأ/نصي)",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "نوع السؤال (اختيارات/صح وخطأ/نصي/برمجي)");

            migrationBuilder.AlterColumn<string>(
                name: "OptionsJson",
                table: "AppQuestions",
                type: "nvarchar(max)",
                nullable: false,
                comment: "خيارات السؤال (مخزنة كـ JSON)",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldComment: "خيارات السؤال (مخزنة بصيغة JSON)");

            migrationBuilder.AlterColumn<int>(
                name: "TotalQuestions",
                table: "AppExams",
                type: "int",
                nullable: false,
                comment: "عدد الأسئلة بالاختبار",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "عدد الأسئلة الكلي");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "AppExams",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                comment: "عنوان الاختبار",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldComment: "عنوان الامتحان");

            migrationBuilder.AlterColumn<int>(
                name: "TimeLimitInMinutes",
                table: "AppExams",
                type: "int",
                nullable: false,
                comment: "الوقت المحدد بالدقائق للامتحان",
                oldClrType: typeof(int),
                oldType: "int",
                oldComment: "المدة الإجمالية المسموح بها لحل الامتحان");

            migrationBuilder.AlterColumn<Guid>(
                name: "SpecializationId",
                table: "AppExams",
                type: "uniqueidentifier",
                nullable: false,
                comment: "التخصص المرتبط بالاختبار",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldComment: "التخصص المرتبط بالامتحان");

            migrationBuilder.AlterColumn<string>(
                name: "Level",
                table: "AppExams",
                type: "nvarchar(max)",
                nullable: false,
                comment: "مستوى الاختبار (مبتدئ/متوسط/متقدم)",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldComment: "مستوى الامتحان (مبتدئ/متوسط/متقدم)");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "AppExams",
                type: "bit",
                nullable: false,
                comment: "هل الاختبار مفعل؟",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldComment: "هل الامتحان مفعل أم لا");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AppExams",
                type: "nvarchar(max)",
                nullable: false,
                comment: "وصف الاختبار",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldComment: "وصف مختصر للامتحان");
        }
    }
}