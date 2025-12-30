@extends('layouts.layout')

@section('content')
<div class="bg-white shadow-lg rounded-lg p-8 max-w-md mx-auto">
    <h1 class="text-3xl font-bold text-gray-900 mb-6">Camera Details</h1>

    <div class="space-y-4">
        <div>
            <h2 class="text-xl font-semibold text-gray-900">{{ $camera->name }}</h2>
        </div>
        <div class="grid grid-cols-1 gap-4">
            <div class="flex justify-between">
                <span class="font-medium text-gray-700">Store:</span>
                <span class="text-gray-900">{{ $camera->store->name }}</span>
            </div>
            <div class="flex justify-between">
                <span class="font-medium text-gray-700">IP Address:</span>
                <span class="text-gray-900">{{ $camera->ip_address }}</span>
            </div>
            <div class="flex justify-between">
                <span class="font-medium text-gray-700">RTSP Port:</span>
                <span class="text-gray-900">{{ $camera->port }}</span>
            </div>
            <div class="flex justify-between">
                <span class="font-medium text-gray-700">Username:</span>
                <span class="text-gray-900">{{ $camera->username }}</span>
            </div>
            <div class="flex justify-between">
                <span class="font-medium text-gray-700">Password:</span>
                <span class="text-gray-900">{{ $camera->password }}</span>
            </div>
        </div>
    </div>

    <div class="mt-6 flex space-x-4">
        <a href="{{ route('cameras.edit', $camera) }}" class="bg-yellow-500 hover:bg-yellow-700 text-white font-bold py-2 px-4 rounded flex items-center">
            <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path>
            </svg>
            Edit
        </a>
        <a href="{{ route('cameras.index') }}" class="bg-gray-500 hover:bg-gray-700 text-white font-bold py-2 px-4 rounded flex items-center">
            <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18"></path>
            </svg>
            Back to List
        </a>
    </div>
</div>
@endsection
