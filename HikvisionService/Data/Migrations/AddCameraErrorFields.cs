using Microsoft.EntityFrameworkCore.Migrations;

namespace HikvisionService.Data.Migrations;

public partial class AddCameraErrorFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add new columns to cameras table
        migrationBuilder.AddColumn<string>(
            name: "last_error",
            table: "cameras",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "last_error_at",
            table: "cameras",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "last_health_check_at",
            table: "cameras",
            type: "TEXT",
            nullable: true);

    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Remove columns from cameras table
        migrationBuilder.DropColumn(
            name: "last_error",
            table: "cameras");

        migrationBuilder.DropColumn(
            name: "last_error_at",
            table: "cameras");

        migrationBuilder.DropColumn(
            name: "last_health_check_at",
            table: "cameras");
    }
}