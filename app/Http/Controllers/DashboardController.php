<?php

namespace App\Http\Controllers;

use App\Models\Camera;
use App\Models\Store;
use Illuminate\Http\Request;

class DashboardController extends Controller
{
    public function index(Request $request)
    {
        $stores = Store::all();

        // Get selected store IDs from request, default to first store if none
        $selectedStoreIds = $request->input('stores', []);
        if (empty($selectedStoreIds) && $stores->isNotEmpty()) {
            $selectedStoreIds = [$stores->first()->id];
        }

        // Get cameras from selected stores
        $cameras = Camera::whereIn('store_id', $selectedStoreIds)->get();

        return view('dashboard', compact('stores', 'cameras', 'selectedStoreIds'));
    }
}
