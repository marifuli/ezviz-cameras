using HikvisionService.Models;
using Hik.Api.Data;

namespace HikvisionService.Services;

public interface IHikvisionService
{
    Task<List<HikRemoteFile>> GetAvailableFilesAsync(long cameraId, DateTime startTime, DateTime endTime, string fileType = "both");
    Task<bool> TestCameraConnectionAsync(long cameraId);
    Task<List<Camera>> GetAllCamerasAsync();
    Task<Camera?> GetCameraByIdAsync(long id);
}