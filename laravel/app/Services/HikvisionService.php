<?php

namespace App\Services;

use App\Models\Camera;
use App\Models\FileDownloadJob;
use App\Models\StorageDrive;
use Carbon\Carbon;
use Illuminate\Support\Collection;
use Illuminate\Support\Facades\Log;

class HikvisionService
{
    public function __construct()
    {
        // Initialize dummy service
    }

    /**
     * Test camera connection (dummy implementation)
     */
    public function testCameraConnection(int $cameraId): array
    {
        $camera = Camera::find($cameraId);
        if (!$camera) {
            return ['Camera not found'];
        }

        $results = [];

        try {

            // Simulate connection test - randomly succeed or fail for demo
            $command = config('app.ezviz-console') . 'test';
            $command .= ' ' . $camera->ip;
            $command .= ' ' . $camera->port;
            $command .= ' ' . $camera->username;
            $command .= ' ' . $camera->password;
            $console = shell_exec( $command);
            $isOnline = str_contains($console, "Result: {") && str_contains($console, "Connection successfu");

            if ($isOnline) {
                $camera->update([
                    'is_online' => true,
                    'last_online_at' => now(),
                    'last_health_check_at' => now(),
                    'last_error' => null,
                    'last_error_at' => null,
                ]);

                $results[] = "Camera {$camera->name} connection successful";

                // Simulate channels
                for ($i = 1; $i <= rand(1, 4); $i++) {
                    $isChannelOnline = rand(1, 10) > 1; // 90% channel success rate
                    $results[] = "Channel {$i}: " . ($isChannelOnline ? 'Online' : 'Offline');
                }
            } else {
                $errorMessage = 'Connection timeout or authentication failed';
                $camera->update([
                    'is_online' => false,
                    'last_health_check_at' => now(),
                    'last_error' => $errorMessage,
                    'last_error_at' => now(),
                ]);

                $results[] = "Camera {$camera->name} connection failed: {$errorMessage}";
            }
            sleep(1);
        } catch (\Exception $ex) {
            $errorMessage = "Connection error: {$ex->getMessage()}";
            $camera->update([
                'is_online' => false,
                'last_health_check_at' => now(),
                'last_error' => $errorMessage,
                'last_error_at' => now(),
            ]);

            $results[] = "Camera {$camera->name} connection failed: {$errorMessage}";
        }

        return $results;
    }

    /**
     * Get dashboard data
     */
    public function getDashboardData(): array
    {
        $cameras = Camera::with('store')->get();
        $totalCameras = $cameras->count();
        $onlineCameras = $cameras->where('is_online', true)->count();
        $offlineCameras = $totalCameras - $onlineCameras;

        $activeDownloadJobs = FileDownloadJob::where('status', 'downloading')->count();
        $failedDownloadJobs = FileDownloadJob::where('status', 'failed')->count();
        $completedDownloadJobs = FileDownloadJob::where('status', 'completed')->count();

        $offlineCamerasList = $cameras->where('is_online', false)
            ->sortByDesc('last_online_at')
            ->take(10)
            ->map(function ($camera) {
                return [
                    'id' => $camera->id,
                    'name' => $camera->name,
                    'is_online' => $camera->is_online,
                    'last_online_at' => $camera->last_online_at,
                    'last_downloaded_at' => $camera->last_downloaded_at,
                    'status' => $camera->status,
                ];
            });

        $storageDrives = StorageDrive::all()->map(function ($drive) {
            return [
                'id' => $drive->id,
                'name' => $drive->name,
                'root_path' => $drive->root_path,
                'total_space' => $drive->total_space,
                'used_space' => $drive->used_space,
                'free_space' => $drive->free_space,
                'status' => $drive->status,
                'usage_percentage' => $drive->usage_percentage,
                'last_checked_at' => $drive->last_checked_at,
            ];
        });

        // Generate downloads per day (last 7 days)
        $downloadsPerDay = collect();
        for ($i = 6; $i >= 0; $i--) {
            $date = Carbon::now()->subDays($i);
            $count = FileDownloadJob::whereDate('end_time', $date->toDateString())
                ->where('status', 'completed')
                ->count();

            $downloadsPerDay->push([
                'label' => $date->format('Y-m-d'),
                'value' => $count,
            ]);
        }

        // Generate downloads per camera
        $downloadsPerCamera = FileDownloadJob::with('camera')
            ->where('status', 'completed')
            ->get()
            ->groupBy('camera.name')
            ->map(function ($jobs, $cameraName) {
                return $jobs->count();
            })
            ->toArray();
        return [
            'total_cameras' => $totalCameras,
            'online_cameras' => $onlineCameras,
            'offline_cameras' => $offlineCameras,
            'active_download_jobs' => $activeDownloadJobs,
            'failed_download_jobs' => $failedDownloadJobs,
            'completed_download_jobs' => $completedDownloadJobs,
            'offline_cameras_list' => $offlineCamerasList,
            'storage_drives' => $storageDrives,
            'downloads_per_day' => $downloadsPerDay,
            'downloads_per_camera' => $downloadsPerCamera,
            'cameras' => $cameras,
        ];
    }

    /**
     * Get footage files (simulated)
     */
    public function getFootageFiles(?int $cameraId, Carbon $startDate, Carbon $endDate, string $fileType = 'both'): Collection
    {
        $query = FileDownloadJob::with('camera')
            ->where('status', 'completed')
            ->whereBetween('file_start_time', [$startDate, $endDate]);

        if ($cameraId) {
            $query->where('camera_id', $cameraId);
        }

        if ($fileType !== 'both') {
            $query->where('file_type', $fileType);
        }

        return $query->orderByDesc('file_start_time')
            ->get()
            ->map(function ($job) {
                return [
                    'file_name' => $job->file_name,
                    'file_path' => $job->download_path,
                    'file_type' => $job->file_type,
                    'file_size' => $job->file_size,
                    'file_start_time' => $job->file_start_time,
                    'file_end_time' => $job->file_end_time,
                    'camera_name' => $job->camera->name,
                    'camera_id' => $job->camera_id,
                    'thumbnail_path' => $job->file_type === 'video' ? '/images/video-placeholder.jpg' : $job->download_path,
                    'download_url' => route('footage.download', ['path' => urlencode($job->download_path)]),
                ];
            });
    }

    /**
     * Simulate storage check
     */
    public function checkStorageDrives(): void
    {
        $drives = StorageDrive::all();

        foreach ($drives as $drive) {
            // Simulate disk usage check
            $totalSpace = rand(100 * 1024 * 1024 * 1024, 1000 * 1024 * 1024 * 1024); // 100GB to 1TB
            $usedSpace = rand(10 * 1024 * 1024 * 1024, $totalSpace * 0.9); // 10GB to 90% of total
            $freeSpace = $totalSpace - $usedSpace;

            $drive->update([
                'total_space' => $totalSpace,
                'used_space' => $usedSpace,
                'free_space' => $freeSpace,
                'status' => $usedSpace / $totalSpace > 0.9 ? 'warning' : 'healthy',
                'last_checked_at' => now(),
            ]);

            Log::info("Updated storage drive {$drive->name}: " . round($usedSpace / $totalSpace * 100, 1) . "% used");
        }
    }
}
