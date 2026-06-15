using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanToProgressPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlanId",
                table: "ProgressPhotos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgressPhotos_PlanId",
                table: "ProgressPhotos",
                column: "PlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgressPhotos_TrainingPlans_PlanId",
                table: "ProgressPhotos",
                column: "PlanId",
                principalTable: "TrainingPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Backfill: attach each existing photo to that client's most recent (non-template) plan,
            // so historical photos surface under a plan instead of being orphaned. Clients with no
            // plan keep PlanId NULL.
            migrationBuilder.Sql(@"
                UPDATE pp
                SET pp.PlanId = (
                    SELECT TOP 1 tp.Id
                    FROM TrainingPlans tp
                    WHERE tp.ClientId = pp.ClientId AND tp.IsTemplate = 0
                    ORDER BY tp.StartDate DESC, tp.Id DESC)
                FROM ProgressPhotos pp
                WHERE pp.PlanId IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProgressPhotos_TrainingPlans_PlanId",
                table: "ProgressPhotos");

            migrationBuilder.DropIndex(
                name: "IX_ProgressPhotos_PlanId",
                table: "ProgressPhotos");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "ProgressPhotos");
        }
    }
}
