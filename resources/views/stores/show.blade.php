@extends('layouts.layout')

@section('content')
<div class="container">
    <h1 class="mb-4">Store Details</h1>

    <div class="card">
        <div class="card-body">
            <h5 class="card-title">{{ $store->name }}</h5>
        </div>
    </div>

    <a href="{{ route('stores.edit', $store) }}" class="btn btn-warning mt-3">Edit</a>
    <a href="{{ route('stores.index') }}" class="btn btn-secondary mt-3">Back to List</a>
</div>
@endsection
