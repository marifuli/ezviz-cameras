using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hik.Api;
using Hik.Api.Data;
using Hik.Api.Abstraction;
using Newtonsoft.Json;

namespace ConsoleApp.Services
{
    public class CameraService
    {
        public class CameraCredentials
        {
            public string IpAddress { get; set; } = string.Empty;
            public int Port { get; set; } = 8000;
            public string Username { get; set; } = "admin";
            public string Password { get; set; } = string.Empty;
        }

        public class VideoFileInfo
        {
            public string Name { get; set; } = string.Empty;
            public long Size { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public int Duration { get; set; }
            public int ChannelNumber { get; set; }
        }

        public class ConnectionResult
        {
            public bool IsSuccess { get; set; }
            public string Message { get; set; } = string.Empty;
            public List<string> ChannelInfo { get; set; } = new List<string>();
        }

        public class FileListResult
        {
            public bool IsSuccess { get; set; }
            public string Message { get; set; } = string.Empty;
            public int FileCount { get; set; }
            public string SavedToPath { get; set; } = string.Empty;
        }

        public class DownloadResult
        {
            public bool IsSuccess { get; set; }
            public string Message { get; set; } = string.Empty;
            public string DownloadPath { get; set; } = string.Empty;
            public long FileSize { get; set; }
        }

        private void InitializeHikApi()
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            HikApi.SetLibraryPath(currentDirectory);
            
            HikApi.Initialize(
                logLevel: 3,
                logDirectory: "logs",
                autoDeleteLogs: true,
                waitTimeMilliseconds: 5000,
                forceReinitialization: true
            );
        }

        /// <summary>
        /// Check if the camera connection is working
        /// </summary>
        /// <param name="credentials">Camera login credentials</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Connection result with status and channel information</returns>
        public async Task<ConnectionResult> CheckCameraConnectionAsync(
            CameraCredentials credentials, 
            CancellationToken cancellationToken = default)
        {
            IHikApi? hikApi = null;
            var result = new ConnectionResult();

            try
            {
                InitializeHikApi();

                hikApi = HikApi.Login(
                    credentials.IpAddress,
                    credentials.Port,
                    credentials.Username,
                    credentials.Password
                );

                result.IsSuccess = true;
                result.Message = "Connection successful";

                // Get channel information
                if (hikApi.IpChannels.Any())
                {
                    foreach (var channel in hikApi.IpChannels)
                    {
                        result.ChannelInfo.Add($"Channel {channel.ChannelNumber}: {channel.Name} (Online: {channel.IsOnline})");
                    }
                }
                else
                {
                    result.ChannelInfo.Add("Direct camera connection (no IP channels)");
                }

                await Task.Delay(100, cancellationToken); // Small delay to ensure connection is stable
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"Connection failed: {ex.Message}";
            }
            finally
            {
                if (hikApi != null)
                {
                    try { hikApi.Logout(); } catch { /* Ignore logout errors */ }
                }
            }

            return result;
        }

        /// <summary>
        /// Get video file list for a time period and save it to a file
        /// </summary>
        /// <param name="credentials">Camera login credentials</param>
        /// <param name="startTime">Start time for video search</param>
        /// <param name="endTime">End time for video search</param>
        /// <param name="saveFilePath">File path to save the video list</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result with file count and save path</returns>
        public async Task<FileListResult> GetVideoFileListAsync(
            CameraCredentials credentials,
            DateTime startTime,
            DateTime endTime,
            string saveFilePath,
            CancellationToken cancellationToken = default)
        {
            IHikApi? hikApi = null;
            var result = new FileListResult();

            try
            {
                InitializeHikApi();

                hikApi = HikApi.Login(
                    credentials.IpAddress,
                    credentials.Port,
                    credentials.Username,
                    credentials.Password
                );

                var allFiles = new List<VideoFileInfo>();

                // Check if it's an NVR with IP channels
                if (hikApi.IpChannels.Any())
                {
                    foreach (var channel in hikApi.IpChannels.Where(c => c.IsOnline))
                    {
                        var videos = await hikApi.VideoService.FindFilesAsync(startTime, endTime, channel.ChannelNumber);
                        
                        foreach (var video in videos)
                        {
                            allFiles.Add(new VideoFileInfo
                            {
                                Name = video.Name,
                                Size = video.Size,
                                StartTime = video.Date,
                                EndTime = video.Date.AddSeconds(video.Duration),
                                Duration = video.Duration,
                                ChannelNumber = channel.ChannelNumber
                            });
                        }
                    }
                }
                else
                {
                    var videos = await hikApi.VideoService.FindFilesAsync(startTime, endTime);
                    
                    foreach (var video in videos)
                    {
                        allFiles.Add(new VideoFileInfo
                        {
                            Name = video.Name,
                            Size = video.Size,
                            StartTime = video.Date,
                            EndTime = video.Date.AddSeconds(video.Duration),
                            Duration = video.Duration,
                            ChannelNumber = 1 // Default channel for direct camera
                        });
                    }
                }

                // Ensure directory exists
                string? directory = Path.GetDirectoryName(saveFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Save file list to JSON
                string jsonContent = JsonConvert.SerializeObject(allFiles, Formatting.Indented);
                await File.WriteAllTextAsync(saveFilePath, jsonContent, cancellationToken);

                result.IsSuccess = true;
                result.Message = "Video file list retrieved and saved successfully";
                result.FileCount = allFiles.Count;
                result.SavedToPath = saveFilePath;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"Failed to get video file list: {ex.Message}";
            }
            finally
            {
                if (hikApi != null)
                {
                    try { hikApi.Logout(); } catch { /* Ignore logout errors */ }
                }
            }

            return result;
        }

        /// <summary>
        /// Download a specific video file
        /// </summary>
        /// <param name="credentials">Camera login credentials</param>
        /// <param name="fileName">Name of the file to download</param>
        /// <param name="downloadPath">Local path to save the downloaded file</param>
        /// <param name="progressCallback">Optional callback for progress updates (percentage 0-100)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Download result with success status and file information</returns>
        public async Task<DownloadResult> DownloadFileAsync(
            CameraCredentials credentials,
            string fileName,
            string downloadPath,
            Action<int>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            IHikApi? hikApi = null;
            var result = new DownloadResult();
            int downloadId = -1;

            try
            {
                InitializeHikApi();

                hikApi = HikApi.Login(
                    credentials.IpAddress,
                    credentials.Port,
                    credentials.Username,
                    credentials.Password
                );

                // Ensure directory exists
                string? directory = Path.GetDirectoryName(downloadPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string tempFilePath = $"{downloadPath}.tmp";

                // Start download
                downloadId = hikApi.VideoService.StartDownloadFile(fileName, tempFilePath);

                // Monitor download progress
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(2000, cancellationToken); // Check every 2 seconds
                    
                    int progress = hikApi.VideoService.GetDownloadPosition(downloadId);
                    
                    // Notify progress if callback is provided
                    progressCallback?.Invoke(progress);

                    if (progress >= 100)
                    {
                        hikApi.VideoService.StopDownloadFile(downloadId);
                        downloadId = -1; // Mark as stopped
                        
                        // Move file to final location
                        File.Move(tempFilePath, downloadPath, true);
                        
                        // Get file size
                        var fileInfo = new FileInfo(downloadPath);
                        
                        result.IsSuccess = true;
                        result.Message = "File downloaded successfully";
                        result.DownloadPath = downloadPath;
                        result.FileSize = fileInfo.Length;
                        break;
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    result.IsSuccess = false;
                    result.Message = "Download was cancelled";
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"Download failed: {ex.Message}";
            }
            finally
            {
                // Stop download if it's still running
                if (hikApi != null && downloadId >= 0)
                {
                    try { hikApi.VideoService.StopDownloadFile(downloadId); } catch { /* Ignore */ }
                }
                
                if (hikApi != null)
                {
                    try { hikApi.Logout(); } catch { /* Ignore logout errors */ }
                }
            }

            return result;
        }
    }
}