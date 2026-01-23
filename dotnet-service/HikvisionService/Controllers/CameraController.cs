using Microsoft.AspNetCore.Mvc;
using HikvisionService.Services;

namespace HikvisionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CameraController : ControllerBase
{
    private readonly IHikvisionService _hikvisionService;
    private readonly ILogger<CameraController> _logger;

    public CameraController(IHikvisionService hikvisionService, ILogger<CameraController> logger)
    {
        _hikvisionService = hikvisionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all cameras
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCameras()
    {
        try
        {
            var cameras = await _hikvisionService.GetAllCamerasAsync();
            return Ok(cameras);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cameras");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get camera by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCamera(long id)
    {
        try
        {
            var camera = await _hikvisionService.GetCameraByIdAsync(id);
            if (camera == null)
            {
                return NotFound($"Camera with ID {id} not found");
            }
            return Ok(camera);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting camera {CameraId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get available files from a camera
    /// </summary>
    [HttpGet("{id}/files")]
    public async Task<IActionResult> GetAvailableFiles(
        long id,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        [FromQuery] string fileType = "both")
    {
        try
        {
            // Set default time range if not provided (last 4 hours)
            var start = startTime ?? DateTime.Now.AddHours(-4);
            var end = endTime ?? DateTime.Now;

            // Validate file type
            if (fileType != "both" && fileType != "video" && fileType != "photo")
            {
                return BadRequest("FileType must be 'both', 'video', or 'photo'");
            }

            var files = await _hikvisionService.GetAvailableFilesAsync(id, start, end, fileType);
            
            // Transform files to a more API-friendly format
            var response = new
            {
                CameraId = id,
                StartTime = start,
                EndTime = end,
                FileType = fileType,
                TotalFiles = files.Count,
                Files = files.Select(f => new
                {
                    Name = f.Name,
                    Size = f.Size,
                    Date = f.Date,
                    Duration = f.Duration, // Duration in seconds for videos
                    Type = f.Duration > 0 ? "video" : "photo" // Videos have duration, photos don't
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available files for camera {CameraId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Test camera connection
    /// </summary>
    [HttpPost("{id}/test-connection")]
    public async Task<IActionResult> TestConnection(long id)
    {
        try
        {
            var isConnected = await _hikvisionService.TestCameraConnectionAsync(id);
            return Ok(new { CameraId = id, IsConnected = isConnected });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection for camera {CameraId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}