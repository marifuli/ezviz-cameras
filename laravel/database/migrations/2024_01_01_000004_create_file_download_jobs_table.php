<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    /**
     * Run the migrations.
     */
    public function up(): void
    {
        Schema::create('file_download_jobs', function (Blueprint $table) {
            $table->id();
            $table->foreignId('camera_id')->constrained('cameras')->cascadeOnDelete();
            $table->string('status')->default('pending');
            $table->integer('progress')->default(0);
            $table->timestamp('start_time')->nullable();
            $table->timestamp('end_time')->nullable();
            $table->string('file_name');
            $table->string('file_type')->default('video');
            $table->unsignedBigInteger('file_size')->default(0);
            $table->timestamp('file_start_time')->nullable();
            $table->timestamp('file_end_time')->nullable();
            $table->string('download_path')->nullable();
            $table->text('error_message')->nullable();
            $table->timestamps();
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('file_download_jobs');
    }
};
