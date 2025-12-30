@extends('layouts.layout')

@section('content')
<div class="container">
    <h1 class="mb-4">Camera Details</h1>

    <div class="card">
        <div class="card-body">
            <h5 class="card-title">{{ $camera->name }}</h5>
            <p class="card-text"><strong>Store:</strong> {{ $camera->store->name }}</p>
            <p class="card-text"><strong>IP Address:</strong> {{ $camera->ip_address }}</p>
            <p class="card-text"><strong>RTSP Port:</strong> {{ $camera->port }}</p>
            <p class="card-text"><strong>Username:</strong> {{ $camera->username }}</p>
            <p class="card-text"><strong>Password:</strong> {{ $camera->password }}</p>
        </div>
    </div>

    <a href="{{ route('cameras.edit', $camera) }}" class="btn btn-warning mt-3">Edit</a>
    <a href="{{ route('cameras.index') }}" class="btn btn-secondary mt-3">Back to List</a>
</div>
@endsection
