
using System.IO;
using System.Collections.Concurrent;
using HikvisionService.Data;
using HikvisionService.Models;
using Microsoft.EntityFrameworkCore;
using Hik.Api;
using Hik.Api.Data;

namespace HikvisionService.Services;

public class DownloadJobService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DownloadJobService> _logger;
    private readonly TimeSpan _checkInterval;
    private readonly int _maxConcurrentDownloads;
    private readonly SemaphoreSlim _downloadSemaphore;
    private readonly ConcurrentDictionary<long, CancellationTokenSource> _activeDownloads = new();
    private readonly ConcurrentDictionary<long, byte> _activeCameras = new();

    public DownloadJobService(
        IServiceScopeFactory scopeFactory,
        ILogger<DownloadJobService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        
        // Get check interval from configuration, default to 1 minute
        int intervalSeconds = configuration.GetValue<int>("DownloadJob:IntervalSeconds", 60);
        _checkInterval = TimeSpan.FromSeconds(intervalSeconds);
        
        // Get max concurrent downloads from configuration, default to 2
        _maxConcurrentDownloads = configuration.GetValue<int>("DownloadJob:MaxConcurrentDownloads", 120);
        _downloadSemaphore = new SemaphoreSlim(_maxConcurrentDownloads, _maxConcurrentDownloads);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Download Job Service is starting with {MaxConcurrentDownloads} max concurrent downloads", 
            _maxConcurrentDownloads);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing download jobs");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        // Cancel all active downloads when service is stopping
        foreach (var cts in _activeDownloads.Values)
        {
            cts.Cancel();
        }

        _logger.LogInformation("Download Job Service is stopping");
    }

    public async Task ProcessPendingJobsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HikvisionDbContext>();

        // Check if any storage drive is critically full
        var drives = await dbContext.StorageDrives.ToListAsync(stoppingToken);
        bool anyCriticallyFull = drives.Any(d => StorageMonitoringService.IsDriveCriticallyFull(d));
        
        if (anyCriticallyFull)
        {
            _logger.LogWarning("One or more storage drives are critically full. Skipping download job processing.");
            return;
        }

        // Get candidate jobs (fetch extra to account for filtered cameras)
        var candidateJobs = await dbContext.FileDownloadJobs
            .Include(j => j.Camera)
            .Where(j => j.Status == "pending" || j.Status == "failed")
            .OrderBy(j => j.CreatedAt)
            .Take(500) // Fetch extra to account for filtered cameras
            .ToListAsync(stoppingToken);

        // Filter: one job per camera, excluding cameras already being processed
        var pendingJobs = candidateJobs
            .Where(j => !_activeCameras.ContainsKey(j.Camera.Id))
            .GroupBy(j => j.Camera.Id)
            .Select(g => g.First())
            .Take(_maxConcurrentDownloads)
            .ToList();

        if (!pendingJobs.Any())
        {
            return;
        }

        _logger.LogInformation("Found {PendingJobCount} pending download jobs for {CameraCount} cameras", 
            pendingJobs.Count, pendingJobs.Select(j => j.Camera.Id).Distinct().Count());

        // Process each job
        foreach (var job in pendingJobs)
        {
            // Skip if we're being cancelled
            if (stoppingToken.IsCancellationRequested)
                break;

            // Skip if the job is already being processed
            if (_activeDownloads.ContainsKey(job.Id))
                continue;

            try
            {
                // Wait for a download slot
                await _downloadSemaphore.WaitAsync(stoppingToken);

                // Start the download in a separate task
                var jobCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                _activeDownloads[job.Id] = jobCts;

                _ = Task.Run(async () =>
                {
                    // Claim this camera
                    if (!_activeCameras.TryAdd(job.Camera.Id, 0))
                    {
                        _downloadSemaphore.Release();
                        return;
                    }

                    try
                    {
                        await DownloadFileAsync(job.Id, jobCts.Token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error downloading file for job {JobId}", job.Id);
                    }
                    finally
                    {
                        _activeCameras.TryRemove(job.Camera.Id, out _);
                        _downloadSemaphore.Release();
                        _activeDownloads.TryRemove(job.Id, out _);
                    }
                }, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting download for job {JobId}", job.Id);
            }
        }
    }

    private async Task DownloadFileAsync(long jobId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HikvisionDbContext>();

        // Get the job with camera
        var job = await dbContext.FileDownloadJobs
            .Include(j => j.Camera)
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job == null)
        {
            _logger.LogWarning("Job {JobId} not found", jobId);
            return;
        }

        // Update job status to downloading
        job.Status = "downloading";
        job.StartTime = DateTime.UtcNow;
        job.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            // Login to camera
            var hikApi = HikApi.Login(
                job.Camera.IpAddress, 
                job.Camera.Port, 
                job.Camera.Username ?? "admin", 
                job.Camera.Password ?? ""
            );

            try
            {
                // Ensure download directory exists
                string downloadDir = Path.GetDirectoryName(job.DownloadPath) ?? "downloads";
                Directory.CreateDirectory(downloadDir);

                // Create a temporary file path for atomic download
                string tempFilePath = $"{job.DownloadPath}.tmp";
                string finalFilePath = job.DownloadPath;

                // Find the file to download
                var files = await hikApi.VideoService.FindFilesAsync(
                    job.FileStartTime.AddMinutes(-10000), // Add a small buffer to ensure
                    DateTime.UtcNow //job.FileEndTime.AddMinutes(10)
                );

                var fileToDownload = files.FirstOrDefault(f => f.Name == job.FileName);
                if (fileToDownload == null)
                {
                    throw new Exception($"File {job.FileName} not found on camera");
                }

                // Simulate download with progress reporting
                await SimulateDownloadAsync(hikApi, fileToDownload, tempFilePath, progress => UpdateDownloadProgress(job.Id, progress, dbContext));

                // Move the temp file to the final location (atomic operation)
                File.Move(tempFilePath, finalFilePath, true);

                // Update job status to completed
                job.Status = "completed";
                job.Progress = 100;
                job.EndTime = DateTime.UtcNow;
                job.UpdatedAt = DateTime.UtcNow;

                // Update camera's LastDownloadedAt
                var camera = job.Camera;
                camera.LastDownloadedAt = DateTime.UtcNow;
                camera.UpdatedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully downloaded file for job {JobId}: {FileName}", job.Id, job.FileName);
            }
            finally
            {
                // Always logout when done with the camera
                try
                {
                    hikApi.Logout();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during camera logout or SDK cleanup");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Download was cancelled
            job.Status = "pending"; // Reset to pending so it can be retried
            job.ErrorMessage = "Download was cancelled";
            job.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
            
            _logger.LogWarning("Download cancelled for job {JobId}: {FileName}", job.Id, job.FileName);
        }
        catch (Exception ex)
        {
            // Update job status to failed
            job.Status = "failed";
            job.ErrorMessage = ex.Message;
            job.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
            
            _logger.LogError(ex, "Failed to download file for job {JobId}: {FileName}", job.Id, job.FileName);
        }
    }

    private void UpdateDownloadProgress(long jobId, int progress, HikvisionDbContext dbContext)
    {
        try
        {
            var job = dbContext.FileDownloadJobs.Find(jobId);
            if (job != null)
            {
                job.Progress = progress;
                job.UpdatedAt = DateTime.UtcNow;
                dbContext.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating progress for job {JobId}", jobId);
        }
    }

    public async Task RetryJobAsync(long jobId)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HikvisionDbContext>();

        var job = await dbContext.FileDownloadJobs.FindAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Job {JobId} not found for retry", jobId);
            return;
        }

        if (job.Status == "downloading")
        {
            _logger.LogWarning("Job {JobId} is already downloading", jobId);
            return;
        }

        // Reset job status to pending
        job.Status = "pending";
        job.Progress = 0;
        job.ErrorMessage = null;
        job.UpdatedAt = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Job {JobId} has been reset for retry", jobId);
    }

    public async Task CancelJobAsync(long jobId)
    {
        // Cancel the download if it's active
        if (_activeDownloads.TryGetValue(jobId, out var cts))
        {
            cts.Cancel();
            _logger.LogInformation("Cancelled active download for job {JobId}", jobId);
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HikvisionDbContext>();

        var job = await dbContext.FileDownloadJobs.FindAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Job {JobId} not found for cancellation", jobId);
            return;
        }

        // Update job status to cancelled
        job.Status = "cancelled";
        job.UpdatedAt = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Job {JobId} has been cancelled", jobId);
    }
    
    private async Task SimulateDownloadAsync(Hik.Api.Abstraction.IHikApi hikApi, HikRemoteFile file, string outputPath, Action<int> progressCallback)
    {
        var destinationPath = outputPath;
        var downloadId = hikApi.VideoService.StartDownloadFile(file.Name, destinationPath);
        do
        {
            await Task.Delay(5000);
            int downloadProgress = hikApi.VideoService.GetDownloadPosition(downloadId);
            progressCallback(downloadProgress);
            if (downloadProgress == 100)
            {
                hikApi.VideoService.StopDownloadFile(downloadId);
                break;
            }
            else if (downloadProgress < 0 || downloadProgress > 100)
            {
                throw new InvalidOperationException($"UpdateDownloadProgress failed, progress value = {downloadProgress}");
            }
        }
        while (true);
    }
}
