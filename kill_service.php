<?php
// kill_service.php

$pidFile = '/tmp/myservice.pid';

if (!file_exists($pidFile)) {
    die("No running service found (PID file missing)\n");
}

$pid = trim(file_get_contents($pidFile));

// Check if process is actually running
if (posix_kill($pid, 0)) {
    // Send SIGTERM (15) for graceful shutdown
    if (posix_kill($pid, 15)) {
        echo "Service with PID {$pid} terminated gracefully.\n";
        
        // Wait a moment and force kill if still running
        sleep(2);
        if (posix_kill($pid, 0)) {
            echo "Process still running, sending SIGKILL...\n";
            posix_kill($pid, 9);
        }
        
        // Remove PID file
        unlink($pidFile);
    } else {
        echo "Failed to kill process {$pid}\n";
    }
} else {
    echo "Process {$pid} is not running. Cleaning up PID file.\n";
    unlink($pidFile);
}
?>