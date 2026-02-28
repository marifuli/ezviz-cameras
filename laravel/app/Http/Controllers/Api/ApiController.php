<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Camera;
use App\Models\FileDownloadJob;
use App\Models\StorageDrive;
use App\Services\HikvisionService;
use Carbon\Carbon;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Log;
use Illuminate\Support\Facades\Storage;
use Symfony\Component\HttpFoundation\BinaryFileResponse;

class ApiController extends Controller
{
    protected $hikvisionService;

    public function __construct(HikvisionService $hikvisionService)
    {
        $this->hikvisionService = $hikvisionService;
    }

    // Dashboard data
    public function getDashboardData(): JsonResponse
    {
        try {
            $dashboardData = $this->hikvisionService->getDashboardData();
            return response()->json($dashboardData);
        } catch (\Exception $ex) {
            Log::error('Error getting dashboard data', ['error' => $ex->getMessage()]);
            return response()->json(['error' => 'Failed to get dashboard data'], 500);
        }
    }

    public function getWorkerStatus(): JsonResponse
    {
        try {
            $dashboardData = $this->hikvisionService->getDashboardData();
            return response()->json($dashboardData['worker_status']);
        } catch (\Exception $ex) {
            Log::error('Error getting worker status', ['error' => $ex->getMessage()]);
            return response()->json(['error' => 'Failed to get worker status'], 500);
        }
    }

    // Camera endpoints
    public function getAllCameras(): JsonResponse
    {
        try {
            $cameras = Camera::with('store')->get();
            return response()->json($cameras);
        } catch (\Exception $ex) {
            Log::error('Error getting all cameras', ['error' => $ex->getMessage()]);
            return response()->json(['error' => 'Failed to get cameras'], 500);
        }
    }

    public function getCamera($id): JsonResponse
    {
        try {
            $camera = Camera::with('store')->find($id);
            if (!$camera) {
                return response()->json(['error' => "Camera with ID {$id} not found"], 404);
            }
            return response()->json($camera);
        } catch (\Exception $ex) {
            Log::error("Error getting camera {$id}", ['error' => $ex->getMessage()]);
            return response()->json(['error' => "Failed to get camera {$id}"], 500);
        }
    }

    public function checkCameraConnection($id): JsonResponse
    {
        try {
            $isOnline = false;
            $results = $this->hikvisionService->testCameraConnection($id);
            foreach ($results as $result) {
                if (strpos($result, 'successful') !== false) {
                    $isOnline = true;
                }
            }
            return response()->json(['isOnline' => $isOnline]);
        } catch (\Exception $ex) {
            Log::error("Error checking camera {$id} connection", ['error' => $ex->getMessage()]);
            return response()->json(['error' => "Failed to check camera {$id} connection"], 500);
        }
    }

    public function checkAllCameras(): JsonResponse
    {
        try {
            // Simulate health check for all cameras
            $cameras = Camera::all();
            foreach ($cameras as $camera) {
                $this->hikvisionService->testCameraConnection($camera->id);
            }

            return response()->json(['message' => 'Camera health check triggered']);
        } catch (\Exception $ex) {
            Log::error('Error triggering camera health check', ['error' => $ex->getMessage()]);
            return response()->json(['error' => 'Failed to trigger camera health check'], 500);
        }
    }

    // Storage drive endpoints
    public function getAllStorageDrives(): JsonResponse
    {
        try {
            $drives = StorageDrive::all();
            return response()->json($drives);
        } catch (\Exception $ex) {
            Log::error('Error getting all storage drives', ['error' => $ex->getMessage()]);
            return response()->json(['error' => 'Failed to get storage drives'], 500);
        }
    }

    public function getStorageDrive($id): JsonResponse
    {
        try {
            $drive = StorageDrive::find($id);
            if (!$drive) {
                return response()->json(['error' => "Storage drive with ID {$id} not found"], 404);
            }
            return response()->json($drive);
        } catch (\Exception $ex) {
            Log::error("Error getting storage drive {$id}", ['error' => $ex->getMessage()]);
            return response()->json(['error' => "Failed to get storage drive {$id}"], 500);
        }
    }

    public function addStorageDrive(Request $request): JsonResponse
    {
        try {
            $request->validate([
                'name' => 'required|string|max:255',
                'root_path' => 'required|string|max:500',
            ]);

            $drive = StorageDrive::create([
                'name' => $request->name,
                'root_path' => $request->root_path,
                'total_space' => 0,
                'used_space' => 0,
                'free_space' => 0,
                'status' => 'unknown',
                'last_checked_at' => now(),
            ]);

            return response()->json($drive, 201);
        } catch (\Exception $ex) {
            Log::error('Error adding storage drive', ['error' => $ex->getMessage()]);
            return response()->json(['error' => 'Failed to add storage drive'], 500);
        }
    }

    public function updateStorageDrive(Request $request, $id): JsonResponse
    {
        try {
            $drive = StorageDrive::find($id);
            if (!$drive) {
                return response()->json(['error' => "Storage drive with ID {$id} not found"], 404);
            }

            $request->validate([
                'name' => 'required|string|max:255',
                'root_path' => 'required|string|max:500',
            ]);

            $drive->update([
                'name' => $request->name,
                'root_path' => $request->root_path,
            ]);

            return response()->json(null, 204);
        } catch (\Exception $ex) {
            Log::error("Error updating storage drive {$id}", ['error' => $ex->getMessage()]);
            return response()->json(['error' => "Failed to update storage drive {$id}"], 500);
        }
    }

    public function deleteStorageDrive($id): JsonResponse
    {
        try {
            $drive = StorageDrive::find($id);
            if (!$drive) {
                return response()->json(['error' => "Storage drive with ID {$id} not found"], 404);
            }

            $drive->delete();
            return response()->json(null, 204);
        } catch (\Exception $ex) {
            Log::error("Error deleting storage drive {$id}", ['error' => $ex->getMessage()]);
            return response()->json(['error' => "Failed to delete storage drive {$id}"], 500);
        }
    }

    public function checkStorageDrives(): JsonResponse
    {
        try {
            $this->hikvisionService->checkStorageDrives();
            return response()->json(['message' => 'Storage check triggered']);
        } catch (\Exception $ex) {
            Log::error('Error triggering storage check', ['error' => $ex->getMessage()]);
            return response()->json(['error' => 'Failed to trigger storage check'], 500);
        }
    }

    // Download job endpoints
    public function getAllDownloadJobs(): JsonResponse
    {
        try {
            $jobs = FileDownloadJob::with('camera')
                ->orderByDesc('created_at')
                ->get()
                ->map(function ($job) {
                    return [
                        'id' => $job->id,
                        'status' => $job->status,
                        'progress' => $job->progress,
                        'created_at' => $job->created_at,
                        'start_time' => $job->start_time,
                        'end_time' => $job->end_time,
                        'updated_at' => $job->updated_at,
                        'camera_id' => $job->camera_id,
                        'camera_name' => $job->camera->name,
                        'file_name' => $job->file_name,
                        'file_type' => $job->file_type,
                        'file_size' => $job->file_size,
                        'error_message' => $job->error_message,
                    ];
                });

            return response()->json($jobs);
        } catch (\Exception $ex) {
            Log::error('Error getting all download jobs', ['error' => $ex->getMessage()]);
            return response()->json(['error' => $ex->getMessage()], 500);
        }
    }

    public function getActiveDownloadJobs(): JsonResponse
    {
        try {
            $jobs = FileDownloadJob::with('camera')
                ->where('status', 'downloading')
                ->orderByDesc('start_time')
                ->get()
                ->map(function ($job) {
                    return [
                        'id' => $job->id,
                        'status' => $job->status,
                        'progress' => $job->progress,
                        'created_at' => $job->created_at,
                        'start_time' => $job->start_time,
                        'end_time' => $job->end_time,
                        'updated_at' => $job->updated_at,
                        'camera_id' => $job->camera_id,
                        'camera_name' => $job->camera->name,
                        'file_name' => $job->file_name,
                        'file_type' => $job->file_type,
                        'file_size' => $job->file_size,
                        'error_message' => $job->error_message,
                    ];
                });

            return response()->json($jobs);
        } catch (\Exception $ex) {
            Log::error('Error getting active download jobs', ['error' => $ex->getMessage()]);
            return response()->json(['error' => 'Failed to get active download jobs'], 500);
        }
    }

    public function getFailedDownloadJobs(): JsonResponse
    {
        try {
            $jobs = FileDownloadJob::with('camera')
                ->where('status', 'failed')
                ->orderByDesc('updated_at')
                ->get()
                ->map(function ($job) {
                    return [
                        'id' => $job->id,
                        'status' => $job->status,
                        'progress' => $job->progress,
                        'created_at' => $job->created_at,
                        'start_time' => $job->start_time,
                        'end_time' => $job->end_time,
                        'updated_at' => $job->updated_at,
                        'camera_id' => $job->camera_id,
                        'camera_name' => $job->camera->name,
                        'file_name' => $job->file_name,
                        'file_type' => $job->file_type,
                        'file_size' => $job->file_size,
                        'error_message' => $job->error_message,
                    ];
                });

            return response()->json($jobs);
        } catch (\Exception $ex) {
            Log::error('Error getting failed download jobs', ['error' => $ex->getMessage()]);
            return response()->json(['error' => 'Failed to get failed download jobs'], 500);
        }
    }

    public function getDownloadJob($id): JsonResponse
    {
        try {
            $job = FileDownloadJob::with('camera')->find($id);
            if (!$job) {
                return response()->json(['error' => "Download job with ID {$id} not found"], 404);
            }
            return response()->json($job);
        } catch (\Exception $ex) {
            Log::error("Error getting download job {$id}", ['error' => $ex->getMessage()]);
            return response()->json(['error' => "Failed to get download job {$id}"], 500);
        }
    }

    // Footage endpoints
    public function getFootageFiles(Request $request): JsonResponse
    {
        try {
            $cameraId = $request->query('cameraId');
            $startDate = Carbon::parse($request->query('startDate', now()->subDays(7)));
            $endDate = Carbon::parse($request->query('endDate', now()));
            $fileType = $request->query('fileType', 'both');

            $files = $this->hikvisionService->getFootageFiles($cameraId, $startDate, $endDate, $fileType);
            return response()->json($files);
        } catch (\Exception $ex) {
            Log::error('Error getting footage files', ['error' => $ex->getMessage()]);
            return response()->json(['error' => 'Failed to get footage files'], 500);
        }
    }

    public function downloadFootage(Request $request): BinaryFileResponse|JsonResponse
    {
        try {
            $path = $request->query('path');
            if (!$path) {
                return response()->json(['error' => 'Path parameter is required'], 400);
            }

            // For demo purposes, return a placeholder response
            // In real implementation, this would serve the actual file
            if (!file_exists($path)) {
                return response()->json(['error' => 'File not found'], 404);
            }

            // Determine content type
            $contentType = 'application/octet-stream';
            if (str_ends_with(strtolower($path), '.mp4')) {
                $contentType = 'video/mp4';
            } elseif (str_ends_with(strtolower($path), '.jpg') || str_ends_with(strtolower($path), '.jpeg')) {
                $contentType = 'image/jpeg';
            }

            return response()->file($path, [
                'Content-Type' => $contentType,
                'Content-Disposition' => 'attachment; filename="' . basename($path) . '"',
            ]);
        } catch (\Exception $ex) {
            Log::error("Error downloading file: {$request->query('path')}", ['error' => $ex->getMessage()]);
            return response()->json(['error' => 'Failed to download file'], 500);
        }
    }
}
