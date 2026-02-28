<?php

namespace App\Jobs;

use App\Models\Camera;
use App\Services\HikvisionService;
use Illuminate\Contracts\Queue\ShouldQueue;
use Illuminate\Foundation\Queue\Queueable;

class CheckCameraStatus implements ShouldQueue
{
    use Queueable;

    /**
     * Create a new job instance.
     */
    public function __construct()
    {
        //
    }

    /**
     * Execute the job.
     */
    public function handle(): void
    {
        $service = new HikvisionService();
        Camera::get()->each(function($cam) use($service) {
            $service->testCameraConnection($cam->id);
        });
    }
}
