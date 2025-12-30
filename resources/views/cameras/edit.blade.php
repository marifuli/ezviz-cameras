@extends('layouts.layout')

@section('content')
<div class="container">
    <h1 class="mb-4">Edit Camera</h1>

    <form action="{{ route('cameras.update', $camera) }}" method="POST">
        @csrf
        @method('PUT')

        <div class="mb-3">
            <label for="store_id" class="form-label">Store</label>
            <select class="form-control select2" id="store_id" name="store_id" required>
                <option value="">Select Store</option>
                @foreach($stores as $store)
                    <option value="{{ $store->id }}" {{ $camera->store_id == $store->id ? 'selected' : '' }}>{{ $store->name }}</option>
                @endforeach
            </select>
        </div>

        <div class="mb-3">
            <label for="name" class="form-label">Name</label>
            <input type="text" class="form-control" id="name" name="name" value="{{ $camera->name }}" required>
        </div>

        <div class="mb-3">
            <label for="ip_address" class="form-label">IP Address</label>
            <input type="text" class="form-control" id="ip_address" name="ip_address" value="{{ $camera->ip_address }}" required>
        </div>

        <div class="mb-3">
            <label for="port" class="form-label">RTSP Port</label>
            <input type="number" class="form-control" id="port" name="port" value="{{ $camera->port }}" required>
        </div>

        <div class="mb-3">
            <label for="username" class="form-label">Username</label>
            <input type="text" class="form-control" id="username" name="username" value="{{ $camera->username }}" required>
        </div>

        <div class="mb-3">
            <label for="password" class="form-label">Password</label>
            <input type="password" class="form-control" id="password" name="password" value="{{ $camera->password }}" required>
        </div>

        <button type="submit" class="btn btn-primary">Update Camera</button>
        <a href="{{ route('cameras.index') }}" class="btn btn-secondary">Cancel</a>
    </form>
</div>
@endsection

@section('scripts')
<script>
    $(document).ready(function() {
        $('.select2').select2();
    });
</script>
@endsection
