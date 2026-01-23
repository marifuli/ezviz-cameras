using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HikvisionService.Models;
using HikvisionService.Models.ViewModels;
using HikvisionService.Services;

namespace HikvisionService.Controllers;

[Authorize]
public class FootageController : Controller
{
    private readonly IHikvisionService _hikvisionService;
    private readonly ILogger<FootageController> _logger;
    private readonly IWebHostEnvironment _env;

    public FootageController(IHikvisionService hikvisionService, ILogger<FootageController> logger, IWebHostEnvironment env)
    {
        _hikvisionService = hikvisionService;
        _logger = logger;
        _env = env;
    }

    public async Task<IActionResult> Index(FootageViewModel model)
    {
        try
        {
            // Set default dates if not provided
            if (model.StartDate == default)
            {
                model.StartDate = DateTime.Today.AddDays(-7);
            }
            
            if (model.EndDate == default)
            {
                model.EndDate = DateTime.Today;
            }
            
            // Get all cameras for the dropdown
            model.Cameras = await _hikvisionService.GetAllCamerasAsync();
            
            // Get footage files based on filters
            model.Files = await _hikvisionService.GetFootageFilesAsync(
                model.SelectedCameraId,
                model.StartDate,
                model.EndDate,
                model.FileType
            );
            
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading footage data");
            return View(new FootageViewModel());
        }
    }
    
    [HttpGet]
    public async Task<IActionResult> Download(string path)
    {
        try
        {
            // Validate and get the file path
            var filePath = await _hikvisionService.GetFootageDownloadUrlAsync(path);
            var absolutePath = Path.Combine(
                _env.ContentRootPath,
                filePath.Replace("/", Path.DirectorySeparatorChar.ToString())
            );

            // Get file info
            var fileInfo = new System.IO.FileInfo(absolutePath);
            if (!fileInfo.Exists)
            {
                return NotFound(absolutePath);
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
            return PhysicalFile(absolutePath, contentType, path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {Path}", path);
            return BadRequest("Error downloading file");
        }
    }
}