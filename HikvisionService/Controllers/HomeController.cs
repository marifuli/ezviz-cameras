using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HikvisionService.Data;
using HikvisionService.Models;

namespace HikvisionService.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly HikvisionDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(HikvisionDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(long[]? stores = null)
    {
        try
        {
            var allStores = await _context.Stores.OrderBy(s => s.Name).ToListAsync();

            var selectedStoreIds = stores?.ToList() ?? allStores.Select(s => s.Id).ToList();
            
            var cameras = await _context.Cameras
                .Include(c => c.Store)
                .Where(c => selectedStoreIds.Contains(c.StoreId ?? 0))
                .OrderBy(c => c.Name)
                .ToListAsync();

            var viewModel = new DashboardViewModel
            {
                Stores = allStores,
                SelectedStoreIds = selectedStoreIds,
                Cameras = cameras
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
            return View("Error");
        }
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
    }
}