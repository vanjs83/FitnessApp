using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlannedExerciseCompletions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlannedExerciseCompletions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlannedExerciseId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedExerciseCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlannedExerciseCompletions_PlannedExercises_PlannedExerciseId",
                        column: x => x.PlannedExerciseId,
                        principalTable: "PlannedExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlannedExerciseCompletions_ClientId",
                table: "PlannedExerciseCompletions",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedExerciseCompletions_PlannedExerciseId_CompletedAt",
                table: "PlannedExerciseCompletions",
                columns: new[] { "PlannedExerciseId", "CompletedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlannedExerciseCompletions");
        }
    }
}
