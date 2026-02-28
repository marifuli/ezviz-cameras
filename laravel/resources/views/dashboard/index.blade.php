@extends('layouts.app')

@section('title', 'Dashboard')

@section('content')
<div class="p-6 space-y-2 bg-gray-50 min-h-screen">
    <!-- PAGE TITLE -->
    <h1 class="text-2xl font-semibold text-gray-800">
        Dashboard
    </h1>

    <!-- ===================== -->
    <!-- SUMMARY STAT CARDS -->
    <!-- ===================== -->
    <div class="grid grid-cols-1 sm:grid-cols-3 md:grid-cols-4 xl:grid-cols-5 gap-4">
        <!-- Total Cameras -->
        <div class="bg-white rounded-xl shadow-sm p-4 flex justify-between items-center">
            <div>
                <p class="text-xs uppercase tracking-wide text-gray-500">Total Cameras</p>
                <p class="text-2xl font-bold text-gray-800">{{ $total_cameras }}</p>
            </div>
            <div class="w-10 h-10 rounded-lg bg-blue-100 text-blue-600 flex items-center justify-center">
                <i class="fas fa-camera"></i>
            </div>
        </div>

        <!-- Online -->
        <div class="bg-white rounded-xl shadow-sm p-4 flex justify-between items-center">
            <div>
                <p class="text-xs uppercase tracking-wide text-gray-500">Online</p>
                <p class="text-2xl font-bold text-gray-800">{{ $online_cameras }}</p>
            </div>
            <div class="w-10 h-10 rounded-lg bg-green-100 text-green-600 flex items-center justify-center">
                <i class="fas fa-check-circle"></i>
            </div>
        </div>

        <!-- Offline -->
        <div class="bg-white rounded-xl shadow-sm p-4 flex justify-between items-center">
            <div>
                <p class="text-xs uppercase tracking-wide text-gray-500">Offline</p>
                <p class="text-2xl font-bold text-gray-800">{{ $offline_cameras }}</p>
            </div>
            <div class="w-10 h-10 rounded-lg bg-red-100 text-red-600 flex items-center justify-center">
                <i class="fas fa-times-circle"></i>
            </div>
        </div>

        <!-- Active Downloads -->
        <div class="bg-white rounded-xl shadow-sm p-4 flex justify-between items-center">
            <div>
                <p class="text-xs uppercase tracking-wide text-gray-500">Active Downloads</p>
                <p class="text-2xl font-bold text-gray-800">{{ $active_download_jobs }}</p>
            </div>
            <div class="w-10 h-10 rounded-lg bg-cyan-100 text-cyan-600 flex items-center justify-center">
                <i class="fas fa-download"></i>
            </div>
        </div>

        <!-- Failed Downloads -->
        <div class="bg-white rounded-xl shadow-sm p-4 flex justify-between items-center">
            <div>
                <p class="text-xs uppercase tracking-wide text-gray-500">Failed</p>
                <p class="text-2xl font-bold text-gray-800">{{ $failed_download_jobs }}</p>
            </div>
            <div class="w-10 h-10 rounded-lg bg-yellow-100 text-yellow-600 flex items-center justify-center">
                <i class="fas fa-exclamation-triangle"></i>
            </div>
        </div>

        <!-- Actions -->
        <div class="bg-white rounded-xl shadow-sm p-4">
            <p class="text-xs uppercase tracking-wide text-gray-500 mb-2">Actions</p>
            <button id="checkAllCameras"
                    class="w-full flex items-center justify-center gap-2 px-3 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg">
                <i class="fas fa-sync"></i> Check All
            </button>
        </div>
    </div>

    <!-- ===================== -->
    <!-- OFFLINE + STORAGE -->
    <!-- ===================== -->
    <div class="grid grid-cols-1 xl:grid-cols-3 gap-6">
        <!-- OFFLINE CAMERAS -->
        <div class="bg-white rounded-xl shadow-sm">
            <div class="px-5 py-4 border-b flex justify-between items-center">
                <h2 class="font-semibold text-gray-800">Offline Cameras</h2>
                <span class="px-2 py-1 text-xs rounded-full bg-red-100 text-red-600">
                    {{ $offline_cameras }}
                </span>
            </div>

            <div class="p-4">
                @if (count($offline_cameras_list) > 0)
                    <table class="w-full text-sm">
                        <thead class="text-gray-500 border-b">
                            <tr>
                                <th class="py-2 text-left">Camera</th>
                                <th class="py-2 text-left">Last Online</th>
                                <th class="py-2 text-right">Action</th>
                            </tr>
                        </thead>
                        <tbody class="divide-y">
                            @foreach ($offline_cameras_list as $camera)
                                <tr>
                                    <td class="py-2">{{ $camera['name'] }}</td>
                                    <td class="py-2">
                                        {{ $camera['last_online_at'] ? \Carbon\Carbon::parse($camera['last_online_at'])->format('Y-m-d H:i') : 'Never' }}
                                    </td>
                                    <td class="py-2 text-right">
                                        <button data-id="{{ $camera['id'] }}"
                                                class="check-camera px-3 py-1 text-xs bg-blue-600 hover:bg-blue-700 text-white rounded">
                                            <i class="fas fa-sync"></i>
                                        </button>
                                    </td>
                                </tr>
                            @endforeach
                        </tbody>
                    </table>
                @else
                    <p class="text-center text-green-600">All cameras are online 🎉</p>
                @endif
            </div>
        </div>

        <!-- STORAGE -->
        <div class="xl:col-span-2 bg-white rounded-xl shadow-sm">
            <div class="px-5 py-4 border-b flex justify-between items-center">
                <h2 class="font-semibold text-gray-800">Storage Usage</h2>
                <button id="checkStorage"
                        class="px-3 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg">
                    <i class="fas fa-sync"></i> Check
                </button>
            </div>

            <div class="p-4 space-y-4">
                @foreach ($storage_drives as $drive)
                    <div class="border rounded-lg p-3">
                        <div class="flex justify-between text-sm font-medium mb-2">
                            <span>{{ $drive['name'] }} ({{ $drive['root_path'] }})</span>
                            <span>{{ number_format($drive['usage_percentage'], 1) }}%</span>
                        </div>

                        <div class="w-full h-2 bg-gray-200 rounded">
                            <div class="h-2 rounded bg-blue-600"
                                 style="width:{{ $drive['usage_percentage'] }}%"></div>
                        </div>

                        <div class="mt-2 text-xs text-gray-500 flex justify-between">
                            <span>Used: {{ App\Helpers\FormatHelper::formatBytes($drive['used_space']) }}</span>
                            <span>Free: {{ App\Helpers\FormatHelper::formatBytes($drive['free_space']) }}</span>
                            <span>Total: {{ App\Helpers\FormatHelper::formatBytes($drive['total_space']) }}</span>
                        </div>
                    </div>
                @endforeach
            </div>
        </div>
    </div>

    <!-- ===================== -->
    <!-- ACTIVE DOWNLOADS -->
    <!-- ===================== -->
    <div class="bg-white rounded-xl shadow-sm">
        <div class="px-5 py-4 border-b">
            <h2 class="font-semibold text-gray-800">Active Downloads</h2>
        </div>
        <div class="p-4" id="activeDownloads" style="overflow: auto; height: 250px;">
            <p class="text-center text-gray-500">Loading active downloads...</p>
        </div>
    </div>

    <!-- ===================== -->
    <!-- CHARTS -->
    <!-- ===================== -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div class="bg-white rounded-xl shadow-sm p-4">
            <h3 class="font-semibold mb-2">Downloads Per Day</h3>
            <div class="h-72">
                <canvas id="downloadsPerDayChart"></canvas>
            </div>
        </div>

        <div class="bg-white rounded-xl shadow-sm p-4">
            <h3 class="font-semibold mb-2">Downloads Per Camera</h3>
            <div class="h-72">
                <canvas id="downloadsPerCameraChart"></canvas>
            </div>
        </div>
    </div>
</div>
@endsection

@push('scripts')
<!-- Chart.js -->
<script src="https://cdn.jsdelivr.net/npm/chart.js@3.7.1/dist/chart.min.js"></script>

<script>
    $(document).ready(function() {
        // Check camera connection
        $('.check-camera').click(function() {
            const cameraId = $(this).data('id');
            const button = $(this);

            button.prop('disabled', true);
            button.html('<i class="fas fa-spinner fa-spin"></i>');

            $.ajax({
                url: `/api/cameras/${cameraId}/check`,
                type: 'POST',
                success: function(result) {
                    if (result.isOnline) {
                        toastr.success('Camera is online!');
                        setTimeout(function() {
                            location.reload();
                        }, 1500);
                    } else {
                        toastr.error('Camera is offline.');
                        button.prop('disabled', false);
                        button.html('<i class="fas fa-sync"></i>');
                    }
                },
                error: function() {
                    toastr.error('Failed to check camera connection.');
                    button.prop('disabled', false);
                    button.html('<i class="fas fa-sync"></i>');
                }
            });
        });

        // Check all cameras
        $('#checkAllCameras').click(function() {
            const button = $(this);

            button.prop('disabled', true);
            button.html('<i class="fas fa-spinner fa-spin"></i> Checking...');

            $.ajax({
                url: '/api/cameras/check-all',
                type: 'POST',
                success: function() {
                    toastr.success('Camera health check triggered.');
                    setTimeout(function() {
                        location.reload();
                    }, 3000);
                },
                error: function() {
                    toastr.error('Failed to trigger camera health check.');
                    button.prop('disabled', false);
                    button.html('<i class="fas fa-sync"></i> Check All');
                }
            });
        });

        // Check storage
        $('#checkStorage').click(function() {
            const button = $(this);

            button.prop('disabled', true);
            button.html('<i class="fas fa-spinner fa-spin"></i> Checking...');

            $.ajax({
                url: '/api/storage-drives/check',
                type: 'POST',
                success: function() {
                    toastr.success('Storage check triggered.');
                    setTimeout(function() {
                        location.reload();
                    }, 3000);
                },
                error: function() {
                    toastr.error('Failed to trigger storage check.');
                    button.prop('disabled', false);
                    button.html('<i class="fas fa-sync"></i> Check');
                }
            });
        });

        // Load active downloads
        function loadActiveDownloads() {
            $.ajax({
                url: '/api/download-jobs/active',
                type: 'GET',
                success: function(jobs) {
                    if (jobs.length === 0) {
                        $('#activeDownloads').html('<p class="text-center">No active downloads.</p>');
                        return;
                    }
                    let html = '<div class="overflow-x-auto">';
                    html += '<table class="min-w-full divide-y divide-gray-200 border border-gray-200 text-sm">';
                    html += '<thead class="bg-gray-100">';
                    html += '<tr>';
                    html += '<th class="px-4 py-2 text-left font-semibold text-gray-700">Camera</th>';
                    html += '<th class="px-4 py-2 text-left font-semibold text-gray-700">File</th>';
                    html += '<th class="px-4 py-2 text-left font-semibold text-gray-700">Progress</th>';
                    html += '<th class="px-4 py-2 text-left font-semibold text-gray-700">Started</th>';
                    html += '</tr>';
                    html += '</thead>';
                    html += '<tbody class="bg-white divide-y divide-gray-200">';

                    jobs.forEach(function(job) {
                        html += '<tr class="hover:bg-gray-50">';
                        html += `<td class="px-4 py-2">${job.camera_name}</td>`;
                        html += `<td class="px-4 py-2">${job.file_name || 'Unknown'}</td>`;

                        html += '<td class="px-4 py-2">';
                        html += '<div class="w-full bg-gray-200 rounded-full h-4">';
                        html += `<div class="bg-blue-600 h-4 rounded-full text-xs text-white flex items-center justify-center transition-all duration-300"
                                style="width: ${job.progress}%">`;
                        html += `${job.progress}%`;
                        html += '</div>';
                        html += '</div>';
                        html += '</td>';

                        const startTime = job.start_time ? new Date(job.start_time).toLocaleString() : 'N/A';
                        html += `<td class="px-4 py-2">${startTime}</td>`;
                        html += '</tr>';
                    });

                    html += '</tbody></table></div>';
                    $('#activeDownloads').html(html);
                },
                error: function() {
                    $('#activeDownloads').html('<p class="text-center text-red-500">Failed to load active downloads.</p>');
                }
            });
        }

        // Initial load
        loadActiveDownloads();

        // Refresh active downloads every 15 seconds
        setInterval(loadActiveDownloads, 15000);

        // Initialize charts
        initializeCharts();
    });

    function initializeCharts() {
        // Downloads Per Day Chart
        const downloadsPerDayCtx = document.getElementById('downloadsPerDayChart').getContext('2d');

        const downloadsPerDayData = {!! json_encode($downloads_per_day) !!};

        const labels = downloadsPerDayData.map(item => {
            const date = new Date(item.label);
            return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
        });

        const values = downloadsPerDayData.map(item => item.value);

        new Chart(downloadsPerDayCtx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Downloads',
                    data: values,
                    backgroundColor: 'rgba(78, 115, 223, 0.8)',
                    borderColor: 'rgba(78, 115, 223, 1)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            precision: 0
                        }
                    }
                }
            }
        });

        // Downloads Per Camera Chart
        const downloadsPerCameraCtx = document.getElementById('downloadsPerCameraChart').getContext('2d');

        const downloadsPerCameraData = {!! json_encode($downloads_per_camera) !!};

        const cameraLabels = Object.keys(downloadsPerCameraData);
        const cameraValues = Object.values(downloadsPerCameraData);

        // Generate colors
        const backgroundColors = cameraLabels.map((_, i) => {
            const hue = (i * 137) % 360;
            return `hsla(${hue}, 70%, 60%, 0.8)`;
        });

        new Chart(downloadsPerCameraCtx, {
            type: 'doughnut',
            data: {
                labels: cameraLabels,
                datasets: [{
                    data: cameraValues,
                    backgroundColor: backgroundColors,
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'right',
                        labels: {
                            boxWidth: 12
                        }
                    }
                }
            }
        });
    }
</script>
@endpush
