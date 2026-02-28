<?php

namespace App\Http\Controllers;

use App\Services\HikvisionService;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Log;
use App\Jobs\CheckCameraStatus;

class HomeController extends Controller
{
    protected $hikvisionService;

    public function __construct(HikvisionService $hikvisionService)
    {
        $this->hikvisionService = $hikvisionService;
    }

    public function index(Request $request)
    {
        CheckCameraStatus::dispatch();
        try {
            $dashboardData = $this->hikvisionService->getDashboardData();
            return view('dashboard.index', $dashboardData);
        } catch (\Exception $ex) {
            Log::error('Error loading dashboard data', ['error' => $ex->getMessage()]);
            return view('dashboard.index', [
                'total_cameras' => 0,
                'online_cameras' => 0,
                'offline_cameras' => 0,
                'active_download_jobs' => 0,
                'failed_download_jobs' => 0,
                'completed_download_jobs' => 0,
                'offline_cameras_list' => collect(),
                'storage_drives' => collect(),
                'downloads_per_day' => collect(),
                'downloads_per_camera' => [],
                'active_workers' => 0,
                'cameras' => collect(),
                'worker_status' => collect(),
            ]);
        }
    }
}
