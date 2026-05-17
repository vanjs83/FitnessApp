using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingPlanPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "TrainingPlans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "TrainingPlans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentClaimedAt",
                table: "TrainingPlans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "TrainingPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "TrainingPlans",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(@"
                UPDATE TrainingPlans
                SET PaymentStatus = 2, ApprovedAt = GETUTCDATE()
                WHERE Price = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "PaymentClaimedAt",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "TrainingPlans");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "TrainingPlans");
        }
    }
}
