<?php

namespace App\Jobs;

use Illuminate\Contracts\Queue\ShouldQueue;
use Illuminate\Foundation\Queue\Queueable;
use App\Models\Camera;
use App\Models\FileDownloadJob;
use Carbon\Carbon;

class CheckCameraFootageStatus implements ShouldQueue
{
    use Queueable;

    /**
     * Create a new job instance.
     */
    public function __construct(public ?int $cameraId = null)
    {
        //
    }

    /**
     * Execute the job.
     */
    public function handle(): void
    {
        if($this->cameraId) {
            try {
                $this->checkCamera();
            } catch (\Throwable $th) {
                // throw $th;
            }
            return;
        }
        Camera::where('is_online', true)
            ->get(['id'])
            ->each(function ($camera) {
                self::dispatch($camera->id);
            });
    }
    function checkCamera() 
    {
        $camera = Camera::find($this->cameraId);
        $json_path = storage_path('app/private/list-' . $this->cameraId . '.json');
        try {
            unlink($json_path);
        } catch (\Throwable $th) {}
        $from = now()->subDays(14);
        $to = now();
        if($camera->last_downloaded_at) {
            $from = $camera->last_downloaded_at->subMinutes(10);
        }
        // Build command
        $command = config('app.ezviz-console') . ' list ' . 
            $camera->ip_address . ' ' . 
            $camera->port . ' ' . 
            $camera->username . ' ' . 
            $camera->password . ' "' . 
            $from->format('Y-m-d H:i:s') . '" "' . 
            $to->format('Y-m-d H:i:s') . '" ' . 
            $json_path;
        exec($command);
        $data = json_decode(file_get_contents($json_path));
        $maxTime = null;
        foreach($data as $file) {
            $maxTime = max($maxTime, $file->EndTime);
            $start = Carbon::parse($file->StartTime)->format('Y-m-d H:i:s');
            $end = Carbon::parse($file->EndTime)->format('Y-m-d H:i:s');
            $check = FileDownloadJob::where('file_start_time', $start)
                ->where('file_end_time', $end)
                ->first();
            if($check) {
                if($check->status == 'completed' && $check->file_size != $file->Size) {
                    $camera->update([
                        'status' => 'pending',
                        'file_size' => $file->Size
                    ]);
                }
                FileDownloadJob::where('id', '!=', $check->id)
                    ->where('file_name', $file->Name)
                    ->delete();
                continue;
            }
            $saved = FileDownloadJob::create([
                'camera_id' => $camera->id,
                'status' => 'pending',
                'progress' => 0,
                'file_name' => $file->Name,
                'file_type' => 'video',
                'file_size' => $file->Size,
                'file_start_time' => $start,
                'file_end_time' => $end,
                'download_path' => 'downloads/' . $this->cameraId . '/' . $file->Name . '.mp4',
            ]);
            FileDownloadJob::where('id', '!=', $saved->id)
                ->where('file_name', $file->Name)
                ->delete();
        }
        unlink($json_path);
    }
}
