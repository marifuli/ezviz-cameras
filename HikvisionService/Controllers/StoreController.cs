using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HikvisionService.Data;
using HikvisionService.Models;

namespace HikvisionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoreController : ControllerBase
{
    private readonly HikvisionDbContext _context;
    private readonly ILogger<StoreController> _logger;

    public StoreController(HikvisionDbContext context, ILogger<StoreController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all stores
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetStores()
    {
        try
        {
            var stores = await _context.Stores
                .Include(s => s.Cameras)
                .OrderBy(s => s.Name)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.CreatedAt,
                    s.UpdatedAt,
                    CamerasCount = s.Cameras.Count
                })
                .ToListAsync();

            return Ok(stores);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stores");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get store by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetStore(long id)
    {
        try
        {
            var store = await _context.Stores
                .Include(s => s.Cameras)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (store == null)
            {
                return NotFound($"Store with ID {id} not found");
            }

            return Ok(store);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting store {StoreId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new store
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateStore([FromBody] CreateStoreRequest request)
    {
        try
        {
            // Check if store name already exists
            if (await _context.Stores.AnyAsync(s => s.Name == request.Name))
            {
                return BadRequest("Store name already exists");
            }

            var store = new Store
            {
                Name = request.Name,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created store: {StoreName} with ID: {StoreId}", store.Name, store.Id);

            return CreatedAtAction(nameof(GetStore), new { id = store.Id }, store);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating store");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update existing store
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStore(long id, [FromBody] UpdateStoreRequest request)
    {
        try
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null)
            {
                return NotFound($"Store with ID {id} not found");
            }

            // Check if new name already exists (excluding current store)
            if (await _context.Stores.AnyAsync(s => s.Name == request.Name && s.Id != id))
            {
                return BadRequest("Store name already exists");
            }

            store.Name = request.Name;
            store.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated store {StoreId}: {StoreName}", store.Id, store.Name);

            return Ok(store);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating store {StoreId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete store
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStore(long id)
    {
        try
        {
            var store = await _context.Stores
                .Include(s => s.Cameras)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (store == null)
            {
                return NotFound($"Store with ID {id} not found");
            }

            // Check if store has cameras
            if (store.Cameras.Any())
            {
                return BadRequest("Cannot delete store with associated cameras. Please delete cameras first.");
            }

            _context.Stores.Remove(store);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted store {StoreId}: {StoreName}", store.Id, store.Name);

            return Ok(new { message = "Store deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting store {StoreId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    // Request DTOs
    public class CreateStoreRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateStoreRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}