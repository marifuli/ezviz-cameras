<?php

return [

    /*
    |--------------------------------------------------------------------------
    | Third Party Services
    |--------------------------------------------------------------------------
    |
    | This file is for storing the credentials for third party services such
    | as Mailgun, Postmark, AWS and more. This file provides the de facto
    | location for this type of information, allowing packages to have
    | a conventional file to locate the various service credentials.
    |
    */

    'mediamtx' => [
        'admin_user' => env('MEDIA_MTX_USERNAME', 'admin'),
        'admin_password' => env('MEDIA_MTX_PASSWORD', 'strongpassword'),
        'ip' => env('MEDIA_MTX_IP', '192.168.0.100'),
    ],

];
