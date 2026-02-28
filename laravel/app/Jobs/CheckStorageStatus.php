<?php

namespace App\Jobs;

use App\Models\StorageDrive;
use Illuminate\Contracts\Queue\ShouldQueue;
use Illuminate\Foundation\Queue\Queueable;

class CheckStorageStatus implements ShouldQueue
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
        StorageDrive::get()->each(function(StorageDrive $drive) {
            $freeSpace = disk_free_space($drive->root_path);
            $totalSpace = disk_total_space($drive->root_path);
            $usedSpace = $totalSpace - $freeSpace;

            $drive->update([
                'free_space' => $freeSpace,
                'used_space' => $usedSpace,
                'total_space' => $totalSpace,
                'last_checked_at' => now(),
            ]);
        });
    }
}
