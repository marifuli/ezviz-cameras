<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class StorageDrive extends Model
{
    use HasFactory;

    protected $fillable = [
        'name',
        'root_path',
        'total_space',
        'used_space',
        'free_space',
        'status',
        'last_checked_at',
    ];

    protected $casts = [
        'total_space' => 'integer',
        'used_space' => 'integer',
        'free_space' => 'integer',
        'last_checked_at' => 'datetime',
        'created_at' => 'datetime',
        'updated_at' => 'datetime',
    ];

    public function getUsagePercentageAttribute(): float
    {
        if ($this->total_space <= 0) {
            return 0;
        }
        return ($this->used_space / $this->total_space) * 100;
    }
}
