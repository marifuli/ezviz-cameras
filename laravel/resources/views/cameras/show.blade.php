@extends('layouts.app')

@section('title', 'Camera Details')

@section('content')
<div class="space-y-6">
    <div class="flex justify-between items-center">
        <div>
            <h1 class="text-2xl font-semibold text-gray-800">{{ $camera->name }}</h1>
            <p class="text-gray-600">Camera ID: {{ $camera->id }}</p>
        </div>
        <div class="flex space-x-3">
            <a href="{{ route('cameras.edit', $camera) }}" class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700">
                <i class="fas fa-edit mr-2"></i>
                Edit Camera
            </a>
            <a href="{{ route('cameras.test-connection', $camera) }}" class="inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50">
                <i class="fas fa-signal mr-2"></i>
                Test Connection
            </a>
        </div>
    </div>

    @if(session('success'))
        <div class="rounded-md bg-green-50 p-4">
            <div class="flex">
                <div class="flex-shrink-0">
                    <i class="fas fa-check-circle text-green-400"></i>
                </div>
                <div class="ml-3">
                    <p class="text-sm font-medium text-green-800">
                        {!! session('success') !!}
                    </p>
                </div>
            </div>
        </div>
    @endif

    @if(session('error'))
        <div class="rounded-md bg-red-50 p-4">
            <div class="flex">
                <div class="flex-shrink-0">
                    <i class="fas fa-exclamation-circle text-red-400"></i>
                </div>
                <div class="ml-3">
                    <p class="text-sm font-medium text-red-800">
                        {{ session('error') }}
                    </p>
                </div>
            </div>
        </div>
    @endif

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Camera Information -->
        <div class="lg:col-span-2 bg-white rounded-lg shadow p-6">
            <h2 class="text-lg font-medium text-gray-900 mb-4">Camera Information</h2>
            <dl class="grid grid-cols-1 gap-x-4 gap-y-6 sm:grid-cols-2">
                <div>
                    <dt class="text-sm font-medium text-gray-500">Store</dt>
                    <dd class="mt-1 text-sm text-gray-900">{{ $camera->store->name ?? 'No Store Assigned' }}</dd>
                </div>
                <div>
                    <dt class="text-sm font-medium text-gray-500">Status</dt>
                    <dd class="mt-1">
                        @if($camera->is_online)
                            <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                                <i class="fas fa-circle text-green-400 mr-1"></i>
                                Online
                            </span>
                        @else
                            <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800">
                                <i class="fas fa-circle text-red-400 mr-1"></i>
                                Offline
                            </span>
                        @endif
                    </dd>
                </div>
                <div>
                    <dt class="text-sm font-medium text-gray-500">IP Address</dt>
                    <dd class="mt-1 text-sm text-gray-900">{{ $camera->ip_address }}</dd>
                </div>
                <div>
                    <dt class="text-sm font-medium text-gray-500">Port</dt>
                    <dd class="mt-1 text-sm text-gray-900">{{ $camera->port }}</dd>
                </div>
                <div>
                    <dt class="text-sm font-medium text-gray-500">Username</dt>
                    <dd class="mt-1 text-sm text-gray-900">{{ $camera->username ?: 'Not set' }}</dd>
                </div>
                <div>
                    <dt class="text-sm font-medium text-gray-500">Server Port</dt>
                    <dd class="mt-1 text-sm text-gray-900">{{ $camera->server_port ?: 'Not set' }}</dd>
                </div>
                <div>
                    <dt class="text-sm font-medium text-gray-500">Last Online</dt>
                    <dd class="mt-1 text-sm text-gray-900">
                        {{ $camera->last_online_at ? $camera->last_online_at->format('Y-m-d H:i:s') : 'Never' }}
                    </dd>
                </div>
                <div>
                    <dt class="text-sm font-medium text-gray-500">Last Downloaded</dt>
                    <dd class="mt-1 text-sm text-gray-900">
                        {{ $camera->last_downloaded_at ? $camera->last_downloaded_at->format('Y-m-d H:i:s') : 'Never' }}
                    </dd>
                </div>
            </dl>

            @if($camera->last_error)
                <div class="mt-6 pt-6 border-t border-gray-200">
                    <dt class="text-sm font-medium text-gray-500">Last Error</dt>
                    <dd class="mt-1 text-sm text-red-600">{{ $camera->last_error }}</dd>
                </div>
            @endif
        </div>

        <!-- Quick Actions -->
        <div class="space-y-6">
            <div class="bg-white rounded-lg shadow p-6">
                <h2 class="text-lg font-medium text-gray-900 mb-4">Quick Actions</h2>
                <div class="space-y-3">
                    <a href="{{ route('cameras.test-connection', $camera) }}" class="w-full inline-flex justify-center items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50">
                        <i class="fas fa-signal mr-2"></i>
                        Test Connection
                    </a>
                    <button onclick="checkCamera({{ $camera->id }})" class="w-full inline-flex justify-center items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700">
                        <i class="fas fa-sync mr-2"></i>
                        Check Status
                    </button>
                    <a href="{{ route('cameras.edit', $camera) }}" class="w-full inline-flex justify-center items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-yellow-600 hover:bg-yellow-700">
                        <i class="fas fa-edit mr-2"></i>
                        Edit Camera
                    </a>
                </div>
            </div>

            <!-- Recent Download Jobs -->
            <div class="bg-white rounded-lg shadow p-6">
                <h2 class="text-lg font-medium text-gray-900 mb-4">Recent Download Jobs</h2>
                <div class="space-y-3">
                    @forelse($camera->fileDownloadJobs()->latest()->take(5)->get() as $job)
                        <div class="flex items-center justify-between p-3 border border-gray-200 rounded-md">
                            <div>
                                <div class="text-sm font-medium text-gray-900">{{ $job->file_name ?: 'Unknown file' }}</div>
                                <div class="text-xs text-gray-500">{{ $job->created_at->format('M j, H:i') }}</div>
                            </div>
                            <div>
                                @if($job->status === 'completed')
                                    <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
                                        <i class="fas fa-check mr-1"></i> Completed
                                    </span>
                                @elseif($job->status === 'downloading')
                                    <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                                        <i class="fas fa-download mr-1"></i> {{ $job->progress }}%
                                    </span>
                                @elseif($job->status === 'failed')
                                    <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-red-100 text-red-800">
                                        <i class="fas fa-times mr-1"></i> Failed
                                    </span>
                                @else
                                    <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
                                        {{ $job->status }}
                                    </span>
                                @endif
                            </div>
                        </div>
                    @empty
                        <p class="text-sm text-gray-500 text-center py-4">No download jobs found</p>
                    @endforelse
                </div>
                @if($camera->fileDownloadJobs->count() > 5)
                    <div class="mt-4 text-center">
                        <a href="{{ route('jobs.index') }}?camera={{ $camera->id }}" class="text-sm text-blue-600 hover:text-blue-800">
                            View all jobs
                        </a>
                    </div>
                @endif
            </div>
        </div>
    </div>

    <div class="flex justify-between">
        <a href="{{ route('cameras.index') }}" class="inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50">
            <i class="fas fa-arrow-left mr-2"></i>
            Back to Cameras
        </a>
        <form method="POST" action="{{ route('cameras.destroy', $camera) }}" class="inline" onsubmit="return confirm('Are you sure you want to delete this camera? This will also delete all associated download jobs.')">
            @csrf
            @method('DELETE')
            <button type="submit" class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-red-600 hover:bg-red-700">
                <i class="fas fa-trash mr-2"></i>
                Delete Camera
            </button>
        </form>
    </div>
</div>
@endsection

@push('scripts')
<script>
function checkCamera(id) {
    const button = event.target;
    const originalText = button.innerHTML;

    button.disabled = true;
    button.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Checking...';

    $.ajax({
        url: `/api/cameras/${id}/check`,
        type: 'POST',
        success: function(result) {
            if (result.isOnline) {
                toastr.success('Camera is online!');
                setTimeout(() => location.reload(), 1500);
            } else {
                toastr.error('Camera is offline.');
            }
        },
        error: function() {
            toastr.error('Failed to check camera connection.');
        },
        complete: function() {
            button.disabled = false;
            button.innerHTML = originalText;
        }
    });
}
</script>
@endpush
