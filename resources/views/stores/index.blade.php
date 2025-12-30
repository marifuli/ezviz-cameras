@extends('layouts.layout')

@section('content')
<div class="container">
    <h1 class="mb-4">Stores</h1>

    <a href="{{ route('stores.create') }}" class="btn btn-primary mb-3">Add Store</a>

    @if(session('success'))
        <div class="alert alert-success">{{ session('success') }}</div>
    @endif

    <table id="stores-table" class="table table-striped">
        <thead>
            <tr>
                <th>Name</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            @forelse($stores as $store)
                <tr>
                    <td >{{ $store->name }}</td>
                    <td style="width: 200px">
                        <a href="{{ route('stores.show', $store) }}" class="btn btn-info btn-sm">View</a>
                        <a href="{{ route('stores.edit', $store) }}" class="btn btn-warning btn-sm">Edit</a>
                        <form action="{{ route('stores.destroy', $store) }}" method="POST" class="d-inline">
                            @csrf
                            @method('DELETE')
                            <button type="submit" class="btn btn-danger btn-sm" onclick="return confirm('Are you sure?')">Delete</button>
                        </form>
                    </td>
                </tr>
            @empty

            @endforelse
        </tbody>
    </table>
</div>
@endsection

@section('scripts')
<script>
    $(document).ready(function() {
        $('#stores-table').DataTable();
    });
</script>
@endsection
