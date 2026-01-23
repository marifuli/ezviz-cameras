using Microsoft.EntityFrameworkCore;
using Serilog;
using HikvisionService.Data;
using HikvisionService.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/hikvision-service-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure SQLite database connection to shared Laravel database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=../laravel/database/database.sqlite";

builder.Services.AddDbContext<HikvisionDbContext>(options =>
    options.UseSqlite(connectionString));

// Register services
builder.Services.AddScoped<IHikvisionService, HikvisionService.Services.HikvisionService>();

// Configure CORS for Laravel integration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS
app.UseCors();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created (for file_download_jobs table if it doesn't exist)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HikvisionDbContext>();
    try
    {
        // Only create tables that don't exist (file_download_jobs)
        context.Database.EnsureCreated();
        Log.Information("Database tables ensured");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error ensuring database tables");
    }
}

Log.Information("Hikvision Service starting...");
app.Run();
