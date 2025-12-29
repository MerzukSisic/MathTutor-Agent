using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiAgents.MathTutorAgent.Migrations
{
    /// <inheritdoc />
    public partial class UpdateForMlSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "MasteryScore",
                table: "StudentTopicStates",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "MasteryScore",
                table: "StudentTopicStates",
                type: "float",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");
        }
    }
}
