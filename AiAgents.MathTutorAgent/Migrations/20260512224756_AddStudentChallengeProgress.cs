using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiAgents.MathTutorAgent.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentChallengeProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentChallengeProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ChallengeKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentChallengeProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentChallengeProgress_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentChallengeProgress_StudentId_ChallengeKey",
                table: "StudentChallengeProgress",
                columns: new[] { "StudentId", "ChallengeKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentChallengeProgress");
        }
    }
}
