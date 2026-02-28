<?php
// status_service.php

$pidFile = '/tmp/myservice.pid';

if (!file_exists($pidFile)) {
    die("Service is not running\n");
}

$pid = trim(file_get_contents($pidFile));

if (posix_kill($pid, 0)) {
    echo "Service is running with PID: {$pid}\n";
    
    // Optional: Get more process info
    $output = shell_exec("ps -p {$pid} -o pid,ppid,cmd,etime");
    echo $output;
} else {
    echo "PID file exists but process {$pid} is not running. Cleaning up.\n";
    unlink($pidFile);
}
?>