<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\Relations\HasMany;

class Camera extends Model
{
    use HasFactory;

    protected $fillable = [
        'store_id',
        'name',
        'ip_address',
        'port',
        'username',
        'password',
        'server_port',
        'is_online',
        'last_online_at',
        'last_downloaded_at',
        'last_error',
        'last_error_at',
        'last_health_check_at',
    ];

    protected $casts = [
        'is_online' => 'boolean',
        'port' => 'integer',
        'server_port' => 'integer',
        'last_online_at' => 'datetime',
        'last_downloaded_at' => 'datetime',
        'last_error_at' => 'datetime',
        'last_health_check_at' => 'datetime',
        'created_at' => 'datetime',
        'updated_at' => 'datetime',
    ];

    protected $hidden = [
        'password',
    ];

    public function store(): BelongsTo
    {
        return $this->belongsTo(Store::class);
    }

    public function fileDownloadJobs(): HasMany
    {
        return $this->hasMany(FileDownloadJob::class);
    }

    public function getStatusAttribute(): string
    {
        if ($this->fileDownloadJobs()->where('status', 'downloading')->exists()) {
            return 'Downloading';
        }
        return 'Idle';
    }
}
