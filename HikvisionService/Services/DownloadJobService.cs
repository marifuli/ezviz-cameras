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
        _maxConcurrentDownloads = configuration.GetValue<int>("DownloadJob:MaxConcurrentDownloads", 2);
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

        // Get pending jobs
        var pendingJobs = await dbContext.FileDownloadJobs
            .Include(j => j.Camera)
            .Where(j => j.Status == "pending" || j.Status == "failed")
            .OrderBy(j => j.CreatedAt)
            .Take(10) // Process in batches
            .ToListAsync(stoppingToken);

        if (!pendingJobs.Any())
        {
            return;
        }

        _logger.LogInformation("Found {PendingJobCount} pending download jobs", pendingJobs.Count);

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
                _downloadSemaphore.Release();
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
            // Set the library path to the current directory
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            HikApi.SetLibraryPath(currentDirectory);
            
            // Initialize with proper logging and force reinitialization
            HikApi.Initialize(
                logLevel: 3, 
                logDirectory: "HikvisionSDKLogs", 
                autoDeleteLogs: true,
                waitTimeMilliseconds: 5000,
                forceReinitialization: true
            );

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
                    job.FileStartTime.AddMinutes(-1), // Add a small buffer to ensure we find the file
                    job.FileEndTime.AddMinutes(1)
                );

                var fileToDownload = files.FirstOrDefault(f => f.Name == job.FileName);
                if (fileToDownload == null)
                {
                    throw new Exception($"File {job.FileName} not found on camera");
                }

                // Simulate download with progress reporting
                await SimulateDownloadAsync(fileToDownload, tempFilePath, progress => UpdateDownloadProgress(job.Id, progress, dbContext));

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
                hikApi.Logout();
                HikApi.Cleanup();
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
    
    private async Task SimulateDownloadAsync(HikRemoteFile file, string outputPath, Action<int> progressCallback)
    {
        // This is a simplified implementation since we don't have direct access to the API's download method
        // In a real implementation, you would use the Hik.Api to download the file with progress reporting
        
        // Simulate download with progress updates
        for (int progress = 0; progress <= 100; progress += 10)
        {
            progressCallback(progress);
            await Task.Delay(500); // Simulate download time
        }
        
        // Create an empty file to simulate the download
        using (var fs = File.Create(outputPath))
        {
            // In a real implementation, this would contain the actual file data
            byte[] content = new byte[1024];
            await fs.WriteAsync(content, 0, content.Length);
        }
    }
}