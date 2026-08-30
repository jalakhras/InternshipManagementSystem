using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementSystem.Migrations
{
    /// <summary>
    /// Drops AppCandidates.Status.
    /// <para>
    /// EF warns that this may lose data. It cannot: nothing in the product ever
    /// assigned the column. Every row in it is the default, and the screen that
    /// read it reported "not invited" about people who had sat the exam and
    /// submitted it. The status is now derived from the facts that actually
    /// record it — whether the person holds a live link, has an unsubmitted
    /// attempt, or has finished one — so there is one source of truth instead of
    /// a stored copy that had already drifted away from all of them.
    /// </para>
    /// </summary>
    public partial class Candidate_Status_Is_Derived : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "AppCandidates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "AppCandidates",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);
        }
    }
}
