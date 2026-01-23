using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HikvisionService.Models.ViewModels;
using System.Text.RegularExpressions;

namespace HikvisionService.Controllers;

[Authorize]
public class LogsController : Controller
{
    private readonly ILogger<LogsController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public LogsController(
        ILogger<LogsController> logger,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
    }

    public IActionResult Index(string logLevel = "All")
    {
        try
        {
            var model = new LogViewModel
            {
                LogLevel = logLevel,
                LastUpdated = DateTime.UtcNow
            };

            // Get log directory from configuration or use default
            string logDirectory = _configuration["Logging:FileLogging:LogDirectory"] ?? "logs";
            
            // Make sure the path is relative to the content root
            if (!Path.IsPathRooted(logDirectory))
            {
                logDirectory = Path.Combine(_environment.ContentRootPath, logDirectory);
            }
            
            // Create the log directory if it doesn't exist
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
                model.LogLines.Add($"Created log directory: {logDirectory}");
                return View(model);
            }
            
            // Find the most recent log file
            var logFiles = Directory.GetFiles(logDirectory, "*.log")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .ToList();
                
            if (!logFiles.Any())
            {
                model.LogLines.Add("No log files found in directory: " + logDirectory);
                return View(model);
            }
            
            string logFilePath = logFiles.First();
            model.LogFilePath = logFilePath;
            
            // Read the log file
            var logLines = ReadLastLines(logFilePath, model.MaxLines);
            
            // Filter by log level if specified
            if (logLevel != "All")
            {
                logLines = logLines.Where(line => line.Contains($"[{logLevel}]", StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            model.LogLines = logLines;
            
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading log files");
            var model = new LogViewModel();
            model.LogLines.Add($"Error reading log files: {ex.Message}");
            return View(model);
        }
    }
    
    [HttpGet]
    public IActionResult Refresh(string logLevel = "All")
    {
        try
        {
            var model = new LogViewModel
            {
                LogLevel = logLevel,
                LastUpdated = DateTime.UtcNow
            };

            // Get log directory from configuration or use default
            string logDirectory = _configuration["Logging:FileLogging:LogDirectory"] ?? "logs";
            
            // Make sure the path is relative to the content root
            if (!Path.IsPathRooted(logDirectory))
            {
                logDirectory = Path.Combine(_environment.ContentRootPath, logDirectory);
            }
            
            // Create the log directory if it doesn't exist
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
                return Json(new { success = false, message = $"Created log directory: {logDirectory}" });
            }
            
            // Find the most recent log file
            var logFiles = Directory.GetFiles(logDirectory, "*.log")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .ToList();
                
            if (!logFiles.Any())
            {
                return Json(new { success = false, message = "No log files found in directory: " + logDirectory });
            }
            
            string logFilePath = logFiles.First();
            model.LogFilePath = logFilePath;
            
            // Read the log file
            var logLines = ReadLastLines(logFilePath, model.MaxLines);
            
            // Filter by log level if specified
            if (logLevel != "All")
            {
                logLines = logLines.Where(line => line.Contains($"[{logLevel}]", StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            model.LogLines = logLines;
            
            return Json(new { 
                success = true, 
                logLines = model.LogLines,
                lastUpdated = model.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing log data");
            return Json(new { success = false, message = ex.Message });
        }
    }
    
    private List<string> ReadLastLines(string filePath, int maxLines)
    {
        var result = new List<string>();
        
        try
        {
            // Use a queue to keep track of the last N lines
            var queue = new Queue<string>(maxLines);
            
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (queue.Count >= maxLines)
                    {
                        queue.Dequeue(); // Remove the oldest line
                    }
                    queue.Enqueue(line); // Add the new line
                }
            }
            
            // Convert queue to list
            result = queue.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading log file: {FilePath}", filePath);
            result.Add($"Error reading log file: {ex.Message}");
        }
        
        return result;
    }
}