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
            // Initialize the Hikvision SDK
            HikApi.Initialize();

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
                hikApi.Logout();
                HikApi.Cleanup();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get files from camera {CameraId}: {ErrorMessage}", cameraId, ex.Message);
        }

        return files;
    }

    public async Task<bool> TestCameraConnectionAsync(long cameraId)
    {
        var camera = await GetCameraByIdAsync(cameraId);
        if (camera == null)
        {
            return false;
        }

        try
        {
            HikApi.Initialize();
            var hikApi = HikApi.Login(camera.IpAddress, camera.Port, camera.Username ?? "admin", camera.Password ?? "");
            
            // Try to get camera time to test connection
            // var cameraTime = hikApi.ConfigService.GetTime();
            _logger.LogInformation("Camera {CameraName} connection test successful. Camera time: {Password}", camera.Name, camera.Password);
            foreach (var channel in hikApi.IpChannels)
            {
                Console.WriteLine($"{channel.Name} {channel.ChannelNumber}; IsOnline : {channel.IsOnline};");
            }
            
            hikApi.Logout();
            HikApi.Cleanup();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Camera {CameraId} connection test failed: {ErrorMessage}", cameraId, ex.Message);
            return false;
        }
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