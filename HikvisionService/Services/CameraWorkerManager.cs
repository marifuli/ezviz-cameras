using System.IO;
using System.Collections.Concurrent;
using HikvisionService.Data;
using HikvisionService.Models;
using Microsoft.EntityFrameworkCore;
using Hik.Api;
using Hik.Api.Data;
using HikvisionService.Models.ViewModels;

namespace HikvisionService.Services;

public class CameraWorkerManager : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CameraWorkerManager> _logger;
    private readonly Dictionary<long, CameraWorker> _workers = new();
    private readonly SemaphoreSlim _workersLock = new(1, 1); // For thread safety
    private readonly TimeSpan _refreshInterval;

    public CameraWorkerManager(
        IServiceProvider services,
        ILogger<CameraWorkerManager> logger,
        IConfiguration configuration)
    {
        _services = services;
        _logger = logger;
        _refreshInterval = TimeSpan.FromMinutes(5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Camera Worker Manager started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshWorkersAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown, break out of loop
                    _logger.LogInformation("Camera Worker Manager is shutting down...");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing camera workers");
                }

                // Wait for next refresh or cancellation
                try
                {
                    await Task.Delay(_refreshInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                    _logger.LogInformation("Camera Worker Manager delay cancelled, shutting down...");
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
            _logger.LogInformation("Camera Worker Manager operation cancelled");
        }
        finally
        {
            // Ensure all workers are stopped when manager stops
            await StopAllWorkersAsync();
        }

        _logger.LogInformation("Camera Worker Manager stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Camera Worker Manager stopping...");
        
        // Stop the background execution first
        await base.StopAsync(cancellationToken);
        
        // Then stop all workers
        await StopAllWorkersAsync();
    }

    private async Task StopAllWorkersAsync()
    {
        await _workersLock.WaitAsync();
        try
        {
            var stopTasks = _workers.Values.Select(worker => 
                SafeStopWorkerAsync(worker));
            
            await Task.WhenAll(stopTasks);
            _workers.Clear();
            
            _logger.LogInformation("All camera workers stopped");
        }
        finally
        {
            _workersLock.Release();
        }
    }

    private async Task SafeStopWorkerAsync(CameraWorker worker)
    {
        try
        {
            // Give each worker 5 seconds to stop gracefully
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await worker.StopAsync(cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping camera worker");
        }
    }

    private async Task RefreshWorkersAsync(CancellationToken stoppingToken)
    {
        // Check if we're already cancelled
        stoppingToken.ThrowIfCancellationRequested();
        
        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HikvisionDbContext>();

        // Get all active cameras
        var cameras = await dbContext.Cameras
            .ToListAsync(stoppingToken);

        var cameraIds = cameras.Select(c => c.Id).ToHashSet();

        await _workersLock.WaitAsync(stoppingToken);
        try
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            // Stop workers for cameras that no longer exist
            var workersToStop = _workers.Keys.Where(id => !cameraIds.Contains(id)).ToList();
            foreach (var cameraId in workersToStop)
            {
                stoppingToken.ThrowIfCancellationRequested();
                
                if (_workers.TryGetValue(cameraId, out var worker))
                {
                    _logger.LogInformation("Stopping worker for camera {CameraId}", cameraId);
                    await worker.StopAsync(stoppingToken);
                    _workers.Remove(cameraId);
                }
            }

            // Start workers for new cameras
            foreach (var camera in cameras)
            {
                stoppingToken.ThrowIfCancellationRequested();
                
                if (!_workers.ContainsKey(camera.Id))
                {
                    _logger.LogInformation("Starting worker for camera {CameraId}: {CameraName}", 
                        camera.Id, camera.Name);
                    
                    var worker = ActivatorUtilities.CreateInstance<CameraWorker>(
                        _services, camera.Id);
                    
                    _workers[camera.Id] = worker;
                    
                    // Important: Start the worker but don't await it
                    _ = worker.StartAsync(stoppingToken).ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            _logger.LogError(task.Exception, 
                                "Camera worker for {CameraId} failed to start", camera.Id);
                        }
                    }, stoppingToken);
                    
                    try
                    {
                        await Task.Delay(1000, stoppingToken); // Stagger starts
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Worker start staggering cancelled");
                        break;
                    }
                }
            }
        }
        finally
        {
            _workersLock.Release();
        }
    }

    // In CameraWorkerManager.cs
    public async Task<List<WorkerStatusViewModel>> GetWorkerStatus()
    {
        var statusList = new List<WorkerStatusViewModel>();
        
        await _workersLock.WaitAsync();
        try
        {
            foreach (var kvp in _workers)
            {
                var worker = kvp.Value;
                var stats = worker.GetWorkerStats();
                string cameraName = await GetCameraName(worker.CameraId);
                
                statusList.Add(new WorkerStatusViewModel
                {
                    CameraId = worker.CameraId,
                    CameraName = cameraName,
                    IsRunning = true,
                    ActiveDownloadCount = stats.ActiveDownloadCount,
                    LastCheckTime = stats.LastCheckTime,
                    LastError = stats.LastError
                });
            }
        }
        finally
        {
            _workersLock.Release();
        }
        
        return statusList;
    }

    // Helper method to get camera name
    private async Task<string> GetCameraName(long cameraId)
    {
        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HikvisionDbContext>();
        var camera = await dbContext.Cameras.FindAsync(cameraId);
        return camera?.Name ?? $"Camera {cameraId}";
    }
    // NEW METHOD: Get active worker count
    public Task<int> GetActiveWorkerCount()
    {
        return Task.FromResult(_workers.Count);
    }
}