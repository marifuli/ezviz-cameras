<?php
// app/Console/Commands/CameraWorker.php

namespace App\Console\Commands;

use App\Models\FileDownloadJob;
use Illuminate\Console\Command;
use Illuminate\Support\Facades\Log;

class CameraWorker extends Command
{
    protected $signature = 'camera:work {camera-id}';
    protected $description = 'Dedicated worker for a specific camera';

    private $cameraId;
    private $running = true;
    private $ezviz_path = '';
    private $pids = [];

    public function handle()
    {
        $this->ezviz_path = config('app.ezviz-console');
        $this->cameraId = $this->argument('camera-id');

        $this->info("Starting dedicated worker for Camera {$this->cameraId}");

        // Handle shutdown signals gracefully
        pcntl_signal(SIGTERM, function () {
            $this->running = false;
        });
        pcntl_signal(SIGINT, function () {
            $this->running = false;
        });

        while ($this->running) {
            try {
                // last job
                $job = FileDownloadJob::where('camera_id', $this->cameraId)
                    ->whereHas('camera', fn($q) => $q->where('is_online', true))
                    ->whereIn('status', ['failed', 'pending', 'downloading'])
                    ->latest()
                    ->first();
                // dd("Job found " . $job);
                if($job) $this->processDownload($job);
            } catch (\Exception $e) {
                $this->error("Error: " . $e->getMessage());
            }

            // Check for signals
            pcntl_signal_dispatch();

            // Wait before next iteration
            sleep(30);
        }

        FileDownloadJob::where('camera_id', $this->cameraId)
            ->where('status', 'downloading')
            ->update(['status' => 'failed']);
        // stop all processes
        foreach ($this->pids as $pid) {
            posix_kill($pid, SIGTERM);
        }
        $this->info("Camera {$this->cameraId} worker stopped");
        return 0;
    }

    private function processDownload(FileDownloadJob $job)
    {
        $target = public_path($job->download_path);
        
        // Build command
        $command = config('app.ezviz-console') . ' download ' . 
            $job->camera->ip_address . ' ' . 
            $job->camera->port . ' ' . 
            $job->camera->username . ' ' . 
            $job->camera->password . ' ' . 
            $job->file_name . ' ' . 
            $target;
        
        $this->line("Executing: " . $command);
        
        // Run the command and stream output
        $process = popen($command, 'r');
        
        while (!feof($process)) {
            $line = fgets($process);
            if ($line) {
                echo $line; // This will show in your console
                flush();    // Force output
                
                // Update progress if needed
                if (preg_match('/(\d+)%/', $line, $matches)) {
                    $job->update([
                        'progress' => $matches[1],
                        'status' => 'downloading'
                    ]);
                } 
                if(str_contains($line, "Download failed")) {
                    $job->delete();
                    dump("Deleted", $line);
                }
            }
        }
        
        pclose($process);
        
        // Mark as complete
        $job->update(['status' => 'completed', 'progress' => 100]);
    }
}
