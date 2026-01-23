# Hikvision Service

A .NET 6 Web API service for managing Hikvision cameras and accessing their files. This service integrates with the Laravel project by sharing the SQLite database.

## Features

- **Camera Management**: View cameras from the shared database
- **File Discovery**: List available video and photo files from cameras
- **Background JobTracking**: Track file download progress and status (future feature)
- **Cross-platform Support**: Runs on Linux using Hikvision Linux SDK

## API Endpoints

### Cameras
- `GET /api/camera` - Get all cameras
- `GET /api/camera/{id}` - Get camera by ID
- `GET /api/camera/{id}/files` - Get available files from camera
  - Query parameters:
    - `startTime`: Start time for file search (default: 4 hours ago)
    - `endTime`: End time for file search (default: now)
    - `fileType`: Type of files to search ("video", "photo", or "both")
- `POST /api/camera/{id}/test-connection` - Test camera connectivity

## Configuration

The service reads the shared SQLite database from the Laravel project at `../laravel/database/database.sqlite`.

### Database Tables

The service creates additional tables for file management:
- `file_download_jobs`: Track download progress and status

### Linux Dependencies

The service requires the Hikvision Linux SDK libraries to be available. These are automatically copied from the `lib/` folder during build.

## Getting Started

1. Navigate to the service directory:
   ```bash
   cd dotnet-service/HikvisionService
   ```

2. Run the service:
   ```bash
   dotnet run
   ```

3. Access the API at `https://localhost:7149` or `http://localhost:5149`

4. View the Swagger documentation at `https://localhost:7149/swagger`

## Integration with Laravel

The service shares the same SQLite database as the Laravel project, allowing seamless integration. Camera information is read from the Laravel `cameras` table, while file management data is stored in service-specific tables.