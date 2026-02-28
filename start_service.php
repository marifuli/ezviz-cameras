<?php
// start_service.php

// The command to run your .NET service
$command = 'dotnet run --project /path/to/your/service 2>&1';

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
$pid = trim($pidLine);

// Save the PID to a file
file_put_contents('/tmp/myservice.pid', $pid);
echo "Service started with PID: " . $pid . "\n";

// Now read the actual output line by line
while (!feof($handle)) {
    $line = fgets($handle);
    if ($line !== false) {
        echo "Received: " . $line;
        ob_flush();
        flush();
    }
}

// Clean up PID file when process ends naturally
if (file_exists('/tmp/myservice.pid')) {
    unlink('/tmp/myservice.pid');
}

pclose($handle);
?>