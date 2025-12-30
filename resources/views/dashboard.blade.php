@extends('layouts.layout')

@section('content')
<div class="container">
    <div class="row">
        <div class="col-md-12">
            <h1 class="mt-4">Dashboard</h1>
            <p>Welcome to the Ezviz Camera Management System.</p>
        </div>
    </div>
    <div class="row mt-4">
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <h5>Stores</h5>
                </div>
                <div class="card-body">
                    <p>Manage your stores.</p>
                    <a href="{{ route('stores.index') }}" class="btn btn-primary">View Stores</a>
                    <a href="{{ route('stores.create') }}" class="btn btn-secondary">Add Store</a>
                </div>
            </div>
        </div>
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <h5>Cameras</h5>
                </div>
                <div class="card-body">
                    <p>Manage your cameras.</p>
                    <a href="{{ route('cameras.index') }}" class="btn btn-primary">View Cameras</a>
                    <a href="{{ route('cameras.create') }}" class="btn btn-secondary">Add Camera</a>
                </div>
            </div>
        </div>
    </div>
</div>
@endsection
