@extends('layouts.layout')

@section('content')
<div class="bg-white shadow-lg rounded-lg p-8">
    <h1 class="text-4xl font-bold text-gray-900 mb-4">Dashboard</h1>
    <p class="text-gray-600 text-lg mb-8">Welcome to the Ezviz Camera Management System.</p>
    <div class="grid grid-cols-1 md:grid-cols-2 gap-8">
        <div class="bg-gradient-to-r from-blue-500 to-blue-600 text-white shadow-lg rounded-lg p-6">
            <h2 class="text-2xl font-semibold mb-3">Stores</h2>
            <p class="mb-6 opacity-90">Manage your stores efficiently.</p>
            <div class="flex flex-col sm:flex-row gap-4">
                <a href="{{ route('stores.index') }}" class="bg-white text-blue-600 hover:bg-gray-100 font-semibold py-2 px-4 rounded-lg transition duration-200 flex items-center">
                    <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"></path>
                    </svg>
                    View Stores
                </a>
                <a href="{{ route('stores.create') }}" class="bg-blue-700 hover:bg-blue-800 text-white font-semibold py-2 px-4 rounded-lg transition duration-200 flex items-center">
                    <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path>
                    </svg>
                    Add Store
                </a>
            </div>
        </div>
        <div class="bg-gradient-to-r from-green-500 to-green-600 text-white shadow-lg rounded-lg p-6">
            <h2 class="text-2xl font-semibold mb-3">Cameras</h2>
            <p class="mb-6 opacity-90">Manage your cameras seamlessly.</p>
            <div class="flex flex-col sm:flex-row gap-4">
                <a href="{{ route('cameras.index') }}" class="bg-white text-green-600 hover:bg-gray-100 font-semibold py-2 px-4 rounded-lg transition duration-200 flex items-center">
                    <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z"></path>
                    </svg>
                    View Cameras
                </a>
                <a href="{{ route('cameras.create') }}" class="bg-green-700 hover:bg-green-800 text-white font-semibold py-2 px-4 rounded-lg transition duration-200 flex items-center">
                    <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path>
                    </svg>
                    Add Camera
                </a>
            </div>
        </div>
    </div>
</div>
@endsection
