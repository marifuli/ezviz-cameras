@extends('layouts.layout')

@section('main')
<div class="p-4">
    <h1 class="text-4xl font-bold text-gray-900 mb-4">Live Camera Feeds</h1>

    <form method="GET" action="{{ route('dashboard') }}" class="mb-8 flex">
        <div style="width: 100%" class="ml-2">
            <label for="stores" class="block text-sm font-medium text-gray-700 mb-2">Select Stores:</label>
            <select name="stores[]" id="stores" multiple class="block w-full" required>
                @foreach($stores as $store)
                    <option value="{{ $store->id }}" {{ in_array($store->id, $selectedStoreIds) ? 'selected' : '' }}>{{ $store->name }}</option>
                @endforeach
            </select>
        </div>
        <div style="width: 100px">
            <label for="">&nbsp;</label>
            <button type="submit" class=" bg-indigo-600 hover:bg-indigo-700 text-white font-semibold py-2 px-4 rounded-lg transition duration-200">Submit</button>
        </div>
    </form>

    @if($cameras->count() > 0)
        <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5 gap-6">
            @foreach($cameras as $camera)
                <div class="relative bg-gray-100 rounded-lg overflow-hidden shadow-lg">
                    <div class="absolute top-2 left-2 bg-black bg-opacity-50 text-white px-2 py-1 rounded text-sm z-10">
                        {{ $camera->name }}
                    </div>
                    <video id="video-{{ $camera->id }}" class="w-full h-48 object-cover" controls autoplay muted></video>
                </div>
            @endforeach
        </div>
    @else
        <p class="text-gray-600">No cameras found for the selected stores.</p>
    @endif
</div>


<script src="https://cdn.jsdelivr.net/npm/hls.js@latest"></script>
<script>
    $(document).ready(function() {
        $('#stores').select2({
            placeholder: 'Select stores',
            allowClear: true
        });
    });
</script>
<script>
    let host = "{{ config('services.mediamtx.ip') }}",
        username = "{{ config("services.mediamtx.admin_user") }}",
        password = "{{ config("services.mediamtx.admin_password") }}",
        local = window.location.origin.includes('localhost') ? 'http' : 'https',
        cameras = @json($cameras);

    cameras.forEach(camera => {
        const video = document.getElementById('video-' + camera.id);
        const videoSrc = local + `://${host}:8888/cam${camera.id}/index.m3u8`;

        if (Hls.isSupported()) {
            const hls = new Hls({
                xhrSetup: function (xhr, url) {
                    const credentials = btoa(`${username}:${password}`);
                    xhr.setRequestHeader("Authorization", `Basic ${credentials}`);
                },
            });
            hls.on(Hls.Events.MEDIA_ATTACHED, () => {
                hls.loadSource(videoSrc);
            });
            hls.attachMedia(video);
        } else if (video.canPlayType('application/vnd.apple.mpegurl')) {
            video.src = videoSrc;
        }
    })
</script>
@endsection
