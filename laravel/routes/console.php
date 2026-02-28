<?php

use App\Jobs\CheckCameraFootageStatus;
use App\Jobs\CheckCameraStatus;
use App\Jobs\CheckStorageStatus;
use App\Models\Camera;
use Illuminate\Support\Facades\Artisan;
use Illuminate\Support\Facades\Schedule;

Schedule::job(CheckCameraStatus::class)->everyFifteenMinutes();
Schedule::job(CheckStorageStatus::class)->hourly();
Schedule::job(CheckCameraFootageStatus::class)->everyTenMinutes();


Artisan::command('generate_supervisor_configs', function () {
    foreach (Camera::get() as $camera) {
        $config = "[program:camera-{$camera->id}]
command=/usr/bin/php /home/ariful/ezviz-cameras/laravel/artisan camera:work {$camera->id}
directory=/home/ariful/ezviz-cameras/laravel
user=root
autostart=true
autorestart=true
startretries=3
stopwaitsecs=30
stopsignal=TERM
redirect_stderr=true
stdout_logfile=/home/ariful/ezviz-cameras/laravel/storage/logs/camera-{$camera->id}.log
stdout_logfile_maxbytes=50MB
stdout_logfile_backups=5
    ";

        file_put_contents("/etc/supervisor/conf.d/camera-{$camera->id}.conf", $config);
    }
    dump(shell_exec("service supervisor restart"));
});
