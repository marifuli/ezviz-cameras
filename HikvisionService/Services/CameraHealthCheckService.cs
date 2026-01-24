using HikvisionService.Data;
using HikvisionService.Models;
using Microsoft.EntityFrameworkCore;
using Hik.Api;

namespace HikvisionService.Services;

public class CameraHealthCheckService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CameraHealthCheckService> _logger;
    private readonly TimeSpan _checkInterval;

    public CameraHealthCheckService(
        IServiceScopeFactory scopeFactory,
        ILogger<CameraHealthCheckService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        
        // Get check interval from configuration, default to 5 minutes
        int intervalMinutes = configuration.GetValue<int>("CameraHealthCheck:IntervalMinutes", 5);
        _checkInterval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Camera Health Check Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckCamerasHealthAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking camera health");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Camera Health Check Service is stopping");
    }

    public async Task CheckCamerasHealthAsync()
    {
        _logger.LogInformation("Starting camera health check");

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HikvisionDbContext>();

        var cameras = await dbContext.Cameras.ToListAsync();
        _logger.LogInformation("Found {CameraCount} cameras to check", cameras.Count);

        foreach (var camera in cameras)
        {
            try
            {
                bool wasOnline = camera.IsOnline;
                bool isNowOnline = await CheckCameraConnectionAsync(camera);

                // Update camera status
                camera.IsOnline = isNowOnline;
                
                if (isNowOnline)
                {
                    camera.LastOnlineAt = DateTime.UtcNow;
                    
                    // If camera was offline and is now online, log the recovery
                    if (!wasOnline)
                    {
                        _logger.LogInformation("Camera {CameraName} ({CameraId}) is back online", camera.Name, camera.Id);
                    }
                }
                else if (wasOnline)
                {
                    // If camera was online and is now offline, log the failure
                    _logger.LogWarning("Camera {CameraName} ({CameraId}) went offline", camera.Name, camera.Id);
                }

                camera.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking camera {CameraName} ({CameraId})", camera.Name, camera.Id);
            }
        }

        _logger.LogInformation("Completed camera health check");
    }

    private async Task<bool> CheckCameraConnectionAsync(Camera camera)
    {
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
            
            // Check for available footage if needed
            await CheckForNewFootageAsync(camera, hikApi);
            
            // Logout and cleanup
            hikApi.Logout();
            HikApi.Cleanup();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Camera {CameraName} ({CameraId}) is offline: {ErrorMessage}", 
                camera.Name, camera.Id, ex.Message);
            
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

    private async Task CheckForNewFootageAsync(Camera camera, Hik.Api.Abstraction.IHikApi hikApi)
    {
        try
        {
            // Get the last 30 days of footage if LastDownloadedAt is null
            // Otherwise, get footage since the last download
            DateTime startTime = camera.LastDownloadedAt?.AddMinutes(-5) ?? DateTime.UtcNow.AddHours(-720);
            DateTime endTime = DateTime.UtcNow;

            var files = new List<Hik.Api.Data.HikRemoteFile>();

            // Check if it's an NVR with IP channels
            if (hikApi.IpChannels.Any())
            {
                foreach (var channel in hikApi.IpChannels.Where(c => c.IsOnline))
                {
                    // Get videos for this channel
                    var videos = await hikApi.VideoService.FindFilesAsync(startTime, endTime, channel.ChannelNumber);
                    files.AddRange(videos);
                }
            }
            else
            {
                // Direct camera connection
                var videos = await hikApi.VideoService.FindFilesAsync(startTime, endTime);
                files.AddRange(videos);
            }

            // If we found new files, create download jobs for them
            if (files.Any())
            {
                _logger.LogInformation("Found {FileCount} new files for camera {CameraName} ({CameraId})", 
                    files.Count, camera.Name, camera.Id);
                
                // This will be handled by the DownloadJobService
                // We just update the camera to indicate it has new data
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<HikvisionDbContext>();
                
                // Create download jobs for each file
                foreach (var file in files)
                {
                    // Check if a job already exists for this file
                    bool jobExists = await dbContext.FileDownloadJobs.AnyAsync(j =>
                        j.CameraId == camera.Id &&
                        j.FileName == file.Name);
                    
                    if (!jobExists)
                    {
                        // Create a new download job
                        var job = new FileDownloadJob
                        {
                            CameraId = camera.Id,
                            FileName = file.Name,
                            FileType = "video",
                            FileSize = file.Size,
                            DownloadPath = $"downloads/{camera.Id}/{file.Name}.mp4",
                            Status = "pending",
                            Progress = 0,
                            FileStartTime = file.Date,
                            FileEndTime = file.Date.AddSeconds(file.Duration),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        
                        dbContext.FileDownloadJobs.Add(job);
                    }
                }
                
                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for new footage for camera {CameraName} ({CameraId})", 
                camera.Name, camera.Id);
        }
    }
}