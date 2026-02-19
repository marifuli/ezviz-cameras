using Microsoft.EntityFrameworkCore;
using Serilog;
using HikvisionService.Data;
using HikvisionService.Services;
using Hik.Api;

var dhakaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Dhaka");
var dhakaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, dhakaTimeZone);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Configure lowercase URLs
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true; // Optional: Also make query strings lowercase
});

// Use Serilog
builder.Host.UseSerilog();

// configure HikApi
string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
HikApi.SetLibraryPath(currentDirectory);

// Initialize with proper logging and force reinitialization
HikApi.Initialize(
    logLevel: 3, 
    logDirectory: "logs", 
    autoDeleteLogs: true,
    waitTimeMilliseconds: 5000,
    forceReinitialization: true
);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure background service intervals
builder.Services.Configure<BackgroundServiceOptions>(builder.Configuration.GetSection("BackgroundServices"));

// Add Authentication
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
    });

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure SQLite database connection to shared Laravel database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=./database.sqlite";

builder.Services.AddDbContext<HikvisionDbContext>(options =>
    options.UseSqlite(connectionString));

// Register services
builder.Services.AddScoped<IHikvisionService, HikvisionService.Services.HikvisionService>();

// Register background services
builder.Services.AddHostedService<CameraHealthCheckService>();
builder.Services.AddHostedService<StorageMonitoringService>();
builder.Services.AddHostedService<CameraWorkerManager>();

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
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// MVC routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// API routing
app.MapControllers();

// Ensure database is created and apply migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HikvisionDbContext>();
    try
    {
        // Ensure database exists
        context.Database.EnsureCreated();
        Log.Information("Database tables ensured");
        
        // Apply migrations
        MigrationRunner.ApplyMigrations(app.Services);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error ensuring database tables or applying migrations");
    }
}

app.Run();
