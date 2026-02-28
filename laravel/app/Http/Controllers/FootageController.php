<?php

namespace App\Http\Controllers;

use App\Models\Camera;
use App\Models\FileDownloadJob;
use App\Services\HikvisionService;
use Carbon\Carbon;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Log;

class FootageController extends Controller
{
    protected $hikvisionService;

    public function __construct(HikvisionService $hikvisionService)
    {
        $this->hikvisionService = $hikvisionService;
    }

    public function index(Request $request)
    {
        try {
            $cameras = Camera::orderBy('name')->get();

            // Default to last 7 days
            $startDate = $request->input('start_date', now()->subDays(7)->format('Y-m-d'));
            $endDate = $request->input('end_date', now()->format('Y-m-d'));
            $cameraId = $request->input('camera_id');
            $fileType = $request->input('file_type', 'both');

            $footageFiles = [];
            if ($request->has('search')) {
                $footageFiles = $this->hikvisionService->getFootageFiles(
                    $cameraId,
                    Carbon::parse($startDate),
                    Carbon::parse($endDate),
                    $fileType
                );
            }

            return view('footage.index', compact(
                'cameras',
                'footageFiles',
                'startDate',
                'endDate',
                'cameraId',
                'fileType'
            ));
        } catch (\Exception $ex) {
            Log::error('Error getting footage files', ['error' => $ex->getMessage()]);
            return redirect()->back()->with('error', 'An error occurred while retrieving footage files.');
        }
    }

    public function download(Request $request)
    {
        try {
            $filePath = $request->query('path');

            if (!$filePath) {
                return redirect()->back()->with('error', 'File path is required.');
            }

            $downloadUrl = $this->hikvisionService->getFootageDownloadUrl($filePath);

            if (!file_exists($downloadUrl)) {
                return redirect()->back()->with('error', 'File not found.');
            }

            // Determine content type
            $contentType = 'application/octet-stream';
            if (str_ends_with(strtolower($filePath), '.mp4')) {
                $contentType = 'video/mp4';
            } elseif (str_ends_with(strtolower($filePath), '.jpg') || str_ends_with(strtolower($filePath), '.jpeg')) {
                $contentType = 'image/jpeg';
            }

            return response()->file($downloadUrl, [
                'Content-Type' => $contentType,
                'Content-Disposition' => 'attachment; filename="' . basename($filePath) . '"',
            ]);
        } catch (\Exception $ex) {
            Log::error("Error downloading footage file: {$request->query('path')}", ['error' => $ex->getMessage()]);
            return redirect()->back()->with('error', 'Failed to download file.');
        }
    }
}
