using Microsoft.EntityFrameworkCore.Migrations;

namespace HikvisionService.Data.Migrations;

public partial class AddCameraStatusAndStorageDrive : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add new columns to cameras table
        migrationBuilder.AddColumn<bool>(
            name: "is_online",
            table: "cameras",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "last_online_at",
            table: "cameras",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "last_downloaded_at",
            table: "cameras",
            type: "TEXT",
            nullable: true);

        // Create storage_drives table
        migrationBuilder.CreateTable(
            name: "storage_drives",
            columns: table => new
            {
                id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                name = table.Column<string>(type: "TEXT", nullable: false),
                root_path = table.Column<string>(type: "TEXT", nullable: false),
                total_space = table.Column<long>(type: "INTEGER", nullable: false),
                used_space = table.Column<long>(type: "INTEGER", nullable: false),
                free_space = table.Column<long>(type: "INTEGER", nullable: false),
                status = table.Column<string>(type: "TEXT", nullable: false),
                last_checked_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_storage_drives", x => x.id);
            });

        // Add indexes
        migrationBuilder.CreateIndex(
            name: "IX_cameras_is_online",
            table: "cameras",
            column: "is_online");

        migrationBuilder.CreateIndex(
            name: "IX_storage_drives_status",
            table: "storage_drives",
            column: "status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop storage_drives table
        migrationBuilder.DropTable(
            name: "storage_drives");

        // Drop indexes
        migrationBuilder.DropIndex(
            name: "IX_cameras_is_online",
            table: "cameras");

        // Remove columns from cameras table
        migrationBuilder.DropColumn(
            name: "is_online",
            table: "cameras");

        migrationBuilder.DropColumn(
            name: "last_online_at",
            table: "cameras");

        migrationBuilder.DropColumn(
            name: "last_downloaded_at",
            table: "cameras");
    }
}