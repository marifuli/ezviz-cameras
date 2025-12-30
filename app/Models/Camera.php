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
    ];

    public function store()
    {
        return $this->belongsTo(Store::class);
    }
}
