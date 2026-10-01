@extends('layouts.app')

@section('title', 'Store Details - ' . $store->name)

@section('content')
<div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
            <div class="flex items-center space-x-3">
                <div class="h-10 w-10 rounded-lg bg-green-100 flex items-center justify-center">
                    <i class="fas fa-store text-green-600 text-lg"></i>
                </div>
                <div>
                    <h1 class="text-2xl font-semibold text-gray-800">{{ $store->name }}</h1>
                    <p class="text-sm text-gray-500">Store ID: {{ $store->id }}</p>
                </div>
            </div>
        </div>
        <div class="flex flex-wrap items-center gap-2">
            <a href="{{ route('cameras.create', ['store_id' => $store->id]) }}" class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 shadow-sm">
                <i class="fas fa-plus mr-2"></i>
                Add Camera
            </a>
            <a href="{{ route('stores.edit', $store) }}" class="inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 shadow-sm">
                <i class="fas fa-edit mr-2"></i>
                Edit Store
            </a>
            <a href="{{ route('stores.index') }}" class="inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 shadow-sm">
                <i class="fas fa-arrow-left mr-2"></i>
                Back to Stores
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

    <!-- Statistics Overview -->
    @php
        $onlineCount = $store->cameras->where('is_online', true)->count();
        $offlineCount = $store->cameras->where('is_online', false)->count();
    @endphp
    <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div class="bg-white rounded-lg shadow-sm p-5 border border-gray-100 flex items-center justify-between">
            <div>
                <p class="text-xs uppercase tracking-wide text-gray-500 font-semibold">Total Cameras</p>
                <p class="text-2xl font-bold text-gray-800 mt-1">{{ $store->cameras->count() }}</p>
            </div>
            <div class="w-12 h-12 rounded-lg bg-blue-50 text-blue-600 flex items-center justify-center text-xl">
                <i class="fas fa-video"></i>
            </div>
        </div>

        <div class="bg-white rounded-lg shadow-sm p-5 border border-gray-100 flex items-center justify-between">
            <div>
                <p class="text-xs uppercase tracking-wide text-gray-500 font-semibold">Online Cameras</p>
                <p class="text-2xl font-bold text-green-600 mt-1">{{ $onlineCount }}</p>
            </div>
            <div class="w-12 h-12 rounded-lg bg-green-50 text-green-600 flex items-center justify-center text-xl">
                <i class="fas fa-check-circle"></i>
            </div>
        </div>

        <div class="bg-white rounded-lg shadow-sm p-5 border border-gray-100 flex items-center justify-between">
            <div>
                <p class="text-xs uppercase tracking-wide text-gray-500 font-semibold">Offline Cameras</p>
                <p class="text-2xl font-bold text-red-600 mt-1">{{ $offlineCount }}</p>
            </div>
            <div class="w-12 h-12 rounded-lg bg-red-50 text-red-600 flex items-center justify-center text-xl">
                <i class="fas fa-times-circle"></i>
            </div>
        </div>
    </div>
    <!-- Store Information -->
    <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h2 class="text-lg font-medium text-gray-900 mb-4 pb-2 border-b border-gray-200 flex items-center">
            <i class="fas fa-info-circle text-blue-600 mr-2"></i>
            Store Information
        </h2>
        <dl class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
            <div>
                <dt class="text-xs uppercase tracking-wider font-semibold text-gray-500">Store Name</dt>
                <dd class="mt-1 text-sm font-medium text-gray-900">{{ $store->name }}</dd>
            </div>
            <div>
                <dt class="text-xs uppercase tracking-wider font-semibold text-gray-500">Address</dt>
                <dd class="mt-1 text-sm text-gray-900">{{ $store->address ?: 'Not provided' }}</dd>
            </div>
            <div>
                <dt class="text-xs uppercase tracking-wider font-semibold text-gray-500">Phone</dt>
                <dd class="mt-1 text-sm text-gray-900">
                    @if($store->phone)
                        <a href="tel:{{ $store->phone }}" class="text-blue-600 hover:text-blue-800">
                            <i class="fas fa-phone-alt mr-1 text-gray-400"></i>{{ $store->phone }}
                        </a>
                    @else
                        <span class="text-gray-400">Not provided</span>
                    @endif
                </dd>
            </div>
            <div>
                <dt class="text-xs uppercase tracking-wider font-semibold text-gray-500">Email</dt>
                <dd class="mt-1 text-sm text-gray-900">
                    @if($store->email)
                        <a href="mailto:{{ $store->email }}" class="text-blue-600 hover:text-blue-800">
                            <i class="fas fa-envelope mr-1 text-gray-400"></i>{{ $store->email }}
                        </a>
                    @else
                        <span class="text-gray-400">Not provided</span>
                    @endif
                </dd>
            </div>
        </dl>
    </div>
    <!-- Cameras in Store -->
    <div class="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        <div class="p-6 border-b border-gray-200 flex justify-between items-center">
            <div>
                <h2 class="text-lg font-medium text-gray-900">Cameras in this Store</h2>
                <p class="text-sm text-gray-500">List of all cameras assigned to {{ $store->name }}</p>
            </div>
            <a href="{{ route('cameras.create', ['store_id' => $store->id]) }}" class="inline-flex items-center px-3 py-1.5 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700">
                <i class="fas fa-plus mr-1.5"></i> Add Camera
            </a>
        </div>
        <div class="overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-200">
                <thead class="bg-gray-50">
                    <tr>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Camera</th>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">IP Address</th>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Last Online</th>
                        <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Actions</th>
                    </tr>
                </thead>
                <tbody class="bg-white divide-y divide-gray-200">
                    @forelse($store->cameras as $camera)
                        <tr class="hover:bg-gray-50">
                            <td class="px-6 py-4 whitespace-nowrap">
                                <a href="{{ route('cameras.show', $camera) }}" class="text-sm font-medium text-blue-600 hover:text-blue-900">
                                    {{ $camera->name }}
                                </a>
                                <div class="text-xs text-gray-400">ID: {{ $camera->id }}</div>
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                                {{ $camera->ip_address }}:{{ $camera->port }}
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap">
                                @if($camera->is_online)
                                    <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                                        <i class="fas fa-circle text-green-400 mr-1.5 text-xs"></i> Online
                                    </span>
                                @else
                                    <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800">
                                        <i class="fas fa-circle text-red-400 mr-1.5 text-xs"></i> Offline
                                    </span>
                                @endif
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                {{ $camera->last_online_at ? $camera->last_online_at->format('Y-m-d H:i') : 'Never' }}
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium space-x-2">
                                <a href="{{ route('cameras.show', $camera) }}" class="text-blue-600 hover:text-blue-900" title="View"><i class="fas fa-eye"></i></a>
                                <a href="{{ route('cameras.edit', $camera) }}" class="text-yellow-600 hover:text-yellow-900" title="Edit"><i class="fas fa-edit"></i></a>
                                <a href="{{ route('cameras.test-connection', $camera) }}" class="text-green-600 hover:text-green-900" title="Test Connection"><i class="fas fa-signal"></i></a>
                            </td>
                        </tr>
                    @empty
                        <tr>
                            <td colspan="5" class="px-6 py-8 text-center text-gray-500">
                                No cameras assigned to this store yet.
                            </td>
                        </tr>
                    @endforelse
                </tbody>
            </table>
        </div>
    </div>
    <!-- Bottom Actions -->
    <div class="flex justify-between items-center pt-2">
        <a href="{{ route('stores.index') }}" class="inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50">
            <i class="fas fa-arrow-left mr-2"></i>
            Back to Stores
        </a>

        @if($store->cameras->count() == 0)
            <form method="POST" action="{{ route('stores.destroy', $store) }}" class="inline"
                  onsubmit="return confirm('Are you sure you want to delete this store?');">
                @csrf
                @method('DELETE')
                <button type="submit" class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-red-600 hover:bg-red-700">
                    <i class="fas fa-trash mr-2"></i>
                    Delete Store
                </button>
            </form>
        @else
            <span class="inline-flex items-center text-xs text-gray-500 bg-gray-100 px-3 py-2 rounded-md" title="Stores with assigned cameras cannot be deleted">
                <i class="fas fa-info-circle mr-1.5 text-gray-400"></i>
                Cannot delete store with active cameras
            </span>
        @endif
    </div>
</div>
@endsection
