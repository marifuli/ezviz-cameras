using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HikvisionService.Data;
using HikvisionService.Models;
using Hik.Api.Data;
using HikvisionService.Services;

namespace HikvisionService.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly HikvisionDbContext _context;
    private readonly ILogger<HomeController> _logger;
    private readonly IHikvisionService _hikvisionService;

    public HomeController(HikvisionDbContext context, ILogger<HomeController> logger, IHikvisionService hikvisionService)   
    {
        _context = context;
        _logger = logger;
        _hikvisionService = hikvisionService;
    }

    public async Task<IActionResult> Index(long[]? stores = null)
    {
        var allStores = await _context.Stores.OrderBy(s => s.Name).ToListAsync();
        var selectedStoreIds = stores?.ToList() ?? allStores.Select(s => s.Id).ToList();
        var cameras = await _context.Cameras
            .Include(c => c.Store)
            // .Where(c => selectedStoreIds.Contains(c.StoreId ?? 0))
            .OrderBy(c => c.Name)
            .ToListAsync();

        var recordings = new List<HikRemoteFile>();

        // foreach (var camera in cameras)
        // {
        //     var recording = await _hikvisionService.GetAvailableFilesAsync(
        //         camera.Id, DateTime.Now.AddDays(-1), 
        //         DateTime.Now
        //     );
        //     recordings.AddRange(recording);
        // }

        var viewModel = new DashboardViewModel
        {
            Stores = allStores,
            SelectedStoreIds = selectedStoreIds,
            Cameras = cameras,
            Recordings = recordings
        };

        return View(viewModel);
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult Error()
    {
        return View();
    }

    public class DashboardViewModel
    {
        public List<Store> Stores { get; set; } = new();
        public List<long> SelectedStoreIds { get; set; } = new();
        public List<Camera> Cameras { get; set; } = new();
        public List<HikRemoteFile> Recordings { get; set; } = new();
    }
}