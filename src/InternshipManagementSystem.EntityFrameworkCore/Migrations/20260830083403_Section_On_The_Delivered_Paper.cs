using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <summary>
    /// Records which part of the exam each question was served under.
    /// <para>
    /// Sections were authorable and never delivered because this column did not
    /// exist: nothing downstream of the form builder could tell one part of a
    /// paper from another, so an exam laid out in four skills produced a flat
    /// paper and a flat result.
    /// </para>
    /// <para>
    /// Nullable, and null on every existing row. That is honest — those papers
    /// were built before anything knew about sections, and back-filling them from
    /// the questions' current sections would invent a layout those candidates
    /// never sat. Their results keep the topic breakdown they already had.
    /// </para>
    /// </summary>
    public partial class Section_On_The_Delivered_Paper : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExamSectionId",
                table: "AppAttemptQuestions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppAttemptQuestions_AttemptId_ExamSectionId_Position",
                table: "AppAttemptQuestions",
                columns: new[] { "AttemptId", "ExamSectionId", "Position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppAttemptQuestions_AttemptId_ExamSectionId_Position",
                table: "AppAttemptQuestions");

            migrationBuilder.DropColumn(
                name: "ExamSectionId",
                table: "AppAttemptQuestions");
        }
    }
}
