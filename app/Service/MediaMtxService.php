<?php
namespace App\Service;

use GuzzleHttp\Promise\PromiseInterface;
use Illuminate\Http\Response;
use Illuminate\Support\Facades\Http;

class MediaMtxService {
    
    function addCamera($name, $rtspUrl): Response|PromiseInterface
    {
        $mediamtxApi = 'http://' . config('services.mediamtx.ip') . ':9997/v3/paths';
        $username = config('services.mediamtx.admin_user');
        $password = config('services.mediamtx.admin_password');

        $response = Http::withBasicAuth($username, $password)
            ->post($mediamtxApi, [
                'path' => $name,
                'source' => $rtspUrl,
            ]);
        return $response;
    }
}
