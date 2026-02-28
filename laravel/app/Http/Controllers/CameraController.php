<?php

namespace App\Http\Controllers;

use App\Models\Camera;
use App\Models\Store;
use App\Services\HikvisionService;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Log;

class CameraController extends Controller
{
    protected $hikvisionService;

    public function __construct(HikvisionService $hikvisionService)
    {
        $this->hikvisionService = $hikvisionService;
    }

    public function index()
    {
        try {
            $cameras = Camera::with('store')->orderBy('name')->get();
            return view('cameras.index', compact('cameras'));
        } catch (\Exception $ex) {
            Log::error('Error getting cameras', ['error' => $ex->getMessage()]);
            return redirect()->back()->with('error', 'An error occurred while retrieving cameras.');
        }
    }

    public function show($id)
    {
        try {
            $camera = Camera::with('store')->findOrFail($id);
            return view('cameras.show', compact('camera'));
        } catch (\Exception $ex) {
            Log::error("Error getting camera {$id}", ['error' => $ex->getMessage()]);
            return redirect()->route('cameras.index')->with('error', 'Camera not found.');
        }
    }

    public function create()
    {
        $stores = Store::orderBy('name')->get();
        return view('cameras.create', compact('stores'));
    }

    public function store(Request $request)
    {
        $request->validate([
            'store_id' => 'required|exists:stores,id',
            'name' => 'required|string|max:255',
            'ip_address' => 'required|ip',
            'port' => 'required|integer|min:1|max:65535',
            'username' => 'nullable|string|max:100',
            'password' => 'nullable|string|max:100',
            'server_port' => 'nullable|integer|min:1|max:65535',
        ]);

        try {
            Camera::create([
                'store_id' => $request->store_id,
                'name' => $request->name,
                'ip_address' => $request->ip_address,
                'port' => $request->port,
                'username' => $request->username,
                'password' => $request->password,
                'server_port' => $request->server_port,
                'is_online' => false,
            ]);

            Log::info('Created camera', ['name' => $request->name]);
            return redirect()->route('cameras.index')->with('success', 'Camera created successfully.');
        } catch (\Exception $ex) {
            Log::error('Error creating camera', ['error' => $ex->getMessage()]);
            return back()->withInput()->with('error', 'An error occurred while creating the camera.');
        }
    }

    public function edit($id)
    {
        try {
            $camera = Camera::findOrFail($id);
            $stores = Store::orderBy('name')->get();
            return view('cameras.edit', compact('camera', 'stores'));
        } catch (\Exception $ex) {
            Log::error("Error getting camera {$id} for edit", ['error' => $ex->getMessage()]);
            return redirect()->route('cameras.index')->with('error', 'Camera not found.');
        }
    }

    public function update(Request $request, $id)
    {
        $request->validate([
            'store_id' => 'required|exists:stores,id',
            'name' => 'required|string|max:255',
            'ip_address' => 'required|ip',
            'port' => 'required|integer|min:1|max:65535',
            'username' => 'nullable|string|max:100',
            'password' => 'nullable|string|max:100',
            'server_port' => 'nullable|integer|min:1|max:65535',
        ]);

        try {
            $camera = Camera::findOrFail($id);

            $camera->update([
                'store_id' => $request->store_id,
                'name' => $request->name,
                'ip_address' => $request->ip_address,
                'port' => $request->port,
                'username' => $request->username,
                'password' => $request->password,
                'server_port' => $request->server_port,
            ]);

            Log::info('Updated camera', ['id' => $id, 'name' => $request->name]);
            return redirect()->route('cameras.index')->with('success', 'Camera updated successfully.');
        } catch (\Exception $ex) {
            Log::error("Error updating camera {$id}", ['error' => $ex->getMessage()]);
            return back()->withInput()->with('error', 'An error occurred while updating the camera.');
        }
    }

    public function destroy($id)
    {
        try {
            $camera = Camera::with('fileDownloadJobs')->findOrFail($id);

            // Delete associated download jobs
            if ($camera->fileDownloadJobs->count() > 0) {
                $camera->fileDownloadJobs()->delete();
            }

            $cameraName = $camera->name;
            $camera->delete();

            Log::info('Deleted camera', ['id' => $id, 'name' => $cameraName]);
            return redirect()->route('cameras.index')->with('success', 'Camera deleted successfully.');
        } catch (\Exception $ex) {
            Log::error("Error deleting camera {$id}", ['error' => $ex->getMessage()]);
            return redirect()->route('cameras.index')->with('error', 'An error occurred while deleting the camera.');
        }
    }

    public function testConnection($id)
    {
        try {
            $channels = $this->hikvisionService->testCameraConnection($id);
            $message = 'Camera channels are:<br>' . implode('<br>', $channels);
            return redirect()->route('cameras.show', $id)->with('success', $message);
        } catch (\Exception $ex) {
            Log::error("Error testing connection for camera {$id}", ['error' => $ex->getMessage()]);
            return redirect()->route('cameras.show', $id)->with('error', 'An error occurred while testing the camera connection.');
        }
    }
}
