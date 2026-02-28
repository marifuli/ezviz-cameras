# Camera Service

A simplified service for interacting with IP cameras, extracted from the original CameraWorker.cs. This service provides three main functions:

1. **Check Camera Connection** - Verify if the camera is accessible and get channel information
2. **Get Video File List** - Retrieve a list of video files for a specific time period and save to a file
3. **Download Video File** - Download a specific video file to a given path

## Usage

### 1. Using the CameraService Class Directly

```csharp
using Console.Services;

var cameraService = new CameraService();

// Setup camera credentials
var credentials = new CameraService.CameraCredentials
{
    IpAddress = "192.168.1.100",
    Port = 8000,
    Username = "admin",
    Password = "your_password"
};

// Check connection
var connectionResult = await cameraService.CheckCameraConnectionAsync(credentials);
if (connectionResult.IsSuccess)
{
    Console.WriteLine($"✅ {connectionResult.Message}");
    foreach (var channel in connectionResult.ChannelInfo)
    {
        Console.WriteLine($"📹 {channel}");
    }
}

// Get video file list
var startTime = DateTime.Now.AddDays(-1);
var endTime = DateTime.Now;
var fileListResult = await cameraService.GetVideoFileListAsync(
    credentials, 
    startTime, 
    endTime, 
    "video_list.json"
);

// Download a file
var downloadResult = await cameraService.DownloadFileAsync(
    credentials,
    "video001.h264",
    "downloads/video001.mp4",
    progress => Console.WriteLine($"Progress: {progress}%")
);
```

### 2. Using the Command Line Interface

The console application now includes camera commands:

```bash
# Test camera connection
./Console camera test 192.168.1.100 8000 admin password123

# Get video file list for a time period
./Console camera list 192.168.1.100 8000 admin password123 "2024-01-01 00:00:00" "2024-01-01 23:59:59" "video_list.json"

# Download a specific video file
./Console camera download 192.168.1.100 8000 admin password123 "video001.h264" "downloads/video001.mp4"

# Run the demo example
./Console camera demo
```

## Classes and Models

### CameraService.CameraCredentials
- `IpAddress`: Camera IP address (required)
- `Port`: Camera port (default: 8000)
- `Username`: Camera username (default: "admin")
- `Password`: Camera password (required)

### CameraService.ConnectionResult
- `IsSuccess`: Whether the connection was successful
- `Message`: Status message
- `ChannelInfo`: List of channel information strings

### CameraService.FileListResult
- `IsSuccess`: Whether the operation was successful
- `Message`: Status message
- `FileCount`: Number of files found
- `SavedToPath`: Path where the file list was saved

### CameraService.DownloadResult
- `IsSuccess`: Whether the download was successful
- `Message`: Status message
- `DownloadPath`: Path where the file was saved
- `FileSize`: Size of the downloaded file in bytes

### CameraService.VideoFileInfo
- `Name`: Video file name
- `Size`: File size in bytes
- `StartTime`: Video start time
- `EndTime`: Video end time
- `Duration`: Video duration in seconds
- `ChannelNumber`: Camera channel number

## Features

- **Async/Await Support**: All methods are asynchronous
- **Error Handling**: Comprehensive error handling with meaningful messages
- **Progress Tracking**: Download progress callback support
- **Multi-Channel Support**: Works with both direct cameras and NVR systems
- **Auto-Cleanup**: Proper resource disposal and logout handling
- **Cancellation Support**: CancellationToken support for all async operations

## Dependencies

- Hik.Api - For camera communication
- Newtonsoft.Json - For JSON serialization
- .NET 6.0 or higher

## Notes

- The service automatically handles HikApi initialization and cleanup
- All methods properly login and logout from the camera
- File lists are saved in JSON format for easy parsing
- Progress callbacks are optional for download operations
- The service supports both direct camera connections and NVR systems with multiple channels