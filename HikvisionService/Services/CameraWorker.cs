
using System.IO;
using System.Collections.Concurrent;
using HikvisionService.Data;
using HikvisionService.Models;
using Microsoft.EntityFrameworkCore;
using Hik.Api;
using Hik.Api.Data;

namespace HikvisionService.Services;
public class CameraWorker : BackgroundService
{
    private readonly long _cameraId;
    private readonly IServiceProvider _services;
    private readonly ILogger<CameraWorker> _logger;
    private readonly SemaphoreSlim _downloadSemaphore;
    private readonly CancellationTokenSource _shutdownCts = new();
    private Task? _executingTask;
    private bool _isStopping;
    private int healthCheckInterval;
    private int maxPerCamera;
    private int _activeDownloadCount = 0;
    private DateTime? _lastCheckTime;
    private string? _lastError;

    // Public property to access the camera ID
    public long CameraId => _cameraId;

    public CameraWorker(
        long cameraId,
        IServiceProvider services,
        ILogger<CameraWorker> logger,
        IConfiguration configuration)
    {
        _cameraId = cameraId;
        _services = services;
        _logger = logger;
        
        healthCheckInterval = configuration.GetValue<int>("CameraHealthCheck:IntervalMinutes", 5);
        maxPerCamera = configuration.GetValue<int>("CameraWorker:MaxConcurrentDownloadsPerCamera", 1);
        _downloadSemaphore = new SemaphoreSlim(maxPerCamera, maxPerCamera);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CameraWorker for Camera {CameraId} started", _cameraId);
        
        // Link the shutdown token with the stopping token
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            stoppingToken, _shutdownCts.Token);
        var linkedToken = linkedCts.Token;

        // Store the executing task
        _executingTask = ExecuteInternalAsync(linkedToken);
        return _executingTask;
    }

    private async Task ExecuteInternalAsync(CancellationToken linkedToken)
    {
        try
        {
            while (!linkedToken.IsCancellationRequested && !_isStopping)
            {
                try
                {
                    await ProcessCameraAsync(linkedToken);
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing camera {CameraId}", _cameraId);
                }

                // Check interval - but respect cancellation
                var interval = TimeSpan.FromSeconds(60 * healthCheckInterval); // Get from config
                await Task.Delay(interval, linkedToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        finally
        {
            _logger.LogInformation("CameraWorker for Camera {CameraId} stopped", _cameraId);
            
            // Release any semaphore waits
            try { _downloadSemaphore.Release(); } catch { /* Ignore */ }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping CameraWorker for Camera {CameraId}...", _cameraId);
        
        _isStopping = true;
        
        // Signal the worker to stop
        _shutdownCts.Cancel();
        
        // Wait for the executing task to complete (with timeout)
        if (_executingTask != null)
        {
            try
            {
                await _executingTask.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("CameraWorker for Camera {CameraId} did not stop gracefully", _cameraId);
            }
        }
        
        _shutdownCts.Dispose();
        _downloadSemaphore.Dispose();
        
        _logger.LogInformation("CameraWorker for Camera {CameraId} stopped", _cameraId);
    }

    private async Task ProcessCameraAsync(CancellationToken stoppingToken)
    {
         _lastCheckTime = DateTime.UtcNow;
        try
        {
            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<HikvisionDbContext>();

            // Get fresh camera data
            var camera = await dbContext.Cameras
                .FirstOrDefaultAsync(c => c.Id == _cameraId, stoppingToken);

            if (camera == null || !camera.IsOnline)
            {
                _logger.LogDebug("Camera {CameraId} is offline or not found", _cameraId);
                return;
            }

            // Check if storage is full
            if (await IsStorageFullAsync(dbContext, stoppingToken))
            {
                _logger.LogWarning("Storage full, attempting to clean up old files for camera {CameraId}", _cameraId);
                
                // Try to clean up old files
                await CleanupOldFilesAsync(camera, dbContext, stoppingToken);
                if (await IsStorageFullAsync(dbContext, stoppingToken))
                {
                    return;
                }
            }

            // Step 1: Look for new files (health check)
            await CheckForNewFilesAsync(camera, dbContext, stoppingToken);

            // Step 2: Process pending downloads for this camera
            await ProcessPendingDownloadsAsync(camera, dbContext, stoppingToken);
            
            _lastError = null; // Clear error on success
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            throw;
        }
    }

    private async Task<int> CleanupOldFilesAsync(Camera camera, HikvisionDbContext dbContext, CancellationToken stoppingToken)
    {
        int deletedCount = 0;
        
        // Get oldest completed files for this camera (limit to 10)
        var oldCompletedJobs = await dbContext.FileDownloadJobs
            .Where(j => j.CameraId == camera.Id 
                        && j.Status == "completed"
                        && j.EndTime != null)
            .OrderBy(j => j.EndTime) // Oldest first
            .Take(10)
            .ToListAsync(stoppingToken);

        if (!oldCompletedJobs.Any())
        {
            return 0;
        }

        foreach (var job in oldCompletedJobs)
        {
            try
            {
                // Delete the physical file if it exists
                if (File.Exists(job.DownloadPath))
                {
                    File.Delete(job.DownloadPath);
                    _logger.LogDebug("Deleted physical file: {FilePath}", job.DownloadPath);
                }

                // Remove the database record
                dbContext.FileDownloadJobs.Remove(job);
                deletedCount++;
                
                _logger.LogInformation("Cleaned up job {JobId} - {FileName} (completed at {EndTime})", 
                    job.Id, job.FileName, job.EndTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up job {JobId} for camera {CameraId}", 
                    job.Id, camera.Id);
            }
        }

        if (deletedCount > 0)
        {
            await dbContext.SaveChangesAsync(stoppingToken);
        }

        return deletedCount;
    }

    private async Task CheckForNewFilesAsync(Camera camera, HikvisionDbContext dbContext, CancellationToken stoppingToken)
    {
        Hik.Api.Abstraction.IHikApi hikApi = null;
        
        try
        {
            hikApi = HikApi.Login(
                camera.IpAddress,
                camera.Port,
                camera.Username ?? "admin",
                camera.Password ?? ""
            );

            DateTime startTime = camera.LastDownloadedAt?.AddMinutes(-5) ?? DateTime.UtcNow.AddHours(-720);
            DateTime endTime = DateTime.UtcNow;

            var files = new List<Hik.Api.Data.HikRemoteFile>();

            // Check if it's an NVR with IP channels
            if (hikApi.IpChannels.Any())
            {
                foreach (var channel in hikApi.IpChannels.Where(c => c.IsOnline))
                {
                    var videos = await hikApi.VideoService.FindFilesAsync(startTime, endTime, channel.ChannelNumber);
                    files.AddRange(videos);
                }
            }
            else
            {
                var videos = await hikApi.VideoService.FindFilesAsync(startTime, endTime);
                files.AddRange(videos);
            }

            if (files.Any())
            {
                _logger.LogInformation("Found {Count} new files for camera {CameraId}", files.Count, camera.Id);
                
                // Get existing files to avoid duplicates
                var existingFiles = (await dbContext.FileDownloadJobs
                    .Where(j => j.CameraId == camera.Id)
                    .Select(j => j.FileName)
                    .ToListAsync(stoppingToken))
                    .ToHashSet();

                var newJobs = files
                    .Where(f => !existingFiles.Contains(f.Name))
                    .Select(file => new FileDownloadJob
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
                    })
                    .ToList();

                if (newJobs.Any())
                {
                    await dbContext.FileDownloadJobs.AddRangeAsync(newJobs, stoppingToken);
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }

            // Update camera's last check time
            camera.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for new files on camera {CameraId}", camera.Id);
            
            // Update camera status if needed
            camera.LastError = ex.Message;
            camera.LastErrorAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(stoppingToken);
        }
        finally
        {
            if (hikApi != null)
            {
                try { hikApi.Logout(); } catch { /* Ignore logout errors */ }
            }
        }
    }

    private async Task ProcessPendingDownloadsAsync(Camera camera, HikvisionDbContext dbContext, CancellationToken stoppingToken)
    {
        // Get pending downloads for this camera only
        var pendingJobs = await dbContext.FileDownloadJobs
            .Where(j => j.CameraId == camera.Id && 
                       (j.Status == "pending" || j.Status == "failed"))
            .OrderBy(j => j.CreatedAt)
            .Take(maxPerCamera)
            .ToListAsync(stoppingToken);

        if (!pendingJobs.Any())
            return;

        // _logger.LogInformation("Camera {CameraId} has {Count} pending downloads", camera.Id, pendingJobs.Count);

        // Process downloads with semaphore to respect camera's connection limit
        var tasks = pendingJobs.Select(async job =>
        {
            await _downloadSemaphore.WaitAsync(stoppingToken);
            try
            {
                await DownloadFileAsync(job, camera, stoppingToken);
            }
            finally
            {
                _downloadSemaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task DownloadFileAsync(FileDownloadJob job, Camera camera, CancellationToken stoppingToken)
    {
        Interlocked.Increment(ref _activeDownloadCount);
        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HikvisionDbContext>();

        Hik.Api.Abstraction.IHikApi hikApi = null;
        
        try
        {
            _logger.LogInformation("Camera {CameraId} starting download: {FileName}", camera.Id, job.FileName);

            // Update job status
            job.Status = "downloading";
            job.StartTime = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(stoppingToken);

            // Login to camera
            hikApi = HikApi.Login(
                camera.IpAddress,
                camera.Port,
                camera.Username ?? "admin",
                camera.Password ?? ""
            );

            // Ensure directory exists
            string downloadDir = Path.GetDirectoryName(job.DownloadPath) ?? "downloads";
            Directory.CreateDirectory(downloadDir);

            string tempFilePath = $"{job.DownloadPath}.tmp";
            string finalFilePath = job.DownloadPath;

            // Start download
            var downloadId = hikApi.VideoService.StartDownloadFile(job.FileName, tempFilePath);

            // Monitor download progress
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
                
                int progress = hikApi.VideoService.GetDownloadPosition(downloadId);
                
                // Update progress
                job.Progress = progress;
                job.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(stoppingToken);

                if (progress >= 100)
                {
                    hikApi.VideoService.StopDownloadFile(downloadId);
                    
                    // Move file to final location
                    File.Move(tempFilePath, finalFilePath, true);
                    
                    // Update job as completed
                    job.Status = "completed";
                    job.EndTime = DateTime.UtcNow;
                    job.UpdatedAt = DateTime.UtcNow;
                    
                    // Update camera's last downloaded time
                    camera.LastDownloadedAt = DateTime.UtcNow;
                    camera.UpdatedAt = DateTime.UtcNow;
                    
                    await dbContext.SaveChangesAsync(stoppingToken);
                    
                    _logger.LogInformation("Camera {CameraId} completed download: {FileName}", camera.Id, job.FileName);
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            job.Status = "pending";
            job.ErrorMessage = "Download cancelled";
            job.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(stoppingToken);
            
            _logger.LogWarning("Camera {CameraId} download cancelled: {FileName}", camera.Id, job.FileName);
        }
        catch (Exception ex)
        {
            job.Status = "failed";
            job.ErrorMessage = ex.Message;
            job.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(stoppingToken);
            
            _logger.LogError(ex, "Camera {CameraId} download failed: {FileName}", camera.Id, job.FileName);
        }
        finally
        {
            Interlocked.Decrement(ref _activeDownloadCount);
            if (hikApi != null)
            {
                try { hikApi.Logout(); } catch { /* Ignore logout errors */ }
            }
        }
    }

    private async Task<bool> IsStorageFullAsync(HikvisionDbContext dbContext, CancellationToken stoppingToken)
    {
        var drives = await dbContext.StorageDrives.ToListAsync(stoppingToken);
        return drives.Any(d => StorageMonitoringService.IsDriveCriticallyFull(d));
    }
        
    public int GetActiveDownloadCount()
    {
        return Interlocked.CompareExchange(ref _activeDownloadCount, 0, 0);
    }
    
    // Add this method to get full status
    public (int ActiveDownloadCount, DateTime? LastCheckTime, string? LastError) GetWorkerStats()
    {
        return (_activeDownloadCount, _lastCheckTime, _lastError);
    }
}