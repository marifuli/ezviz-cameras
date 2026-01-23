using Microsoft.EntityFrameworkCore;
using HikvisionService.Data;
using HikvisionService.Models;
using Hik.Api;
using Hik.Api.Data;

namespace HikvisionService.Services;

public class HikvisionService : IHikvisionService
{
    private readonly HikvisionDbContext _context;
    private readonly ILogger<HikvisionService> _logger;

    public HikvisionService(HikvisionDbContext context, ILogger<HikvisionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<HikRemoteFile>> GetAvailableFilesAsync(long cameraId, DateTime startTime, DateTime endTime, string fileType = "both")
    {
        var camera = await GetCameraByIdAsync(cameraId);
        if (camera == null)
        {
            _logger.LogWarning("Camera with ID {CameraId} not found", cameraId);
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
                logDirectory: "HikvisionSDKLogs", 
                autoDeleteLogs: true,
                waitTimeMilliseconds: 5000, // Increase timeout for better reliability
                forceReinitialization: true // Force reinitialization to ensure clean state
            );

            // Login to the camera
            var hikApi = HikApi.Login(camera.IpAddress, camera.Port, camera.Username ?? "admin", camera.Password ?? "");
            _logger.LogInformation("Successfully connected to camera {CameraName} at {IpAddress}:{Port}", camera.Name, camera.IpAddress, camera.Port);

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
                            _logger.LogInformation("Found {VideoCount} videos for channel {ChannelNumber}", videos.Count, channel.ChannelNumber);
                        }

                        if (fileType is "both" or "photo")
                        {
                            // Note: Photos might not be available per channel basis on NVR
                            try 
                            {
                                var photos = await hikApi.PhotoService.FindFilesAsync(startTime, endTime);
                                files.AddRange(photos);
                                _logger.LogInformation("Found {PhotoCount} photos for channel {ChannelNumber}", photos.Count, channel.ChannelNumber);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to get photos from channel {ChannelNumber}", channel.ChannelNumber);
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
                        _logger.LogInformation("Found {VideoCount} videos from camera", videos.Count);
                    }

                    if (fileType is "both" or "photo")
                    {
                        var photos = await hikApi.PhotoService.FindFilesAsync(startTime, endTime);
                        files.AddRange(photos);
                        _logger.LogInformation("Found {PhotoCount} photos from camera", photos.Count);
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
            _logger.LogError(ex, "Failed to get files from camera {CameraId}: {ErrorMessage}", cameraId, ex.Message);
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
                _logger.LogWarning(cleanupEx, "Failed to clean up Hikvision SDK resources");
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
                logDirectory: "HikvisionSDKLogs", 
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
}