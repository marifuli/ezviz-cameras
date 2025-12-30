@extends('layouts.layout')

@section('content')
<div class="container">
    <h1 class="mb-4">Add Store</h1>

    <form action="{{ route('stores.store') }}" method="POST">
        @csrf

        <div class="mb-3">
            <label for="name" class="form-label">Name</label>
            <input type="text" class="form-control" id="name" name="name" required>
        </div>

        <button type="submit" class="btn btn-primary">Add Store</button>
        <a href="{{ route('stores.index') }}" class="btn btn-secondary">Cancel</a>
    </form>
</div>
@endsection
