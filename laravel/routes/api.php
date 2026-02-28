<?php

use Illuminate\Http\Request;
use Illuminate\Support\Facades\Route;
use App\Http\Controllers\Api\ApiController;

// Dashboard API
Route::get('/dashboard', [ApiController::class, 'getDashboardData']);
Route::get('/worker-status', [ApiController::class, 'getWorkerStatus']);

// Camera API endpoints
Route::prefix('cameras')->group(function () {
    Route::get('/', [ApiController::class, 'getAllCameras']);
    Route::get('/{id}', [ApiController::class, 'getCamera']);
    Route::post('/{id}/check', [ApiController::class, 'checkCameraConnection']);
    Route::post('/check-all', [ApiController::class, 'checkAllCameras']);
});

// Storage drive API endpoints
Route::prefix('storage-drives')->group(function () {
    Route::get('/', [ApiController::class, 'getAllStorageDrives']);
    Route::get('/{id}', [ApiController::class, 'getStorageDrive']);
    Route::post('/', [ApiController::class, 'addStorageDrive']);
    Route::put('/{id}', [ApiController::class, 'updateStorageDrive']);
    Route::delete('/{id}', [ApiController::class, 'deleteStorageDrive']);
    Route::post('/check', [ApiController::class, 'checkStorageDrives']);
});

// Download job API endpoints
Route::prefix('download-jobs')->group(function () {
    Route::get('/', [ApiController::class, 'getAllDownloadJobs']);
    Route::get('/active', [ApiController::class, 'getActiveDownloadJobs']);
    Route::get('/failed', [ApiController::class, 'getFailedDownloadJobs']);
    Route::get('/{id}', [ApiController::class, 'getDownloadJob']);
});

// Footage API endpoints
Route::prefix('footage')->group(function () {
    Route::get('/', [ApiController::class, 'getFootageFiles']);
    Route::get('/download', [ApiController::class, 'downloadFootage']);
});
