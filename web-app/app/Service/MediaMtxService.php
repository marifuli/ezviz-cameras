<?php
namespace App\Service;

use App\Models\Camera;
use Symfony\Component\Yaml\Yaml;

class MediaMtxService {

    static function updateMediaMtxConfig($path = null, $downscale = false)
    {
        if(!$path) $path = base_path('mediamtx.yml');

        $paths = [];

        foreach (Camera::get() as $camera) {
            $user = $camera->username;
            $pass = $camera->password;
            $host = $camera->ip_address;
            $port = $camera->port;
            if($user && $pass)
                $rtsp = "rtsp://$user:$pass@$host:$port/h264";
            else
                $rtsp = "rtsp://$host:$port/h264";
            $name = "cam" . $camera->id;

            // FFmpeg command for recording
            if ($downscale) {
                // $ffmpegCommand = "ffmpeg -i $rtsp -vf scale=640:480 -c:v libx264 -preset fast -c:a aac -f segment -segment_time 3600 -strftime 1 \"$recordFolder/%Y-%m-%d/$name-%H.mp4\"";
            } else {
                // $ffmpegCommand = "ffmpeg -i $rtsp -c copy -f segment -segment_time 3600 -strftime 1 \"$recordFolder/%Y-%m-%d/$name-%H.mp4\"";
            }

            $paths[$name] = [
                'source' => $rtsp,
                'record' => true,
                'recordPath' => "/recordings/%path/%Y-%m-%d/%H-%M-%S-%f",
                "recordFormat" => 'fmp4',
                "recordMaxPartSize" => "50M",
                "recordSegmentDuration" => "1h",
                "recordDeleteAfter" => "200h"
            ];
        }

        $config = [
            'webrtc' => true,
            'hls' => true,
            "playback" => true,
            "playbackAddress" => ":9996",
            'api' => true,
            'apiAddress' => ':9997',
            'authMethod' => 'internal',
            'authInternalUsers' => [
                [
                    'user' => config('services.mediamtx.admin_user'),
                    'pass' => config('services.mediamtx.admin_password'),
                    'permissions' => [
                        ['action' => 'publish'],
                        ['action' => 'read'],
                        ['action' => 'playback'],
                        ['action' => 'api']
                    ]
                ]
            ],
            'paths' => $paths
        ];

        // Use Symfony Yaml component to dump the YAML
        $yaml = Yaml::dump($config, 6, 2, Yaml::DUMP_MULTI_LINE_LITERAL_BLOCK);
        $yaml = trim($yaml);
        $yaml = str_replace(" true", " yes", $yaml);
        $yaml = str_replace(" false", " no", $yaml);
        file_put_contents($path, $yaml);
        return $yaml;
    }
}
