using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HikvisionService.Data;
using HikvisionService.Models;
using HikvisionService.Models.ViewModels;

namespace HikvisionService.Controllers;

[Authorize]
public class JobsController : Controller
{
    private readonly HikvisionDbContext _context;
    private readonly ILogger<JobsController> _logger;

    public JobsController(HikvisionDbContext context, ILogger<JobsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int page = 1, long? storeId = null, long? cameraId = null, string? status = null)
    {
        const int pageSize = 25;
        
        var query = _context.FileDownloadJobs
            .Include(j => j.Camera)
                .ThenInclude(c => c.Store)
            .AsQueryable();

        // Apply filters
        if (storeId.HasValue)
        {
            query = query.Where(j => j.Camera.StoreId == storeId);
        }

        if (cameraId.HasValue)
        {
            query = query.Where(j => j.CameraId == cameraId);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(j => j.Status == status);
        }

        // Order by UpdatedAt descending
        query = query.OrderByDescending(j => j.UpdatedAt);

        var totalJobs = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalJobs / (double)pageSize);

        var jobs = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Get dropdown options
        var stores = await _context.Stores
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name
            })
            .ToListAsync();

        var No_Store = "No Store";
        var cameras = await _context.Cameras
            .Include(c => c.Store)
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.Name} ({(c.Store != null ? c.Store.Name : No_Store)})"
            })
            .ToListAsync();

        var statusOptions = new List<SelectListItem>
        {
            new() { Value = "", Text = "All Statuses" },
            new() { Value = "pending", Text = "Pending" },
            new() { Value = "downloading", Text = "Downloading" },
            new() { Value = "completed", Text = "Completed" },
            new() { Value = "failed", Text = "Failed" }
        };

        var viewModel = new JobsIndexViewModel
        {
            Jobs = jobs,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalJobs = totalJobs,
            PageSize = pageSize,
            StoreId = storeId,
            CameraId = cameraId,
            Status = status,
            Stores = new List<SelectListItem> { new() { Value = "", Text = "All Stores" } }.Concat(stores),
            Cameras = new List<SelectListItem> { new() { Value = "", Text = "All Cameras" } }.Concat(cameras),
            StatusOptions = statusOptions
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetCamerasByStore(long? storeId)
    {
        var query = _context.Cameras.AsQueryable();
        
        if (storeId.HasValue)
        {
            query = query.Where(c => c.StoreId == storeId);
        }

        var cameras = await query
            .OrderBy(c => c.Name)
            .Select(c => new { id = c.Id, name = c.Name })
            .ToListAsync();

        return Json(cameras);
    }
}