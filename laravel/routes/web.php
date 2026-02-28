<?php

use Illuminate\Support\Facades\Route;
use App\Http\Controllers\HomeController;
use App\Http\Controllers\AuthController;
use App\Http\Controllers\CameraController;
use App\Http\Controllers\JobController;
use App\Http\Controllers\FootageController;
use App\Http\Controllers\StoreController;

// Authentication routes
Route::get('/login', [AuthController::class, 'showLogin'])->name('login');
Route::post('/login', [AuthController::class, 'login']);
Route::post('/logout', [AuthController::class, 'logout'])->name('logout');
Route::get('/change-password', [AuthController::class, 'showChangePassword'])->name('change-password');
Route::post('/change-password', [AuthController::class, 'changePassword']);

// Protected routes (require authentication)
Route::middleware('auth')->group(function () {
    // Redirect root to dashboard
    Route::get('/', function () {
        return redirect()->route('dashboard');
    });

    // Dashboard routes
    Route::get('/dashboard', [HomeController::class, 'index'])->name('dashboard');

    // Camera routes
    Route::resource('cameras', CameraController::class);
    Route::get('/cameras/{camera}/test-connection', [CameraController::class, 'testConnection'])->name('cameras.test-connection');

    // Job routes
    Route::get('/jobs', [JobController::class, 'index'])->name('jobs.index');
    Route::get('/jobs/{job}', [JobController::class, 'show'])->name('jobs.show');

    // Footage routes
    Route::get('/footage', [FootageController::class, 'index'])->name('footage.index');
    Route::get('/footage/download', [FootageController::class, 'download'])->name('footage.download');

    // Store routes
    Route::resource('stores', StoreController::class);
});
