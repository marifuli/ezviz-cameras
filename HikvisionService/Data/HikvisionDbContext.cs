using Microsoft.EntityFrameworkCore;
using HikvisionService.Models;

namespace HikvisionService.Data;

public class FileDownloadJobDto
{
    public long Id { get; set; }
    public string Status { get; set; } = "";
    public int Progress { get; set; }
    public DateTime CreatedAt { get; set; }

    public long CameraId { get; set; }
    public string CameraName { get; set; } = "";
}

public class HikvisionDbContext : DbContext
{
    public HikvisionDbContext(DbContextOptions<HikvisionDbContext> options) : base(options)
    {
    }

    public DbSet<Camera> Cameras { get; set; } = null!;
    public DbSet<Store> Stores { get; set; } = null!;
    public DbSet<FileDownloadJob> FileDownloadJobs { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<StorageDrive> StorageDrives { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships
        modelBuilder.Entity<Camera>()
            .HasOne(c => c.Store)
            .WithMany(s => s.Cameras)
            .HasForeignKey(c => c.StoreId);

        modelBuilder.Entity<FileDownloadJob>()
            .HasOne(j => j.Camera)
            .WithMany(c => c.FileDownloadJobs)
            .HasForeignKey(j => j.CameraId);

        // Set default values
        modelBuilder.Entity<Camera>()
            .Property(c => c.Port)
            .HasDefaultValue(554);

        modelBuilder.Entity<FileDownloadJob>()
            .Property(j => j.Status)
            .HasDefaultValue("pending");

        modelBuilder.Entity<FileDownloadJob>()
            .Property(j => j.Progress)
            .HasDefaultValue(0);

        // Configure indexes for better performance
        modelBuilder.Entity<FileDownloadJob>()
            .HasIndex(j => j.CameraId);

        modelBuilder.Entity<FileDownloadJob>()
            .HasIndex(j => j.Status);

        modelBuilder.Entity<FileDownloadJob>()
            .HasIndex(j => j.FileType);
    }
}