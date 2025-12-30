<?php

namespace App\Http\Controllers;

use App\Models\Camera;
use GuzzleHttp\Promise\PromiseInterface;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\Http;
use Illuminate\Http\Client\Response;
use App\Service\MediaMtxService;

class CameraController extends Controller
{
    private MediaMtxService $mediamtxService;

    public function __construct(MediaMtxService $mediamtxService)
    {
        $this->mediamtxService = $mediamtxService;
    }

    /**
     * Display a listing of the resource.
     */
    public function index()
    {
        $cameras = Auth::user()->cameras;
        return view('cameras.index', compact('cameras'));
    }

    /**
     * Show the form for creating a new resource.
     */
    public function create()
    {
        return view('cameras.create');
    }

    /**
     * Store a newly created resource in storage.
     */
    public function store(Request $request)
    {
        $request->validate([
            'name' => 'required|string|max:255',
            'ip_address' => 'required|ip',
            'port' => 'required|integer|min:1|max:65535',
            'username' => 'required|string|max:255',
            'password' => 'required|string|max:255',
            'wifi_ssid' => 'nullable|string|max:255',
            'wifi_password' => 'nullable|string|max:255',
        ]);

        Camera::query()->create($request->all());

        return redirect()->route('cameras.index')->with('success', 'Camera added successfully.');
    }

    /**
     * Display the specified resource.
     */
    public function show(string $id)
    {
        $camera = Camera::query()->findOrFail($id);
        return view('cameras.show', compact('camera'));
    }

    /**
     * Show the form for editing the specified resource.
     */
    public function edit(string $id)
    {
        $camera = Camera::query()->findOrFail($id);
        return view('cameras.edit', compact('camera'));
    }

    /**
     * Update the specified resource in storage.
     */
    public function update(Request $request, string $id)
    {
        $camera = Camera::query()->findOrFail($id);

        $request->validate([
            'name' => 'required|string|max:255',
            'ip_address' => 'required|ip',
            'port' => 'required|integer|min:1|max:65535',
            'username' => 'required|string|max:255',
            'password' => 'required|string|max:255',
            'wifi_ssid' => 'nullable|string|max:255',
            'wifi_password' => 'nullable|string|max:255',
        ]);

        $camera->update($request->all());

        return redirect()->route('cameras.index')->with('success', 'Camera updated successfully.');
    }

    /**
     * Remove the specified resource from storage.
     */
    public function destroy(string $id)
    {
        $camera = Camera::query()->findOrFail($id);
        $camera->delete();

        return redirect()->route('cameras.index')->with('success', 'Camera deleted successfully.');
    }
}
