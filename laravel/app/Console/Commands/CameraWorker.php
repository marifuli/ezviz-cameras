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
                    ->whereIn('status', ['failed', 'pending'])
                    ->first();
                dump("Job found " . $job->toJson());
                if($job) $this->processDownload($job);
            } catch (\Exception $e) {
                $this->error("Error: " . $e->getMessage());
            }

            // Check for signals
            pcntl_signal_dispatch();

            // Wait before next iteration
            sleep(30);
        }

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
        // The command to run your .NET service
        $command = config('app.ezviz-console');

        // Add 'echo $$' to get the PID of the shell process
        // We run it through 'sh -c' to get a proper PID
        $fullCommand = 'sh -c \'echo $$; exec ' . $command . '\'';

        // Open the process for reading
        $handle = popen($fullCommand, 'r');

        if ($handle === false) {
            die('Failed to start the process.');
        }

        // The first line will be the PID
        $pidLine = fgets($handle);
        $this->pids[] = trim($pidLine);

        // Now read the actual output line by line
        while (!feof($handle)) {
            $line = fgets($handle);
            if ($line !== false) {
                echo "Received: " . $line;
                ob_flush();
                flush();
            }
        }

        pclose($handle);
    }
}
