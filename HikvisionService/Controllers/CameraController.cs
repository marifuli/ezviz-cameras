using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HikvisionService.Data;
using HikvisionService.Models;
using HikvisionService.Services;
using System.ComponentModel.DataAnnotations;

namespace HikvisionService.Controllers;

public class CameraController : Controller
{
    private readonly HikvisionDbContext _context;
    private readonly IHikvisionService _hikvisionService;
    private readonly ILogger<CameraController> _logger;

    public CameraController(HikvisionDbContext context, IHikvisionService hikvisionService, ILogger<CameraController> logger)
    {
        _context = context;
        _hikvisionService = hikvisionService;
        _logger = logger;
    }

    // GET: /Camera
    public async Task<IActionResult> Index()
    {
        try
        {
            var cameras = await _context.Cameras
                .Include(c => c.Store)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(cameras);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cameras");
            TempData["ErrorMessage"] = "An error occurred while retrieving cameras.";
            return View(new List<Camera>());
        }
    }

    // GET: /Camera/Details/5
    public async Task<IActionResult> Details(long id)
    {
        try
        {
            var camera = await _context.Cameras
                .Include(c => c.Store)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (camera == null)
            {
                TempData["ErrorMessage"] = $"Camera with ID {id} not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(camera);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting camera {CameraId}", id);
            TempData["ErrorMessage"] = "An error occurred while retrieving the camera.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: /Camera/Create
    public async Task<IActionResult> Create()
    {
        await PopulateStoresDropdown();
        return View();
    }

    // POST: /Camera/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CameraModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Validate that the store exists
                var storeExists = await _context.Stores.AnyAsync(s => s.Id == model.StoreId);
                if (!storeExists)
                {
                    ModelState.AddModelError("StoreId", "Selected store does not exist");
                    await PopulateStoresDropdown();
                    return View(model);
                }

                var camera = new Camera
                {
                    StoreId = model.StoreId,
                    Name = model.Name,
                    IpAddress = model.IpAddress,
                    Port = model.Port,
                    Username = model.Username,
                    Password = model.Password,
                    ServerPort = model.ServerPort,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Cameras.Add(camera);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created camera: {CameraName} with ID: {CameraId}", camera.Name, camera.Id);
                TempData["SuccessMessage"] = "Camera created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating camera");
                ModelState.AddModelError("", "An error occurred while creating the camera.");
            }
        }

        await PopulateStoresDropdown();
        return View(model);
    }

    // GET: /Camera/Edit/5
    public async Task<IActionResult> Edit(long id)
    {
        try
        {
            var camera = await _context.Cameras.FindAsync(id);
            if (camera == null)
            {
                TempData["ErrorMessage"] = $"Camera with ID {id} not found.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new CameraModel
            {
                Id = camera.Id,
                StoreId = camera.StoreId ?? 0,
                Name = camera.Name,
                IpAddress = camera.IpAddress,
                Port = camera.Port,
                Username = camera.Username,
                Password = camera.Password,
                ServerPort = camera.ServerPort
            };

            await PopulateStoresDropdown(camera.StoreId);
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting camera {CameraId} for edit", id);
            TempData["ErrorMessage"] = "An error occurred while retrieving the camera.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: /Camera/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, CameraModel model)
    {
        if (id != model.Id)
        {
            TempData["ErrorMessage"] = "Invalid camera ID.";
            return RedirectToAction(nameof(Index));
        }

        if (ModelState.IsValid)
        {
            try
            {
                var camera = await _context.Cameras.FindAsync(id);
                if (camera == null)
                {
                    TempData["ErrorMessage"] = $"Camera with ID {id} not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate that the store exists
                var storeExists = await _context.Stores.AnyAsync(s => s.Id == model.StoreId);
                if (!storeExists)
                {
                    ModelState.AddModelError("StoreId", "Selected store does not exist");
                    await PopulateStoresDropdown(camera.StoreId);
                    return View(model);
                }

                camera.StoreId = model.StoreId;
                camera.Name = model.Name;
                camera.IpAddress = model.IpAddress;
                camera.Port = model.Port;
                camera.Username = model.Username;
                camera.Password = model.Password;
                camera.ServerPort = model.ServerPort;
                camera.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated camera {CameraId}: {CameraName}", camera.Id, camera.Name);
                TempData["SuccessMessage"] = "Camera updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating camera {CameraId}", id);
                ModelState.AddModelError("", "An error occurred while updating the camera.");
            }
        }

        await PopulateStoresDropdown(model.StoreId);
        return View(model);
    }

    // GET: /Camera/Delete/5
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            var camera = await _context.Cameras
                .Include(c => c.Store)
                .Include(c => c.FileDownloadJobs)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (camera == null)
            {
                TempData["ErrorMessage"] = $"Camera with ID {id} not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(camera);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting camera {CameraId} for delete", id);
            TempData["ErrorMessage"] = "An error occurred while retrieving the camera.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: /Camera/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        try
        {
            var camera = await _context.Cameras
                .Include(c => c.FileDownloadJobs)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (camera == null)
            {
                TempData["ErrorMessage"] = $"Camera with ID {id} not found.";
                return RedirectToAction(nameof(Index));
            }

            // Delete associated download jobs
            if (camera.FileDownloadJobs.Any())
            {
                _context.FileDownloadJobs.RemoveRange(camera.FileDownloadJobs);
            }

            _context.Cameras.Remove(camera);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted camera {CameraId}: {CameraName}", camera.Id, camera.Name);
            TempData["SuccessMessage"] = "Camera deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting camera {CameraId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the camera.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: /Camera/TestConnection/5
    public async Task<IActionResult> TestConnection(long id)
    {
        try
        {
            var chList = await _hikvisionService.TestCameraConnectionAsync(id);
            TempData["SuccessMessage"] = "Camera channels are: <br> " + string.Join(" <br> ", chList);
            
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection for camera {CameraId}", id);
            TempData["ErrorMessage"] = "An error occurred while testing the camera connection.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // Helper methods
    private async Task PopulateStoresDropdown(long? selectedStoreId = null)
    {
        var stores = await _context.Stores.OrderBy(s => s.Name).ToListAsync();
        ViewBag.Stores = new SelectList(stores, "Id", "Name", selectedStoreId);
    }

    // View Models
    public class CameraModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "Store is required")]
        [Display(Name = "Store")]
        public long StoreId { get; set; }

        [Required(ErrorMessage = "Camera name is required")]
        [StringLength(100, ErrorMessage = "Camera name cannot be longer than 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "IP address is required")]
        [Display(Name = "IP Address")]
        public string IpAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Port is required")]
        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
        public int Port { get; set; } = 554;

        public string? Username { get; set; }

        public string? Password { get; set; }

        [Display(Name = "Server Port")]
        [Range(1, 65535, ErrorMessage = "Server port must be between 1 and 65535")]
        public int? ServerPort { get; set; }
    }
}