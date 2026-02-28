@extends('layouts.app')

@section('title', 'Edit Camera')

@section('content')
<div class="max-w-3xl mx-auto">
    <div class="bg-white rounded-lg shadow-sm p-6">
        <div class="mb-6">
            <h1 class="text-2xl font-semibold text-gray-800">Edit Camera</h1>
            <p class="text-gray-600">Update camera configuration</p>
        </div>

        @if ($errors->any())
            <div class="rounded-md bg-red-50 p-4 mb-6">
                <div class="flex">
                    <div class="flex-shrink-0">
                        <i class="fas fa-exclamation-circle text-red-400"></i>
                    </div>
                    <div class="ml-3">
                        <h3 class="text-sm font-medium text-red-800">
                            Please fix the following errors:
                        </h3>
                        <div class="mt-2 text-sm text-red-700">
                            <ul class="list-disc pl-5 space-y-1">
                                @foreach ($errors->all() as $error)
                                    <li>{{ $error }}</li>
                                @endforeach
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
        @endif

        <form method="POST" action="{{ route('cameras.update', $camera) }}" class="space-y-6">
            @csrf
            @method('PUT')

            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                    <label for="store_id" class="block text-sm font-medium text-gray-700">Store *</label>
                    <select id="store_id" name="store_id" required class="mt-1 block w-full border border-gray-300 rounded-md shadow-sm px-3 py-2 focus:outline-none focus:ring-blue-500 focus:border-blue-500">
                        <option value="">Select a store</option>
                        @foreach($stores as $store)
                            <option value="{{ $store->id }}" {{ (old('store_id', $camera->store_id) == $store->id) ? 'selected' : '' }}>
                                {{ $store->name }}
                            </option>
                        @endforeach
                    </select>
                </div>

                <div>
                    <label for="name" class="block text-sm font-medium text-gray-700">Camera Name *</label>
                    <input type="text" id="name" name="name" required value="{{ old('name', $camera->name) }}"
                           class="mt-1 block w-full border border-gray-300 rounded-md shadow-sm px-3 py-2 focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                           placeholder="e.g., Front Door Camera">
                </div>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                    <label for="ip_address" class="block text-sm font-medium text-gray-700">IP Address *</label>
                    <input type="text" id="ip_address" name="ip_address" required value="{{ old('ip_address', $camera->ip_address) }}"
                           class="mt-1 block w-full border border-gray-300 rounded-md shadow-sm px-3 py-2 focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                           placeholder="192.168.1.100">
                </div>

                <div>
                    <label for="port" class="block text-sm font-medium text-gray-700">Port *</label>
                    <input type="number" id="port" name="port" required value="{{ old('port', $camera->port) }}" min="1" max="65535"
                           class="mt-1 block w-full border border-gray-300 rounded-md shadow-sm px-3 py-2 focus:outline-none focus:ring-blue-500 focus:border-blue-500">
                </div>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                    <label for="username" class="block text-sm font-medium text-gray-700">Username</label>
                    <input type="text" id="username" name="username" value="{{ old('username', $camera->username) }}"
                           class="mt-1 block w-full border border-gray-300 rounded-md shadow-sm px-3 py-2 focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                           placeholder="admin">
                </div>

                <div>
                    <label for="password" class="block text-sm font-medium text-gray-700">Password</label>
                    <input type="text" id="password" name="password" value="{{ old('password', $camera->password) }}"
                           class="mt-1 block w-full border border-gray-300 rounded-md shadow-sm px-3 py-2 focus:outline-none focus:ring-blue-500 focus:border-blue-500">
                </div>
            </div>

            <div>
                <label for="server_port" class="block text-sm font-medium text-gray-700">Server Port (Optional)</label>
                <input type="number" id="server_port" name="server_port" value="{{ old('server_port', $camera->server_port) }}" min="1" max="65535"
                       class="mt-1 block w-full md:w-1/2 border border-gray-300 rounded-md shadow-sm px-3 py-2 focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                       placeholder="8000">
                <p class="mt-1 text-sm text-gray-500">Additional server port if required by the camera</p>
            </div>

            <div class="border-t border-gray-200 pt-6">
                <div class="flex items-center justify-between">
                    <a href="{{ route('cameras.index') }}" class="inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50">
                        <i class="fas fa-arrow-left mr-2"></i>
                        Back to Cameras
                    </a>
                    <button type="submit" class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">
                        <i class="fas fa-save mr-2"></i>
                        Update Camera
                    </button>
                </div>
            </div>
        </form>
    </div>
</div>
@endsection
