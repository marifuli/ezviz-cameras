@extends('layouts.layout')

@section('content')
<div class="container">
    <h1 class="mb-4">Edit Store</h1>

    <form action="{{ route('stores.update', $store) }}" method="POST">
        @csrf
        @method('PUT')

        <div class="mb-3">
            <label for="name" class="form-label">Name</label>
            <input type="text" class="form-control" id="name" name="name" value="{{ $store->name }}" required>
        </div>

        <button type="submit" class="btn btn-primary">Update Store</button>
        <a href="{{ route('stores.index') }}" class="btn btn-secondary">Cancel</a>
    </form>
</div>
@endsection
