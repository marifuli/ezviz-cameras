<?php

namespace App\Http\Controllers;

use App\Models\FileDownloadJob;
use App\Models\Camera;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Log;

class JobController extends Controller
{
    public function index(Request $request)
    {
        try {
            $query = FileDownloadJob::with('camera');

            // Filter by camera if specified
            if ($request->has('camera') && $request->camera) {
                $query->where('camera_id', $request->camera);
            }

            // Filter by status if specified
            if ($request->has('status') && $request->status) {
                $query->where('status', $request->status);
            }

            $jobs = $query->orderByDesc('created_at')->paginate(25);
            $cameras = Camera::orderBy('name')->get();

            return view('jobs.index', compact('jobs', 'cameras'));
        } catch (\Exception $ex) {
            Log::error('Error getting download jobs', ['error' => $ex->getMessage()]);
            return redirect()->back()->with('error', 'An error occurred while retrieving download jobs.');
        }
    }

    public function show($id)
    {
        try {
            $job = FileDownloadJob::with('camera')->findOrFail($id);
            return view('jobs.show', compact('job'));
        } catch (\Exception $ex) {
            Log::error("Error getting download job {$id}", ['error' => $ex->getMessage()]);
            return redirect()->route('jobs.index')->with('error', 'Download job not found.');
        }
    }
}
