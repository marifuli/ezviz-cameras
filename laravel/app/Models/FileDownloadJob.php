<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class FileDownloadJob extends Model
{
    use HasFactory;

    protected $fillable = [
        'camera_id',
        'status',
        'progress',
        'start_time',
        'end_time',
        'file_name',
        'file_type',
        'file_size',
        'file_start_time',
        'file_end_time',
        'download_path',
        'error_message',
    ];

    protected $casts = [
        'camera_id' => 'integer',
        'progress' => 'integer',
        'file_size' => 'integer',
        'start_time' => 'datetime',
        'end_time' => 'datetime',
        'file_start_time' => 'datetime',
        'file_end_time' => 'datetime',
        'created_at' => 'datetime',
        'updated_at' => 'datetime',
    ];

    public function camera(): BelongsTo
    {
        return $this->belongsTo(Camera::class);
    }

    public function scopeActive($query)
    {
        return $query->where('status', 'downloading');
    }

    public function scopeFailed($query)
    {
        return $query->where('status', 'failed');
    }

    public function scopeCompleted($query)
    {
        return $query->where('status', 'completed');
    }
}
