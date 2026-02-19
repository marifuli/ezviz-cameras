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
            
            // Apply migrations using raw SQL instead of EF Core migrations
            ApplyCameraStatusMigration(dbContext);
            ApplyStorageDriveMigration(dbContext);
            ApplyCameraHealthCheckMigration(dbContext);
            
            logger.LogInformation("Migrations applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying migrations");
        }
    }
    
    private static void ApplyCameraStatusMigration(HikvisionDbContext dbContext)
    {
        // Check if columns already exist
        bool columnsExist = false;
        try
        {
            // Try to query one of the new columns
            _ = dbContext.Cameras.FirstOrDefault(c => c.IsOnline);
            columnsExist = true;
        }
        catch
        {
            columnsExist = false;
        }
        
        if (!columnsExist)
        {
            // Add new columns to cameras table
            dbContext.Database.ExecuteSqlRaw(@"
                ALTER TABLE cameras ADD COLUMN is_online INTEGER NOT NULL DEFAULT 0;
                ALTER TABLE cameras ADD COLUMN last_online_at TEXT NULL;
                ALTER TABLE cameras ADD COLUMN last_downloaded_at TEXT NULL;
                CREATE INDEX IX_cameras_is_online ON cameras(is_online);
            ");
        }
    }
    
    private static void ApplyStorageDriveMigration(HikvisionDbContext dbContext)
    {
        // Check if table exists
        bool tableExists = false;
        try
        {
            _ = dbContext.StorageDrives.FirstOrDefault();
            tableExists = true;
        }
        catch
        {
            tableExists = false;
        }
        
        if (!tableExists)
        {
            // Create storage_drives table
            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE storage_drives (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    root_path TEXT NOT NULL,
                    total_space INTEGER NOT NULL DEFAULT 0,
                    used_space INTEGER NOT NULL DEFAULT 0,
                    free_space INTEGER NOT NULL DEFAULT 0,
                    status TEXT NOT NULL DEFAULT 'Normal',
                    last_checked_at TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );
                CREATE INDEX IX_storage_drives_status ON storage_drives(status);
            ");
        }
    }
    
    private static void ApplyCameraHealthCheckMigration(HikvisionDbContext dbContext)
    {
        // Add new columns to cameras table
        dbContext.Database.ExecuteSqlRaw(@"
        -- Check if column exists before adding
        SELECT CASE 
            WHEN NOT EXISTS (SELECT * FROM pragma_table_info('cameras') WHERE name = 'last_error')
            THEN 'ALTER TABLE cameras ADD COLUMN last_error TEXT NULL;'
            ELSE '-- Column last_error already exists'
        END;

        SELECT CASE 
            WHEN NOT EXISTS (SELECT * FROM pragma_table_info('cameras') WHERE name = 'last_error_at')
            THEN 'ALTER TABLE cameras ADD COLUMN last_error_at TEXT NULL;'
            ELSE '-- Column last_error_at already exists'
        END;

        SELECT CASE 
            WHEN NOT EXISTS (SELECT * FROM pragma_table_info('cameras') WHERE name = 'last_health_check_at')
            THEN 'ALTER TABLE cameras ADD COLUMN last_health_check_at TEXT NULL;'
            ELSE '-- Column last_health_check_at already exists'
        END;
        ");
    }
}