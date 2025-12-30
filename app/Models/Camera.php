<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class Camera extends Model
{
    protected $fillable = [
        'user_id',
        'name',
        'ip_address',
        'port',
        'username',
        'password',
        'wifi_ssid',
        'wifi_password',
    ];

    public function user()
    {
        return $this->belongsTo(User::class);
    }
}
