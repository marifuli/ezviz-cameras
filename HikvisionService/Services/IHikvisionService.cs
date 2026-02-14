using HikvisionService.Models;
using HikvisionService.Models.ViewModels;
using HikvisionService.Data;
using Hik.Api.Data;

namespace HikvisionService.Services;

public interface IHikvisionService
{
    Task<List<HikRemoteFile>> GetAvailableFilesAsync(long cameraId, DateTime startTime, DateTime endTime, string fileType = "both");
    Task<List<string>> TestCameraConnectionAsync(long cameraId);
    Task<List<Camera>> GetAllCamerasAsync();
    Task<Camera?> GetCameraByIdAsync(long id);
    
    // Camera health check methods
    Task<bool> CheckCameraConnectionAsync(long cameraId);
    Task TriggerCameraHealthCheckAsync();
    
    // Storage drive methods
    Task<List<StorageDrive>> GetAllStorageDrivesAsync();
    Task<StorageDrive?> GetStorageDriveByIdAsync(long id);
    Task<StorageDrive> AddStorageDriveAsync(StorageDrive drive);
    Task<bool> UpdateStorageDriveAsync(StorageDrive drive);
    Task<bool> DeleteStorageDriveAsync(long id);
    Task TriggerStorageCheckAsync();
    
    // Download job methods
    Task<List<FileDownloadJobDto>> GetAllDownloadJobsAsync();
    Task<List<FileDownloadJob>> GetActiveDownloadJobsAsync();
    Task<List<FileDownloadJob>> GetFailedDownloadJobsAsync();
    Task<List<FileDownloadJob>> GetCompletedDownloadJobsAsync();
    Task<FileDownloadJob?> GetDownloadJobByIdAsync(long id);
    Task<bool> RetryDownloadJobAsync(long id);
    Task<bool> CancelDownloadJobAsync(long id);
    
    // Dashboard methods
    Task<DashboardViewModel> GetDashboardDataAsync();
    
    // Footage methods
    Task<List<FootageFileViewModel>> GetFootageFilesAsync(long? cameraId, DateTime startDate, DateTime endDate, string fileType = "both");
    Task<string> GetFootageDownloadUrlAsync(string filePath);
}