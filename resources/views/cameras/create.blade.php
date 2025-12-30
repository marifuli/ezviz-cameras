@extends('layouts.layout')

@section('content')
<div class="container">
    <h1 class="mb-4">Add Camera</h1>

    <form action="{{ route('cameras.store') }}" method="POST">
        @csrf

        <div class="mb-3">
            <label for="name" class="form-label">Name</label>
            <input type="text" class="form-control" id="name" name="name" required>
        </div>

        <div class="mb-3">
            <label for="ip_address" class="form-label">IP Address</label>
            <input type="text" class="form-control" id="ip_address" name="ip_address" required>
        </div>

        <div class="mb-3">
            <label for="port" class="form-label">Port</label>
            <input type="number" class="form-control" id="port" name="port" value="554" required>
        </div>

        <div class="mb-3">
            <label for="username" class="form-label">Username</label>
            <input type="text" class="form-control" id="username" name="username" required>
        </div>

        <div class="mb-3">
            <label for="password" class="form-label">Password</label>
            <input type="password" class="form-control" id="password" name="password" required>
        </div>

        <div class="mb-3">
            <label for="wifi_ssid" class="form-label">WiFi SSID (Optional)</label>
            <input type="text" class="form-control" id="wifi_ssid" name="wifi_ssid">
        </div>

        <div class="mb-3">
            <label for="wifi_password" class="form-label">WiFi Password (Optional)</label>
            <input type="password" class="form-control" id="wifi_password" name="wifi_password">
        </div>

        <button type="submit" class="btn btn-primary">Add Camera</button>
        <a href="{{ route('cameras.index') }}" class="btn btn-secondary">Cancel</a>
    </form>
</div>
@endsection
