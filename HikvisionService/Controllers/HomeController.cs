using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HikvisionService.Data;
using HikvisionService.Models;
using HikvisionService.Models.ViewModels;
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

    public async Task<IActionResult> Index()
    {
        try
        {
            var dashboardData = await _hikvisionService.GetDashboardDataAsync();
            return View(dashboardData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard data");
            return View(new Models.ViewModels.DashboardViewModel());
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
}