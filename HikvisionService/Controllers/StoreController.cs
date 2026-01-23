using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HikvisionService.Data;
using HikvisionService.Models;
using System.ComponentModel.DataAnnotations;

namespace HikvisionService.Controllers;

public class StoreController : Controller
{
    private readonly HikvisionDbContext _context;
    private readonly ILogger<StoreController> _logger;

    public StoreController(HikvisionDbContext context, ILogger<StoreController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: /Store
    public async Task<IActionResult> Index()
    {
        try
        {
            var stores = await _context.Stores
                .Include(s => s.Cameras)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(stores);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stores");
            TempData["ErrorMessage"] = "An error occurred while retrieving stores.";
            return View(new List<Store>());
        }
    }

    // GET: /Store/Details/5
    public async Task<IActionResult> Details(long id)
    {
        try
        {
            var store = await _context.Stores
                .Include(s => s.Cameras)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (store == null)
            {
                TempData["ErrorMessage"] = $"Store with ID {id} not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(store);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting store {StoreId}", id);
            TempData["ErrorMessage"] = "An error occurred while retrieving the store.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: /Store/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Store/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StoreModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Check if store name already exists
                if (await _context.Stores.AnyAsync(s => s.Name == model.Name))
                {
                    ModelState.AddModelError("Name", "Store name already exists");
                    return View(model);
                }

                var store = new Store
                {
                    Name = model.Name,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Stores.Add(store);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created store: {StoreName} with ID: {StoreId}", store.Name, store.Id);
                TempData["SuccessMessage"] = "Store created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating store");
                ModelState.AddModelError("", "An error occurred while creating the store.");
            }
        }

        return View(model);
    }

    // GET: /Store/Edit/5
    public async Task<IActionResult> Edit(long id)
    {
        try
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null)
            {
                TempData["ErrorMessage"] = $"Store with ID {id} not found.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new StoreModel
            {
                Id = store.Id,
                Name = store.Name
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting store {StoreId} for edit", id);
            TempData["ErrorMessage"] = "An error occurred while retrieving the store.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: /Store/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, StoreModel model)
    {
        if (id != model.Id)
        {
            TempData["ErrorMessage"] = "Invalid store ID.";
            return RedirectToAction(nameof(Index));
        }

        if (ModelState.IsValid)
        {
            try
            {
                var store = await _context.Stores.FindAsync(id);
                if (store == null)
                {
                    TempData["ErrorMessage"] = $"Store with ID {id} not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if new name already exists (excluding current store)
                if (await _context.Stores.AnyAsync(s => s.Name == model.Name && s.Id != id))
                {
                    ModelState.AddModelError("Name", "Store name already exists");
                    return View(model);
                }

                store.Name = model.Name;
                store.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated store {StoreId}: {StoreName}", store.Id, store.Name);
                TempData["SuccessMessage"] = "Store updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating store {StoreId}", id);
                ModelState.AddModelError("", "An error occurred while updating the store.");
            }
        }

        return View(model);
    }

    // GET: /Store/Delete/5
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            var store = await _context.Stores
                .Include(s => s.Cameras)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (store == null)
            {
                TempData["ErrorMessage"] = $"Store with ID {id} not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(store);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting store {StoreId} for delete", id);
            TempData["ErrorMessage"] = "An error occurred while retrieving the store.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: /Store/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        try
        {
            var store = await _context.Stores
                .Include(s => s.Cameras)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (store == null)
            {
                TempData["ErrorMessage"] = $"Store with ID {id} not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check if store has cameras
            if (store.Cameras.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete store with associated cameras. Please delete cameras first.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Stores.Remove(store);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted store {StoreId}: {StoreName}", store.Id, store.Name);
            TempData["SuccessMessage"] = "Store deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting store {StoreId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the store.";
            return RedirectToAction(nameof(Index));
        }
    }

    // View Models
    public class StoreModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "Store name is required")]
        [StringLength(100, ErrorMessage = "Store name cannot be longer than 100 characters")]
        public string Name { get; set; } = string.Empty;
    }
}