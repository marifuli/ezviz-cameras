@extends('layouts.layout')

@section('content')
<div class="bg-white shadow-lg rounded-lg p-8 max-w-md mx-auto">
    <h1 class="text-3xl font-bold text-gray-900 mb-6">Edit Camera</h1>

    <form action="{{ route('cameras.update', $camera) }}" method="POST" class="space-y-6">
        @csrf
        @method('PUT')

        <div>
            <label for="store_id" class="block text-sm font-medium text-gray-700">Store</label>
            <select class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 select2" id="store_id" name="store_id" required>
                <option value="">Select Store</option>
                @foreach($stores as $store)
                    <option value="{{ $store->id }}" {{ $camera->store_id == $store->id ? 'selected' : '' }}>{{ $store->name }}</option>
                @endforeach
            </select>
        </div>

        <div>
            <label for="name" class="block text-sm font-medium text-gray-700">Name</label>
            <input type="text" class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500" id="name" name="name" value="{{ $camera->name }}" required>
        </div>

        <div>
            <label for="ip_address" class="block text-sm font-medium text-gray-700">IP Address</label>
            <input type="text" class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500" id="ip_address" name="ip_address" value="{{ $camera->ip_address }}" required>
        </div>

        <div>
            <label for="port" class="block text-sm font-medium text-gray-700">Port</label>
            <input type="number" class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500" id="port" name="port" value="{{ $camera->port }}" required>
        </div>

        <div>
            <label for="username" class="block text-sm font-medium text-gray-700">Username</label>
            <input type="text" class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500" id="username" name="username" value="{{ $camera->username }}" >
        </div>

        <div>
            <label for="password" class="block text-sm font-medium text-gray-700">Password</label>
            <input type="password" class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500" id="password" name="password" value="{{ $camera->password }}" >
        </div>
        <div>
            <label for="password" class="block text-sm font-medium text-gray-700">
                Server port
            </label>
            <input type="text" class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500" id="server_port" name="server_port" value="{{ $camera->server_port }}">
        </div>

        <div class="flex space-x-4">
            <button type="submit" class="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded flex items-center">
                <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path>
                </svg>
                Update Camera
            </button>
            <a href="{{ route('cameras.index') }}" class="bg-gray-500 hover:bg-gray-700 text-white font-bold py-2 px-4 rounded flex items-center">
                <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                </svg>
                Cancel
            </a>
        </div>
    </form>
</div>
@endsection

@section('scripts')
<script>
    $(document).ready(function() {
        $('.select2').select2();
    });
</script>
@endsection
