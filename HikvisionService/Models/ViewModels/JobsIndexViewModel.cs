using Microsoft.AspNetCore.Mvc.Rendering;

namespace HikvisionService.Models.ViewModels;

public class JobsIndexViewModel
{
    public IEnumerable<FileDownloadJob> Jobs { get; set; } = new List<FileDownloadJob>();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalJobs { get; set; }
    public int PageSize { get; set; } = 25;
    
    // Filters
    public long? StoreId { get; set; }
    public long? CameraId { get; set; }
    public string? Status { get; set; }
    
    // Dropdowns
    public IEnumerable<SelectListItem> Stores { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Cameras { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
    
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}