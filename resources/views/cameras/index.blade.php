@extends('layouts.layout')

@section('content')
<div class="container">
    <h1 class="mb-4">Cameras</h1>

    <a href="{{ route('cameras.create') }}" class="btn btn-primary mb-3">Add Camera</a>

    @if(session('success'))
        <div class="alert alert-success">{{ session('success') }}</div>
    @endif

    <table class="table table-striped">
        <thead>
            <tr>
                <th>Name</th>
                <th>IP Address</th>
                <th>Port</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            @forelse($cameras as $camera)
                <tr>
                    <td>{{ $camera->name }}</td>
                    <td>{{ $camera->ip_address }}</td>
                    <td>{{ $camera->port }}</td>
                    <td>
                        <a href="{{ route('cameras.show', $camera) }}" class="btn btn-info btn-sm">View</a>
                        <a href="{{ route('cameras.edit', $camera) }}" class="btn btn-warning btn-sm">Edit</a>
                        <form action="{{ route('cameras.destroy', $camera) }}" method="POST" class="d-inline">
                            @csrf
                            @method('DELETE')
                            <button type="submit" class="btn btn-danger btn-sm" onclick="return confirm('Are you sure?')">Delete</button>
                        </form>
                    </td>
                </tr>
            @empty
                <tr>
                    <td colspan="4" class="text-center">No cameras found.</td>
                </tr>
            @endforelse
        </tbody>
    </table>
</div>
@endsection
