@extends('layouts.layout')

@section('content')
<div class="container">
    <div class="row justify-content-center">
        <div class="col-md-8">
            <div class="card mt-5">
                <div class="card-header">
                    <h3>Dashboard</h3>
                </div>
                <div class="card-body">
                    <h4>Welcome, {{ Auth::user()->name }}!</h4>
                    <p>You are logged in.</p>
                    <a href="{{ route('cameras.index') }}" class="btn btn-primary">Manage Cameras</a>
                </div>
            </div>
        </div>
    </div>
</div>
@endsection
