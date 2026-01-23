using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HikvisionService.Services;
using HikvisionService.Data;
using HikvisionService.Models;

namespace HikvisionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CameraController : ControllerBase
{
    private readonly IHikvisionService _hikvisionService;
    private readonly ILogger<CameraController> _logger;
    private readonly HikvisionDbContext _context;

    public CameraController(IHikvisionService hikvisionService, ILogger<CameraController> logger, HikvisionDbContext context)
    {
        _hikvisionService = hikvisionService;
        _logger = logger;
        _context = context;
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

    /// <summary>
    /// Create a new camera
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCamera([FromBody] CreateCameraRequest request)
    {
        try
        {
            // Validate that the store exists
            var storeExists = await _context.Stores.AnyAsync(s => s.Id == request.StoreId);
            if (!storeExists)
            {
                return BadRequest($"Store with ID {request.StoreId} does not exist");
            }

            var camera = new Camera
            {
                StoreId = request.StoreId,
                Name = request.Name,
                IpAddress = request.IpAddress,
                Port = request.Port,
                Username = request.Username,
                Password = request.Password,
                ServerPort = request.ServerPort,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Cameras.Add(camera);
            await _context.SaveChangesAsync();

            // Load the store for the response
            await _context.Entry(camera)
                .Reference(c => c.Store)
                .LoadAsync();

            _logger.LogInformation("Created camera: {CameraName} with ID: {CameraId}", camera.Name, camera.Id);

            return CreatedAtAction(nameof(GetCamera), new { id = camera.Id }, camera);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating camera");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update existing camera
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCamera(long id, [FromBody] UpdateCameraRequest request)
    {
        try
        {
            var camera = await _context.Cameras.FindAsync(id);
            if (camera == null)
            {
                return NotFound($"Camera with ID {id} not found");
            }

            // Validate that the store exists
            var storeExists = await _context.Stores.AnyAsync(s => s.Id == request.StoreId);
            if (!storeExists)
            {
                return BadRequest($"Store with ID {request.StoreId} does not exist");
            }

            camera.StoreId = request.StoreId;
            camera.Name = request.Name;
            camera.IpAddress = request.IpAddress;
            camera.Port = request.Port;
            camera.Username = request.Username;
            camera.Password = request.Password;
            camera.ServerPort = request.ServerPort;
            camera.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Load the store for the response
            await _context.Entry(camera)
                .Reference(c => c.Store)
                .LoadAsync();

            _logger.LogInformation("Updated camera {CameraId}: {CameraName}", camera.Id, camera.Name);

            return Ok(camera);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating camera {CameraId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete camera
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCamera(long id)
    {
        try
        {
            var camera = await _context.Cameras
                .Include(c => c.FileDownloadJobs)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (camera == null)
            {
                return NotFound($"Camera with ID {id} not found");
            }

            // Delete associated download jobs
            if (camera.FileDownloadJobs.Any())
            {
                _context.FileDownloadJobs.RemoveRange(camera.FileDownloadJobs);
            }

            _context.Cameras.Remove(camera);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted camera {CameraId}: {CameraName}", camera.Id, camera.Name);

            return Ok(new { message = "Camera deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting camera {CameraId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    // Request DTOs
    public class CreateCameraRequest
    {
        public long StoreId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 554;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int? ServerPort { get; set; }
    }

    public class UpdateCameraRequest
    {
        public long StoreId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 554;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int? ServerPort { get; set; }
    }
}