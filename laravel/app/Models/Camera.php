<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class Camera extends Model
{
    protected $fillable = [
        'store_id',
        'name',
        'ip_address',
        'port',
        'username',
        'password',
        'server_port'
    ];

    static function boot()
    {
        parent::boot();
        self::created(function ($model) {

        });
        self::updated(function ($model) {

        });
        self::deleted(function ($model) {

        });
    }

    public function store()
    {
        return $this->belongsTo(Store::class);
    }

    public function getHlsUrlAttribute()
    {
        $mediamtxIp = config('services.mediamtx.ip');
        return "http://{$mediamtxIp}:8888/{$this->name}/index.m3u8";
    }
}
