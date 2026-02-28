<?php

namespace App\Http\Controllers;

use App\Models\User;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\Hash;
use Illuminate\Support\Facades\Log;
use Illuminate\Validation\Rule;

class AuthController extends Controller
{
    public function showLogin()
    {
        if (Auth::check()) {
            return redirect()->route('dashboard');
        }
        return view('auth.login');
    }

    public function login(Request $request)
    {
        $request->validate([
            'email' => 'required|email',
            'password' => 'required',
        ]);

        try {
            $credentials = $request->only('email', 'password');

            if (Auth::attempt($credentials, true)) {
                $request->session()->regenerate();
                Log::info('User logged in successfully', ['email' => $request->email]);
                return redirect()->route('dashboard');
            }

            return back()->withErrors([
                'email' => 'The provided credentials do not match our records.',
            ])->onlyInput('email');
        } catch (\Exception $ex) {
            Log::error('Error during login', ['error' => $ex->getMessage(), 'email' => $request->email]);
            return back()->withErrors([
                'email' => 'An error occurred during login.',
            ])->onlyInput('email');
        }
    }

    public function showRegister()
    {
        // Registration disabled for now
        return redirect()->route('dashboard');
    }

    public function register(Request $request)
    {
        // Registration disabled for now
        return redirect()->route('dashboard');
    }

    public function logout(Request $request)
    {
        Auth::logout();
        $request->session()->invalidate();
        $request->session()->regenerateToken();
        Log::info('User logged out successfully');
        return redirect()->route('login');
    }

    public function showChangePassword()
    {
        if (!Auth::check()) {
            return redirect()->route('login');
        }
        return view('auth.change-password');
    }

    public function changePassword(Request $request)
    {
        $request->validate([
            'current_password' => 'required',
            'password' => 'required|min:6|confirmed',
        ]);

        try {
            $user = Auth::user();

            if (!Hash::check($request->current_password, $user->password)) {
                return back()->withErrors([
                    'current_password' => 'Current password is incorrect.',
                ]);
            }

            $user->password = $request->password; // Will be hashed by the model's mutator
            $user->save();

            Log::info('Password changed successfully', ['user_id' => $user->id]);
            return back()->with('success', 'Password changed successfully.');
        } catch (\Exception $ex) {
            Log::error('Error changing password', ['error' => $ex->getMessage()]);
            return back()->withErrors([
                'password' => 'An error occurred while changing password.',
            ]);
        }
    }
}
