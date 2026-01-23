using Microsoft.EntityFrameworkCore;
using HikvisionService.Data;
using HikvisionService.Models;
using HikvisionService.Models.ViewModels;
using Hik.Api;
using Hik.Api.Data;

namespace HikvisionService.Services;

public class HikvisionService : IHikvisionService
{
    private readonly HikvisionDbContext _context;
    private readonly ILogger<HikvisionService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public HikvisionService(
        HikvisionDbContext context,
        ILogger<HikvisionService> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<List<HikRemoteFile>> GetAvailableFilesAsync(long cameraId, DateTime startTime, DateTime endTime, string fileType = "both")
    {
        var camera = await GetCameraByIdAsync(cameraId);
        if (camera == null)
        {
            _logger.LogWarning("GetAvailableFilesAsync: Camera with ID {CameraId} not found", cameraId);
            return new List<HikRemoteFile>();
        }

        var files = new List<HikRemoteFile>();

        try
        {
            // Set the library path to the current directory
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            HikApi.SetLibraryPath(currentDirectory);
            
            // Initialize with proper logging and force reinitialization
            HikApi.Initialize(
                logLevel: 3, 
                logDirectory: "logs", 
                autoDeleteLogs: true,
                waitTimeMilliseconds: 5000, // Increase timeout for better reliability
                forceReinitialization: true // Force reinitialization to ensure clean state
            );

            // Login to the camera
            var hikApi = HikApi.Login(camera.IpAddress, camera.Port, camera.Username ?? "admin", camera.Password ?? "");
            _logger.LogInformation("GetAvailableFilesAsync: Successfully connected to camera {CameraName} at {IpAddress}:{Port}", camera.Name, camera.IpAddress, camera.Port);

            try
            {
                // Check if it's an NVR with IP channels
                if (hikApi.IpChannels.Any())
                {
                    foreach (var channel in hikApi.IpChannels.Where(c => c.IsOnline))
                    {
                        if (fileType is "both" or "video")
                        {
                            var videos = await hikApi.VideoService.FindFilesAsync(startTime, endTime, channel.ChannelNumber);
                            files.AddRange(videos);
                            _logger.LogInformation("GetAvailableFilesAsync: Found {VideoCount} videos for channel {ChannelNumber}", videos.Count, channel.ChannelNumber);
                        }

                        if (fileType is "both" or "photo")
                        {
                            // Note: Photos might not be available per channel basis on NVR
                            try 
                            {
                                var photos = await hikApi.PhotoService.FindFilesAsync(startTime, endTime);
                                files.AddRange(photos);
                                _logger.LogInformation("GetAvailableFilesAsync: Found {PhotoCount} photos for channel {ChannelNumber}", photos.Count, channel.ChannelNumber);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "GetAvailableFilesAsync: Failed to get photos from channel {ChannelNumber}", channel.ChannelNumber);
                            }
                        }
                    }
                }
                else
                {
                    // Direct camera connection
                    if (fileType is "both" or "video")
                    {
                        var videos = await hikApi.VideoService.FindFilesAsync(startTime, endTime);
                        files.AddRange(videos);
                        _logger.LogInformation("GetAvailableFilesAsync: Found {VideoCount} videos from camera", videos.Count);
                    }

                    if (fileType is "both" or "photo")
                    {
                        var photos = await hikApi.PhotoService.FindFilesAsync(startTime, endTime);
                        files.AddRange(photos);
                        _logger.LogInformation("GetAvailableFilesAsync: Found {PhotoCount} photos from camera", photos.Count);
                    }
                }
            }
            finally
            {
                // Always logout when done with the camera
                hikApi.Logout();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAvailableFilesAsync: Failed to get files from camera {CameraId}: {ErrorMessage}", cameraId, ex.Message);
        }
        finally
        {
            // Always clean up SDK resources, even if an exception occurs
            try
            {
                HikApi.Cleanup();
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "GetAvailableFilesAsync: Failed to clean up Hikvision SDK resources");
            }
        }

        return files;
    }

    public async Task<List<string>> TestCameraConnectionAsync(long cameraId)
    {
        var list = new List<string>();
        var camera = await GetCameraByIdAsync(cameraId);
        if (camera == null)
        {
            list.Add("Camera not found");
            return list;
        }

        try
        {
            // Set the library path to the current directory
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            HikApi.SetLibraryPath(currentDirectory);
            
            // Initialize with proper logging and force reinitialization
            HikApi.Initialize(
                logLevel: 3, 
                logDirectory: "logs", 
                autoDeleteLogs: true,
                waitTimeMilliseconds: 5000, // Increase timeout for better reliability
                forceReinitialization: true // Force reinitialization to ensure clean state
            );
            // Login to camera
            var hikApi = HikApi.Login(camera.IpAddress, camera.Port, camera.Username ?? "admin", camera.Password ?? "");
            
            // Try to get camera time to test connection
            // var cameraTime = hikApi.ConfigService.GetTime();
            _logger.LogInformation("Camera {CameraName} connection test successful", camera.Name);
            foreach (var channel in hikApi.IpChannels)
            {
                list.Add($"Channel: {channel.Name} {channel.ChannelNumber}; IsOnline: {channel.IsOnline}");
            }
            
            hikApi.Logout();
        }
        catch (Exception ex)
        {
            list.Add($"Camera {camera.Name} connection test failed: {ex.Message}");
        }
        finally
        {
            // Always clean up SDK resources, even if an exception occurs
            try
            {
                HikApi.Cleanup();
            }
            catch (Exception cleanupEx)
            {
                list.Add($"Failed to clean up Hikvision SDK resources: {cleanupEx.Message}");
            }
        }
        return list;
    }

    public async Task<List<Camera>> GetAllCamerasAsync()
    {
        return await _context.Cameras
            .Include(c => c.Store)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Camera?> GetCameraByIdAsync(long id)
    {
        return await _context.Cameras
            .Include(c => c.Store)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    // Camera health check methods
    public async Task<bool> CheckCameraConnectionAsync(long cameraId)
    {
        var camera = await GetCameraByIdAsync(cameraId);
        if (camera == null)
        {
            _logger.LogWarning("CheckCameraConnectionAsync: Camera with ID {CameraId} not found", cameraId);
            return false;
        }

        try
        {
            // Set the library path to the current directory
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            HikApi.SetLibraryPath(currentDirectory);
            
            // Initialize with proper logging and force reinitialization
            HikApi.Initialize(
                logLevel: 3,
                logDirectory: "logs",
                autoDeleteLogs: true,
                waitTimeMilliseconds: 5000,
                forceReinitialization: true
            );

            // Login to camera with a short timeout
            var hikApi = HikApi.Login(
                camera.IpAddress,
                camera.Port,
                camera.Username ?? "admin",
                camera.Password ?? ""
            );
            
            // If we get here, the camera is online
            _logger.LogDebug("Camera {CameraName} ({CameraId}) is online", camera.Name, camera.Id);
            
            // Update camera status
            camera.IsOnline = true;
            camera.LastOnlineAt = DateTime.UtcNow;
            camera.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            // Logout and cleanup
            hikApi.Logout();
            HikApi.Cleanup();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Camera {CameraName} ({CameraId}) is offline: {ErrorMessage}",
                camera.Name, camera.Id, ex.Message);
            
            // Update camera status
            camera.IsOnline = false;
            camera.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            // Ensure cleanup even if an exception occurs
            try
            {
                HikApi.Cleanup();
            }
            catch
            {
                // Ignore cleanup errors
            }
            
            return false;
        }
    }

    public async Task TriggerCameraHealthCheckAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var cameraHealthCheckService = scope.ServiceProvider.GetRequiredService<CameraHealthCheckService>();
        await cameraHealthCheckService.CheckCamerasHealthAsync();
    }

    // Storage drive methods
    public async Task<List<StorageDrive>> GetAllStorageDrivesAsync()
    {
        return await _context.StorageDrives
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<StorageDrive?> GetStorageDriveByIdAsync(long id)
    {
        return await _context.StorageDrives
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<StorageDrive> AddStorageDriveAsync(StorageDrive drive)
    {
        drive.CreatedAt = DateTime.UtcNow;
        drive.UpdatedAt = DateTime.UtcNow;
        drive.LastCheckedAt = DateTime.UtcNow;
        
        _context.StorageDrives.Add(drive);
        await _context.SaveChangesAsync();
        
        return drive;
    }

    public async Task<bool> UpdateStorageDriveAsync(StorageDrive drive)
    {
        var existingDrive = await _context.StorageDrives.FindAsync(drive.Id);
        if (existingDrive == null)
        {
            return false;
        }
        
        existingDrive.Name = drive.Name;
        existingDrive.RootPath = drive.RootPath;
        existingDrive.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteStorageDriveAsync(long id)
    {
        var drive = await _context.StorageDrives.FindAsync(id);
        if (drive == null)
        {
            return false;
        }
        
        _context.StorageDrives.Remove(drive);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task TriggerStorageCheckAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var storageMonitoringService = scope.ServiceProvider.GetRequiredService<StorageMonitoringService>();
        await storageMonitoringService.CheckStorageDrivesAsync();
    }

    // Download job methods
    public async Task<List<FileDownloadJob>> GetAllDownloadJobsAsync()
    {
        return await _context.FileDownloadJobs
            .Include(j => j.Camera)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<FileDownloadJob>> GetActiveDownloadJobsAsync()
    {
        return await _context.FileDownloadJobs
            .Include(j => j.Camera)
            .Where(j => j.Status == "downloading")
            .OrderByDescending(j => j.StartTime)
            .ToListAsync();
    }

    public async Task<List<FileDownloadJob>> GetFailedDownloadJobsAsync()
    {
        return await _context.FileDownloadJobs
            .Include(j => j.Camera)
            .Where(j => j.Status == "failed")
            .OrderByDescending(j => j.UpdatedAt)
            .ToListAsync();
    }
    
    public async Task<List<FileDownloadJob>> GetCompletedDownloadJobsAsync()
    {
        return await _context.FileDownloadJobs
            .Include(j => j.Camera)
            .Where(j => j.Status == "completed")
            .OrderByDescending(j => j.EndTime)
            .ToListAsync();
    }

    public async Task<FileDownloadJob?> GetDownloadJobByIdAsync(long id)
    {
        return await _context.FileDownloadJobs
            .Include(j => j.Camera)
            .FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<bool> RetryDownloadJobAsync(long id)
    {
        using var scope = _serviceProvider.CreateScope();
        var downloadJobService = scope.ServiceProvider.GetRequiredService<DownloadJobService>();
        await downloadJobService.RetryJobAsync(id);
        return true;
    }

    public async Task<bool> CancelDownloadJobAsync(long id)
    {
        using var scope = _serviceProvider.CreateScope();
        var downloadJobService = scope.ServiceProvider.GetRequiredService<DownloadJobService>();
        await downloadJobService.CancelJobAsync(id);
        return true;
    }

    // Dashboard methods
    public async Task<DashboardViewModel> GetDashboardDataAsync()
    {
        var dashboard = new DashboardViewModel();
        
        // Get camera statistics
        var cameras = await _context.Cameras.ToListAsync();
        dashboard.TotalCameras = cameras.Count;
        dashboard.OnlineCameras = cameras.Count(c => c.IsOnline);
        dashboard.OfflineCameras = cameras.Count(c => !c.IsOnline);
        
        // Get job statistics
        dashboard.ActiveDownloadJobs = await _context.FileDownloadJobs
            .CountAsync(j => j.Status == "downloading");
        dashboard.FailedDownloadJobs = await _context.FileDownloadJobs
            .CountAsync(j => j.Status == "failed");
        dashboard.CompletedDownloadJobs = await _context.FileDownloadJobs
            .CountAsync(j => j.Status == "completed");
        
        // Get offline cameras list
        var offlineCameras = await _context.Cameras
            .Where(c => !c.IsOnline)
            .OrderByDescending(c => c.LastOnlineAt)
            .Take(10)
            .ToListAsync();
        
        foreach (var camera in offlineCameras)
        {
            var cameraStatus = new CameraStatusViewModel
            {
                Id = camera.Id,
                Name = camera.Name,
                IsOnline = camera.IsOnline,
                LastOnlineAt = camera.LastOnlineAt,
                LastDownloadedAt = camera.LastDownloadedAt,
                Status = await GetCameraStatusAsync(camera.Id)
            };
            
            dashboard.OfflineCamerasList.Add(cameraStatus);
        }
        
        // Get storage drives
        var drives = await _context.StorageDrives.ToListAsync();
        foreach (var drive in drives)
        {
            var driveViewModel = new StorageDriveViewModel
            {
                Id = drive.Id,
                Name = drive.Name,
                RootPath = drive.RootPath,
                TotalSpace = drive.TotalSpace,
                UsedSpace = drive.UsedSpace,
                FreeSpace = drive.FreeSpace,
                Status = drive.Status,
                LastCheckedAt = drive.LastCheckedAt
            };
            
            dashboard.StorageDrives.Add(driveViewModel);
        }
        
        // Generate chart data for downloads per day (last 7 days)
        var lastWeek = DateTime.UtcNow.AddDays(-7);
        var completedJobs = await _context.FileDownloadJobs
            .Where(j => j.Status == "completed" && j.EndTime >= lastWeek)
            .ToListAsync();
            
        var downloadsPerDay = completedJobs
            .GroupBy(j => j.EndTime?.Date)
            .Select(g => new ChartDataPoint {
                Label = g.Key?.ToString("yyyy-MM-dd") ?? "Unknown",
                Value = g.Count()
            })
            .OrderBy(p => p.Label)
            .ToList();
            
        // Add missing days with zero downloads
        for (int i = 0; i < 7; i++)
        {
            var date = DateTime.UtcNow.AddDays(-i).Date.ToString("yyyy-MM-dd");
            if (!downloadsPerDay.Any(p => p.Label == date))
            {
                downloadsPerDay.Add(new ChartDataPoint { Label = date, Value = 0 });
            }
        }
        
        dashboard.DownloadsPerDay = downloadsPerDay.OrderBy(p => p.Label).ToList();
        
        // Generate storage usage trend data (if available)
        // This would require historical storage data which might not be available
        // For now, we'll just use current values
        foreach (var drive in dashboard.StorageDrives)
        {
            dashboard.StorageUsageTrend.Add(new ChartDataPoint {
                Label = drive.Name,
                Value = drive.UsagePercentage,
                Category = "Current"
            });
        }
        
        // Generate downloads per camera statistics
        var downloadsPerCamera = await _context.FileDownloadJobs
            .Where(j => j.Status == "completed")
            .Include(j => j.Camera)
            .GroupBy(j => j.Camera.Name)
            .Select(g => new { CameraName = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CameraName, x => x.Count);
            
        dashboard.DownloadsPerCamera = downloadsPerCamera;
        
        return dashboard;
    }
    
    private async Task<string> GetCameraStatusAsync(long cameraId)
    {
        // Check if camera has active downloads
        bool hasActiveDownloads = await _context.FileDownloadJobs
            .AnyAsync(j => j.CameraId == cameraId && j.Status == "downloading");
        
        if (hasActiveDownloads)
        {
            return "Downloading";
        }
        
        return "Idle";
    }
    
    // Footage methods
    public async Task<List<FootageFileViewModel>> GetFootageFilesAsync(long? cameraId, DateTime startDate, DateTime endDate, string fileType = "both")
    {
        var result = new List<FootageFileViewModel>();
        
        try
        {
            // Get completed download jobs that match the criteria
            var query = _context.FileDownloadJobs
                .Include(j => j.Camera)
                .Where(j => j.Status == "completed" &&
                           j.FileStartTime >= startDate &&
                           j.FileEndTime <= endDate);
                           
            if (cameraId.HasValue)
            {
                query = query.Where(j => j.CameraId == cameraId.Value);
            }
            
            if (fileType != "both")
            {
                query = query.Where(j => j.FileType == fileType);
            }
            
            var jobs = await query.OrderByDescending(j => j.FileStartTime).ToListAsync();
            
            foreach (var job in jobs)
            {
                var footageFile = new FootageFileViewModel
                {
                    FileName = job.FileName,
                    FilePath = job.DownloadPath,
                    FileType = job.FileType,
                    FileSize = job.FileSize,
                    FileStartTime = job.FileStartTime,
                    FileEndTime = job.FileEndTime,
                    CameraName = job.Camera.Name,
                    CameraId = job.CameraId,
                    // Generate a thumbnail path (this would need to be implemented)
                    ThumbnailPath = job.FileType == "video" ? "/thumbnails/video-placeholder.jpg" : job.DownloadPath,
                    // Generate a download URL
                    DownloadUrl = $"/api/footage/download?path={Uri.EscapeDataString(job.DownloadPath)}"
                };
                
                result.Add(footageFile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting footage files");
        }
        
        return result;
    }
    
    public Task<string> GetFootageDownloadUrlAsync(string filePath)
    {
        // Check if the file exists
        if (!System.IO.File.Exists(filePath))
        {
            throw new FileNotFoundException("Footage file not found", filePath);
        }
        
        // Return the file path - the actual download will be handled by the controller
        return Task.FromResult(filePath);
    }
}