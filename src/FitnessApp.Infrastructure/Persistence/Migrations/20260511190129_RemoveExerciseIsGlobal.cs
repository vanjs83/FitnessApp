using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExerciseIsGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DECLARE @firstTrainer NVARCHAR(450) = (
                    SELECT TOP 1 u.Id
                    FROM AspNetUsers u
                    INNER JOIN AspNetUserRoles ur ON ur.UserId = u.Id
                    INNER JOIN AspNetRoles r ON r.Id = ur.RoleId
                    WHERE r.[Name] = 'Trainer'
                    ORDER BY u.CreatedAt
                );
                IF @firstTrainer IS NOT NULL
                BEGIN
                    UPDATE Exercises
                    SET CreatedByUserId = @firstTrainer
                    WHERE CreatedByUserId IS NULL;
                END;
            ");

            migrationBuilder.DropColumn(
                name: "IsGlobal",
                table: "Exercises");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGlobal",
                table: "Exercises",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
