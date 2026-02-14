using Microsoft.AspNetCore.Mvc;
using HikvisionService.Models;
using HikvisionService.Services;
using HikvisionService.Models.ViewModels;
using HikvisionService.Data;

namespace HikvisionService.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private readonly IHikvisionService _hikvisionService;
    private readonly ILogger<ApiController> _logger;

    public ApiController(IHikvisionService hikvisionService, ILogger<ApiController> logger)
    {
        _hikvisionService = hikvisionService;
        _logger = logger;
    }

    // Dashboard data
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardViewModel>> GetDashboardData()
    {
        try
        {
            var dashboardData = await _hikvisionService.GetDashboardDataAsync();
            return Ok(dashboardData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard data");
            return StatusCode(500, new { error = "Failed to get dashboard data" });
        }
    }

    // Camera endpoints
    [HttpGet("cameras")]
    public async Task<ActionResult<List<Camera>>> GetAllCameras()
    {
        try
        {
            var cameras = await _hikvisionService.GetAllCamerasAsync();
            return Ok(cameras);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all cameras");
            return StatusCode(500, new { error = "Failed to get cameras" });
        }
    }

    [HttpGet("cameras/{id}")]
    public async Task<ActionResult<Camera>> GetCamera(long id)
    {
        try
        {
            var camera = await _hikvisionService.GetCameraByIdAsync(id);
            if (camera == null)
            {
                return NotFound(new { error = $"Camera with ID {id} not found" });
            }
            return Ok(camera);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting camera {CameraId}", id);
            return StatusCode(500, new { error = $"Failed to get camera {id}" });
        }
    }

    [HttpPost("cameras/{id}/check")]
    public async Task<ActionResult> CheckCameraConnection(long id)
    {
        try
        {
            bool isOnline = await _hikvisionService.CheckCameraConnectionAsync(id);
            return Ok(new { isOnline });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking camera {CameraId} connection", id);
            return StatusCode(500, new { error = $"Failed to check camera {id} connection" });
        }
    }

    [HttpPost("cameras/check-all")]
    public async Task<ActionResult> CheckAllCameras()
    {
        try
        {
            await _hikvisionService.TriggerCameraHealthCheckAsync();
            return Ok(new { message = "Camera health check triggered" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering camera health check");
            return StatusCode(500, new { error = "Failed to trigger camera health check" });
        }
    }

    // Storage drive endpoints
    [HttpGet("storage-drives")]
    public async Task<ActionResult<List<StorageDrive>>> GetAllStorageDrives()
    {
        try
        {
            var drives = await _hikvisionService.GetAllStorageDrivesAsync();
            return Ok(drives);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all storage drives");
            return StatusCode(500, new { error = "Failed to get storage drives" });
        }
    }

    [HttpGet("storage-drives/{id}")]
    public async Task<ActionResult<StorageDrive>> GetStorageDrive(long id)
    {
        try
        {
            var drive = await _hikvisionService.GetStorageDriveByIdAsync(id);
            if (drive == null)
            {
                return NotFound(new { error = $"Storage drive with ID {id} not found" });
            }
            return Ok(drive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage drive {DriveId}", id);
            return StatusCode(500, new { error = $"Failed to get storage drive {id}" });
        }
    }

    [HttpPost("storage-drives")]
    public async Task<ActionResult<StorageDrive>> AddStorageDrive(StorageDrive drive)
    {
        try
        {
            var newDrive = await _hikvisionService.AddStorageDriveAsync(drive);
            return CreatedAtAction(nameof(GetStorageDrive), new { id = newDrive.Id }, newDrive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding storage drive");
            return StatusCode(500, new { error = "Failed to add storage drive" });
        }
    }

    [HttpPut("storage-drives/{id}")]
    public async Task<ActionResult> UpdateStorageDrive(long id, StorageDrive drive)
    {
        if (id != drive.Id)
        {
            return BadRequest(new { error = "ID mismatch" });
        }

        try
        {
            bool success = await _hikvisionService.UpdateStorageDriveAsync(drive);
            if (!success)
            {
                return NotFound(new { error = $"Storage drive with ID {id} not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating storage drive {DriveId}", id);
            return StatusCode(500, new { error = $"Failed to update storage drive {id}" });
        }
    }

    [HttpDelete("storage-drives/{id}")]
    public async Task<ActionResult> DeleteStorageDrive(long id)
    {
        try
        {
            bool success = await _hikvisionService.DeleteStorageDriveAsync(id);
            if (!success)
            {
                return NotFound(new { error = $"Storage drive with ID {id} not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting storage drive {DriveId}", id);
            return StatusCode(500, new { error = $"Failed to delete storage drive {id}" });
        }
    }

    [HttpPost("storage-drives/check")]
    public async Task<ActionResult> CheckStorageDrives()
    {
        try
        {
            await _hikvisionService.TriggerStorageCheckAsync();
            return Ok(new { message = "Storage check triggered" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering storage check");
            return StatusCode(500, new { error = "Failed to trigger storage check" });
        }
    }

    // Download job endpoints
    [HttpGet("download-jobs")]
    public async Task<ActionResult<List<FileDownloadJobDto>>> GetAllDownloadJobs()
    {
        try
        {
            var jobs = await _hikvisionService.GetAllDownloadJobsAsync();
            return Ok(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all download jobs");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("download-jobs/active")]
    public async Task<ActionResult<List<FileDownloadJobDto>>> GetActiveDownloadJobs()
    {
        try
        {
            var jobs = await _hikvisionService.GetActiveDownloadJobsAsync();
            return Ok(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active download jobs");
            return StatusCode(500, new { error = "Failed to get active download jobs" });
        }
    }

    [HttpGet("download-jobs/failed")]
    public async Task<ActionResult<List<FileDownloadJobDto>>> GetFailedDownloadJobs()
    {
        try
        {
            var jobs = await _hikvisionService.GetFailedDownloadJobsAsync();
            return Ok(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting failed download jobs");
            return StatusCode(500, new { error = "Failed to get failed download jobs" });
        }
    }

    [HttpGet("download-jobs/{id}")]
    public async Task<ActionResult<FileDownloadJob>> GetDownloadJob(long id)
    {
        try
        {
            var job = await _hikvisionService.GetDownloadJobByIdAsync(id);
            if (job == null)
            {
                return NotFound(new { error = $"Download job with ID {id} not found" });
            }
            return Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting download job {JobId}", id);
            return StatusCode(500, new { error = $"Failed to get download job {id}" });
        }
    }

    [HttpPost("download-jobs/{id}/retry")]
    public async Task<ActionResult> RetryDownloadJob(long id)
    {
        try
        {
            bool success = await _hikvisionService.RetryDownloadJobAsync(id);
            if (!success)
            {
                return NotFound(new { error = $"Download job with ID {id} not found" });
            }
            return Ok(new { message = $"Download job {id} retry triggered" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying download job {JobId}", id);
            return StatusCode(500, new { error = $"Failed to retry download job {id}" });
        }
    }

    [HttpPost("download-jobs/{id}/cancel")]
    public async Task<ActionResult> CancelDownloadJob(long id)
    {
        try
        {
            bool success = await _hikvisionService.CancelDownloadJobAsync(id);
            if (!success)
            {
                return NotFound(new { error = $"Download job with ID {id} not found" });
            }
            return Ok(new { message = $"Download job {id} cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling download job {JobId}", id);
            return StatusCode(500, new { error = $"Failed to cancel download job {id}" });
        }
    }
    
    // Footage endpoints
    [HttpGet("footage")]
    public async Task<ActionResult<List<FootageFileViewModel>>> GetFootageFiles(
        [FromQuery] long? cameraId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string fileType = "both")
    {
        try
        {
            var files = await _hikvisionService.GetFootageFilesAsync(cameraId, startDate, endDate, fileType);
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting footage files");
            return StatusCode(500, new { error = "Failed to get footage files" });
        }
    }
    
    [HttpGet("footage/download")]
    public async Task<ActionResult> DownloadFootage([FromQuery] string path)
    {
        try
        {
            var filePath = await _hikvisionService.GetFootageDownloadUrlAsync(path);
            
            // Get file info
            var fileInfo = new System.IO.FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                return NotFound(new { error = "File not found" });
            }
            
            // Determine content type
            string contentType = "application/octet-stream";
            if (filePath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                contentType = "video/mp4";
            }
            else if (filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                     filePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                contentType = "image/jpeg";
            }
            
            // Return the file
            return PhysicalFile(filePath, contentType, fileInfo.Name);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = "File not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {Path}", path);
            return StatusCode(500, new { error = "Failed to download file" });
        }
    }
}