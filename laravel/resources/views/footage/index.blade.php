@extends('layouts.app')

@section('title', 'Footage Archive')

@section('content')
<div class="space-y-6">
    <div class="flex justify-between items-center">
        <h1 class="text-2xl font-semibold text-gray-800">Footage Archive</h1>
    </div>

    <!-- Search Form -->
    <div class="bg-white rounded-lg shadow p-6">
        <h2 class="text-lg font-medium text-gray-900 mb-4">Search Footage</h2>
        <form method="GET" class="space-y-4">
            <input type="hidden" name="search" value="1">

            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                <div>
                    <label for="camera_id" class="block text-sm font-medium text-gray-700">Camera</label>
                    <select name="camera_id" id="camera_id" class="mt-1 block w-full border border-gray-300 rounded-md shadow-sm px-3 py-2 focus:outline-none focus:ring-blue-500 focus:border-blue-500">
                        <option value="">All Cameras</option>
                        @foreach($cameras as $camera)
                            <option value="{{ $camera->id }}" {{ $cameraId == $camera->id ? 'selected' : '' }}>
                                {{ $camera->name }}
                            </option>
                        @endforeach
                    </select>
                </div>

                <div>
                    <label for="start_date" class="block text-sm font-medium text-gray-700">Start Date</label>
                    <input type="date" id="start_date" name="start_date" value="{{ $startDate }}"
                           class="mt-1 block w-full border border-gray-300 rounded-md shadow-sm px-3 py-2 focus:outline-none focus:ring-blue-500 focus:border-blue-500">
                </div>

                <div>
                    <label for="end_date" class="block text-sm font-medium text-gray-700">End Date</label>
                    <input type="date" id="end_date" name="end_date" value="{{ $endDate }}"
                           class="mt-1 block w-full border border-gray-300 rounded-md shadow-sm px-3 py-2 focus:outline-none focus:ring-blue-500 focus:border-blue-500">
                </div>

                <div>
                    <label for="file_type" class="block text-sm font-medium text-gray-700">File Type</label>
                    <select name="file_type" id="file_type" class="mt-1 block w-full border border-gray-300 rounded-md shadow-sm px-3 py-2 focus:outline-none focus:ring-blue-500 focus:border-blue-500">
                        <option value="both" {{ $fileType == 'both' ? 'selected' : '' }}>Videos & Photos</option>
                        <option value="video" {{ $fileType == 'video' ? 'selected' : '' }}>Videos Only</option>
                        <option value="photo" {{ $fileType == 'photo' ? 'selected' : '' }}>Photos Only</option>
                    </select>
                </div>
            </div>

            <div class="flex justify-end">
                <button type="submit" class="inline-flex items-center px-6 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700">
                    <i class="fas fa-search mr-2"></i>
                    Search Footage
                </button>
            </div>
        </form>
    </div>

    @if(count($footageFiles) > 0)
        <!-- Results -->
        <div class="bg-white rounded-lg shadow overflow-hidden">
            <div class="px-6 py-4 border-b border-gray-200">
                <h3 class="text-lg font-medium text-gray-900">Found {{ count($footageFiles) }} files</h3>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4 p-6">
                @foreach($footageFiles as $file)
                    <div class="border border-gray-200 rounded-lg overflow-hidden hover:shadow-md transition-shadow">
                        <div class="aspect-video bg-gray-100 flex items-center justify-center">
                            @if($file['file_type'] === 'video')
                                <i class="fas fa-play-circle text-blue-600 text-4xl"></i>
                            @else
                                <i class="fas fa-image text-green-600 text-4xl"></i>
                            @endif
                        </div>

                        <div class="p-4">
                            <h4 class="text-sm font-medium text-gray-900 truncate">{{ $file['file_name'] ?? 'Unknown File' }}</h4>
                            <p class="text-xs text-gray-500 mt-1">{{ $file['camera_name'] }}</p>

                            @if(isset($file['file_start_time']))
                                <p class="text-xs text-gray-500">{{ \Carbon\Carbon::parse($file['file_start_time'])->format('Y-m-d H:i') }}</p>
                            @endif

                            @if(isset($file['file_size']))
                                <p class="text-xs text-gray-500">{{ App\Helpers\FormatHelper::formatBytes($file['file_size']) }}</p>
                            @endif

                            <div class="mt-3 flex justify-between items-center">
                                <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium {{ $file['file_type'] === 'video' ? 'bg-blue-100 text-blue-800' : 'bg-green-100 text-green-800' }}">
                                    {{ ucfirst($file['file_type']) }}
                                </span>

                                @if(isset($file['download_url']))
                                    <a href="{{ $file['download_url'] }}" class="text-blue-600 hover:text-blue-800 text-sm">
                                        <i class="fas fa-download"></i>
                                    </a>
                                @endif
                            </div>
                        </div>
                    </div>
                @endforeach
            </div>
        </div>
    @elseif(request()->has('search'))
        <div class="bg-white rounded-lg shadow p-12 text-center">
            <i class="fas fa-film text-gray-400 text-6xl mb-4"></i>
            <h3 class="text-lg font-medium text-gray-900 mb-2">No footage found</h3>
            <p class="text-gray-500">Try adjusting your search criteria or check if the camera has recorded footage during this time period.</p>
        </div>
    @else
        <div class="bg-white rounded-lg shadow p-12 text-center">
            <i class="fas fa-search text-gray-400 text-6xl mb-4"></i>
            <h3 class="text-lg font-medium text-gray-900 mb-2">Search for Footage</h3>
            <p class="text-gray-500">Use the search form above to find recorded footage from your cameras.</p>
        </div>
    @endif
</div>
@endsection
