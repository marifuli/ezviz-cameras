<?php

namespace App\Http\Controllers;

use App\Models\Store;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Log;

class StoreController extends Controller
{
    public function index()
    {
        try {
            $stores = Store::withCount('cameras')->orderBy('name')->get();
            return view('stores.index', compact('stores'));
        } catch (\Exception $ex) {
            Log::error('Error getting stores', ['error' => $ex->getMessage()]);
            return redirect()->back()->with('error', 'An error occurred while retrieving stores.');
        }
    }

    public function show($id)
    {
        try {
            $store = Store::with('cameras')->findOrFail($id);
            return view('stores.show', compact('store'));
        } catch (\Exception $ex) {
            Log::error("Error getting store {$id}", ['error' => $ex->getMessage()]);
            return redirect()->route('stores.index')->with('error', 'Store not found.');
        }
    }

    public function create()
    {
        return view('stores.create');
    }

    public function store(Request $request)
    {
        $request->validate([
            'name' => 'required|string|max:255',
            'address' => 'nullable|string|max:500',
            'phone' => 'nullable|string|max:20',
            'email' => 'nullable|email|max:100',
        ]);

        try {
            Store::create([
                'name' => $request->name,
                'address' => $request->address,
                'phone' => $request->phone,
                'email' => $request->email,
            ]);

            Log::info('Created store', ['name' => $request->name]);
            return redirect()->route('stores.index')->with('success', 'Store created successfully.');
        } catch (\Exception $ex) {
            Log::error('Error creating store', ['error' => $ex->getMessage()]);
            return back()->withInput()->with('error', 'An error occurred while creating the store.');
        }
    }

    public function edit($id)
    {
        try {
            $store = Store::findOrFail($id);
            return view('stores.edit', compact('store'));
        } catch (\Exception $ex) {
            Log::error("Error getting store {$id} for edit", ['error' => $ex->getMessage()]);
            return redirect()->route('stores.index')->with('error', 'Store not found.');
        }
    }

    public function update(Request $request, $id)
    {
        $request->validate([
            'name' => 'required|string|max:255',
            'address' => 'nullable|string|max:500',
            'phone' => 'nullable|string|max:20',
            'email' => 'nullable|email|max:100',
        ]);

        try {
            $store = Store::findOrFail($id);

            $store->update([
                'name' => $request->name,
                'address' => $request->address,
                'phone' => $request->phone,
                'email' => $request->email,
            ]);

            Log::info('Updated store', ['id' => $id, 'name' => $request->name]);
            return redirect()->route('stores.index')->with('success', 'Store updated successfully.');
        } catch (\Exception $ex) {
            Log::error("Error updating store {$id}", ['error' => $ex->getMessage()]);
            return back()->withInput()->with('error', 'An error occurred while updating the store.');
        }
    }

    public function destroy($id)
    {
        try {
            $store = Store::with('cameras')->findOrFail($id);

            if ($store->cameras->count() > 0) {
                return redirect()->route('stores.index')->with('error', 'Cannot delete store with associated cameras. Please delete or reassign the cameras first.');
            }

            $storeName = $store->name;
            $store->delete();

            Log::info('Deleted store', ['id' => $id, 'name' => $storeName]);
            return redirect()->route('stores.index')->with('success', 'Store deleted successfully.');
        } catch (\Exception $ex) {
            Log::error("Error deleting store {$id}", ['error' => $ex->getMessage()]);
            return redirect()->route('stores.index')->with('error', 'An error occurred while deleting the store.');
        }
    }
}
