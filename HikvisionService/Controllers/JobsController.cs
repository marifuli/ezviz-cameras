using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HikvisionService.Services;

namespace HikvisionService.Controllers;

[Authorize]
public class JobsController : Controller
{
    private readonly IHikvisionService _hikvisionService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IHikvisionService hikvisionService, ILogger<JobsController> logger)
    {
        _hikvisionService = hikvisionService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }
}