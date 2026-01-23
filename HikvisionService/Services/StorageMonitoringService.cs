using System.IO;
using HikvisionService.Data;
using HikvisionService.Models;
using Microsoft.EntityFrameworkCore;

namespace HikvisionService.Services;

public class StorageMonitoringService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StorageMonitoringService> _logger;
    private readonly TimeSpan _checkInterval;

    // Thresholds for storage usage
    private const double WARNING_THRESHOLD = 70.0; // 70%
    private const double CRITICAL_THRESHOLD = 85.0; // 85%
    private const double FULL_THRESHOLD = 95.0; // 95%

    public StorageMonitoringService(
        IServiceScopeFactory scopeFactory,
        ILogger<StorageMonitoringService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        
        // Get check interval from configuration, default to 15 minutes
        int intervalMinutes = configuration.GetValue<int>("StorageMonitoring:IntervalMinutes", 15);
        _checkInterval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Storage Monitoring Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckStorageDrivesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking storage drives");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Storage Monitoring Service is stopping");
    }

    public async Task CheckStorageDrivesAsync()
    {
        _logger.LogInformation("Starting storage drives check");

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HikvisionDbContext>();

        var drives = await dbContext.StorageDrives.ToListAsync();
        
        // If no drives are configured, check if we need to add the default drive
        if (!drives.Any())
        {
            _logger.LogInformation("No storage drives configured. Adding default drive");
            await AddDefaultDriveAsync(dbContext);
            drives = await dbContext.StorageDrives.ToListAsync();
        }

        foreach (var drive in drives)
        {
            try
            {
                await UpdateDriveInfoAsync(drive, dbContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking drive {DriveName} ({DrivePath})", drive.Name, drive.RootPath);
            }
        }

        _logger.LogInformation("Completed storage drives check");
    }

    private async Task AddDefaultDriveAsync(HikvisionDbContext dbContext)
    {
        try
        {
            // Get the directory where the application is running
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string driveLetter = Path.GetPathRoot(appDirectory) ?? "C:\\";
            
            var drive = new StorageDrive
            {
                Name = "Default Drive",
                RootPath = driveLetter,
                Status = "Normal",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastCheckedAt = DateTime.UtcNow
            };
            
            dbContext.StorageDrives.Add(drive);
            await dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Added default drive {DrivePath}", driveLetter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding default drive");
        }
    }

    private async Task UpdateDriveInfoAsync(StorageDrive drive, HikvisionDbContext dbContext)
    {
        try
        {
            var driveInfo = new DriveInfo(drive.RootPath);
            
            if (!driveInfo.IsReady)
            {
                _logger.LogWarning("Drive {DriveName} ({DrivePath}) is not ready", drive.Name, drive.RootPath);
                drive.Status = "Error";
                drive.UpdatedAt = DateTime.UtcNow;
                drive.LastCheckedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
                return;
            }
            
            // Update drive information
            drive.TotalSpace = driveInfo.TotalSize;
            drive.FreeSpace = driveInfo.AvailableFreeSpace;
            drive.UsedSpace = driveInfo.TotalSize - driveInfo.AvailableFreeSpace;
            drive.LastCheckedAt = DateTime.UtcNow;
            drive.UpdatedAt = DateTime.UtcNow;
            
            // Calculate usage percentage
            double usagePercentage = (double)drive.UsedSpace / drive.TotalSpace * 100;
            
            // Update status based on thresholds
            string previousStatus = drive.Status;
            
            if (usagePercentage >= FULL_THRESHOLD)
            {
                drive.Status = "Full";
                _logger.LogWarning("Drive {DriveName} ({DrivePath}) is FULL: {UsagePercentage:F1}%", 
                    drive.Name, drive.RootPath, usagePercentage);
            }
            else if (usagePercentage >= CRITICAL_THRESHOLD)
            {
                drive.Status = "Critical";
                _logger.LogWarning("Drive {DriveName} ({DrivePath}) is at CRITICAL level: {UsagePercentage:F1}%", 
                    drive.Name, drive.RootPath, usagePercentage);
            }
            else if (usagePercentage >= WARNING_THRESHOLD)
            {
                drive.Status = "Warning";
                _logger.LogInformation("Drive {DriveName} ({DrivePath}) is at WARNING level: {UsagePercentage:F1}%", 
                    drive.Name, drive.RootPath, usagePercentage);
            }
            else
            {
                drive.Status = "Normal";
                _logger.LogDebug("Drive {DriveName} ({DrivePath}) is at NORMAL level: {UsagePercentage:F1}%", 
                    drive.Name, drive.RootPath, usagePercentage);
            }
            
            // Log status changes
            if (previousStatus != drive.Status)
            {
                _logger.LogInformation("Drive {DriveName} ({DrivePath}) status changed from {PreviousStatus} to {CurrentStatus}", 
                    drive.Name, drive.RootPath, previousStatus, drive.Status);
            }
            
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating drive info for {DriveName} ({DrivePath})", drive.Name, drive.RootPath);
        }
    }
    
    public static bool IsDriveCriticallyFull(StorageDrive drive)
    {
        if (drive == null)
            return false;
            
        double usagePercentage = (double)drive.UsedSpace / drive.TotalSpace * 100;
        return usagePercentage >= FULL_THRESHOLD;
    }
}