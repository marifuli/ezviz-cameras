using Microsoft.EntityFrameworkCore;

namespace HikvisionService.Data;

public static class MigrationRunner
{
    public static void ApplyMigrations(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HikvisionDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<HikvisionDbContext>>();

        try
        {
            logger.LogInformation("Applying migrations...");
            
            // Apply the migration
            var migration = new Migrations.AddCameraStatusAndStorageDrive();
            migration.Up(dbContext.Database.GetMigrationBuilder());
            
            logger.LogInformation("Migrations applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying migrations");
        }
    }
}