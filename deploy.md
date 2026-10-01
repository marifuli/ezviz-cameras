# Deployment & Development Guide for Ezviz / Hikvision Camera Management System

This guide provides complete instructions for setting up the development environment and deploying the application on a Linux Virtual Private Server (VPS).

---

## 1. System Architecture & Overview

The system consists of three main components:

1. **Laravel Web Application (`/laravel`)**:
   - Built on **Laravel 11** (PHP 8.2+).
   - Provides web dashboard, camera & store management, video footage browser, REST APIs, and background job dispatching.
   - Manages queue workers and scheduled tasks (`CheckCameraStatus`, `CheckCameraFootageStatus`, `CheckStorageStatus`).
   - Spawns and manages dedicated worker processes per camera (`php artisan camera:work {camera-id}`).

2. **C# .NET Console Application (`/console-app`)**:
   - Built on **.NET 6.0**.
   - Acts as the CLI bridge between Laravel and the Hikvision/Ezviz cameras.
   - Provides CLI commands for:
     - `camera test`: Verifies camera connectivity and lists channels.
     - `camera list`: Retrieves remote video recording metadata for specified time intervals.
     - `camera download`: Streams and downloads recorded video files to disk.

3. **Hikvision NetSDK Native Wrapper (`/Hik.Api`)**:
   - C# wrapper targeting native 64-bit Linux shared libraries (`HikvisionSDK/*.so` and `HCNetSDKCom/*.so`).
   - Direct low-level P/Invoke interface communicating with Hikvision/Ezviz hardware over port `8000`.

---

## 2. Server & System Prerequisites

### Minimum Server Requirements
- **OS**: Ubuntu 22.04 LTS or Ubuntu 24.04 LTS (x86_64 / amd64 architecture required for native `.so` SDK libraries).
- **CPU**: 2+ Cores (recommended 4+ for multiple concurrent video downloads).
- **RAM**: 2 GB minimum (4 GB+ recommended).
- **Disk**: SSD storage with sufficient capacity for video recordings and database.

### Required Software Packages
- **PHP 8.2 or 8.3** with extensions:
  - `php-cli`, `php-fpm`, `php-mbstring`, `php-xml`, `php-curl`, `php-zip`, `php-sqlite3` (or `php-mysql`), `php-pcntl`, `php-posix`, `php-bcmath`
- **Composer 2.x**
- **.NET 6.0 SDK** (or .NET 8.0 SDK with .NET 6 runtime support)
- **Nginx**
- **Supervisor** (process monitor for camera workers and queue workers)
- **Git**
- **Native SDK runtime dependencies**: `libssl1.1` (or OpenSSL 1.1 compat), `libopenal1`

---

## 3. Local Development Setup

### 3.1. Install Dependencies on Local Machine

#### On Ubuntu / Debian:
```bash
# Update package list
sudo apt update

# Install PHP 8.2 and extensions
sudo apt install -y php8.2 php8.2-cli php8.2-curl php8.2-mbstring php8.2-xml \
    php8.2-zip php8.2-sqlite3 php8.2-bcmath composer

# Install .NET SDK 6.0 or 8.0
sudo apt install -y dotnet-sdk-8.0
```

### 3.2. Clone & Build the .NET Console Application

The Laravel backend invokes the compiled .NET assembly. Build the console application in Release mode:

```bash
cd console-app
dotnet build -c Release
cd ..
```

Verify that the build outputs exist:
```bash
# Output binary should be at:
# console-app/bin/Release/net6.0-windows/ConsoleApp.dll (or net6.0/ConsoleApp.dll)
```

Test the CLI tool directly:
```bash
dotnet console-app/bin/Release/net6.0-windows/ConsoleApp.dll camera
```

### 3.3. Configure & Run Laravel

```bash
cd laravel

# Install Composer packages
composer install

# Copy environment configuration
cp .env.example .env

# Generate application key
php artisan key:generate

# Prepare SQLite database (or configure MySQL in .env)
touch database/database.sqlite

# Run database migrations
php artisan migrate

# Seed initial data (optional)
php artisan db:seed
```

Check `laravel/config/app.php` to ensure the `ezviz-console` path matches your compiled console DLL path:
```php
'ezviz-console' => "dotnet " . base_path("/../console-app/bin/Release/net6.0-windows/ConsoleApp.dll") . ' camera ',
```

Run the development server:
```bash
php artisan serve
```

In separate terminal windows, run the queue listener and a camera worker for testing:
```bash
# Queue listener
php artisan queue:listen

# Camera worker (replace 1 with your camera ID in database)
php artisan camera:work 1
```

Access the dashboard at `http://localhost:8000`.


---

## 4. Production VPS Deployment Guide

### Step 1: VPS Initial Server Setup & Package Installation

Connect to your VPS as `root` or a `sudo` user:

```bash
sudo apt update && sudo apt upgrade -y
sudo apt install -y curl wget git unzip nginx supervisor ufw

# Install PHP 8.2 and required extensions
sudo add-apt-repository ppa:ondrej/php -y
sudo apt update
sudo apt install -y php8.2-fpm php8.2-cli php8.2-curl php8.2-mbstring \
    php8.2-xml php8.2-zip php8.2-sqlite3 php8.2-mysql php8.2-bcmath \
    php8.2-pcntl php8.2-posix

# Install Composer
curl -sS https://getcomposer.org/installer | sudo php -- --install-dir=/usr/local/bin --filename=composer

# Install Microsoft Package Repository & .NET 6/8 SDK
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-8.0

# Install OpenSSL 1.1 / OpenAL compatibility libraries (needed for native Hikvision SDK)
sudo apt install -y libssl-dev libopenal1
```

---

### Step 2: Clone the Application & Set Permissions

```bash
# Create project folder
sudo mkdir -p /var/www/ezviz-cameras
sudo chown -R $USER:www-data /var/www/ezviz-cameras

# Clone repo
git clone <your-git-repo-url> /var/www/ezviz-cameras
cd /var/www/ezviz-cameras
```

---

### Step 3: Build the .NET Console Engine

```bash
cd /var/www/ezviz-cameras/console-app
dotnet publish -c Release -o /var/www/ezviz-cameras/console-app/publish

# Ensure native Hikvision shared libraries (.so) have execute permissions
chmod +x /var/www/ezviz-cameras/Hik.Api/HikvisionSDK/*.so*
chmod +x /var/www/ezviz-cameras/Hik.Api/HikvisionSDK/HCNetSDKCom/*.so*
```

---

### Step 4: Configure Laravel for Production

```bash
cd /var/www/ezviz-cameras/laravel

# Install production dependencies
composer install --no-dev --optimize-autoloader

# Setup .env
cp .env.example .env
php artisan key:generate

# Edit production configuration
nano .env
```

Ensure production values in `laravel/.env`:
```dotenv
APP_NAME="Ezviz Camera Manager"
APP_ENV=production
APP_DEBUG=false
APP_URL=https://your-domain.com

DB_CONNECTION=mysql
DB_HOST=127.0.0.1
DB_PORT=3306
DB_DATABASE=ezviz_cameras
DB_USERNAME=ezviz_user
DB_PASSWORD=your_secure_password

QUEUE_CONNECTION=database
CACHE_STORE=database
SESSION_DRIVER=database
```

Update `laravel/config/app.php` if using the published path:
```php
'ezviz-console' => "dotnet " . base_path("/../console-app/publish/ConsoleApp.dll") . ' camera ',
```

---

### Step 5: Database Setup & Migrations

If using MySQL/MariaDB:
```bash
sudo mysql -u root -e "CREATE DATABASE ezviz_cameras CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
sudo mysql -u root -e "CREATE USER 'ezviz_user'@'localhost' IDENTIFIED BY 'your_secure_password';"
sudo mysql -u root -e "GRANT ALL PRIVILEGES ON ezviz_cameras.* TO 'ezviz_user'@'localhost'; FLUSH PRIVILEGES;"
```

Run database migrations:
```bash
cd /var/www/ezviz-cameras/laravel
php artisan migrate --force
```

Create an admin user:
```bash
php artisan tinker
```
```php
\App\Models\User::create([
    'name' => 'Admin',
    'email' => 'admin@your-domain.com',
    'password' => 'YourAdminPassword123'
]);
exit
```

---

### Step 6: File Permissions & Directory Setup

```bash
cd /var/www/ezviz-cameras/laravel

# Create download directory
mkdir -p public/downloads storage/logs storage/framework/{cache,sessions,views}

# Set permissions
sudo chown -R www-data:www-data /var/www/ezviz-cameras/laravel/storage
sudo chown -R www-data:www-data /var/www/ezviz-cameras/laravel/bootstrap/cache
sudo chown -R www-data:www-data /var/www/ezviz-cameras/laravel/public/downloads
sudo chmod -R 775 /var/www/ezviz-cameras/laravel/storage
sudo chmod -R 775 /var/www/ezviz-cameras/laravel/bootstrap/cache
sudo chmod -R 775 /var/www/ezviz-cameras/laravel/public/downloads

# Optimize config and route cache
php artisan config:cache
php artisan route:cache
php artisan view:cache
```


---

### Step 7: Nginx Web Server Configuration

Create an Nginx configuration file:

```bash
sudo nano /etc/nginx/sites-available/ezviz-cameras.conf
```

Add the configuration:

```nginx
server {
    listen 80;
    listen [::]:80;
    server_name your-domain.com; # Replace with your domain or VPS IP
    root /var/www/ezviz-cameras/laravel/public;

    add_header X-Frame-Options "SAMEORIGIN";
    add_header X-Content-Type-Options "nosniff";

    index index.php index.html;
    charset utf-8;

    client_max_body_size 500M;

    # Video downloads location
    location /downloads/ {
        alias /var/www/ezviz-cameras/laravel/public/downloads/;
        autoindex off;
        sendfile on;
        tcp_nopush on;
    }

    location / {
        try_files $uri $uri/ /index.php?$query_string;
    }

    location = /favicon.ico { access_log off; log_not_found off; }
    location = /robots.txt  { access_log off; log_not_found off; }

    error_page 404 /index.php;

    location ~ \.php$ {
        fastcgi_pass unix:/var/run/php/php8.2-fpm.sock;
        fastcgi_param SCRIPT_FILENAME $realpath_root$fastcgi_script_name;
        include fastcgi_params;
        fastcgi_read_timeout 300;
    }

    location ~ /\.(?!well-known).* {
        deny all;
    }
}
```

Enable site and restart Nginx:
```bash
sudo ln -s /etc/nginx/sites-available/ezviz-cameras.conf /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

#### SSL with Let's Encrypt (Certbot)
```bash
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d your-domain.com
```

---

### Step 8: Configure Laravel Scheduler (Cron)

Edit server crontab for `www-data`:
```bash
sudo crontab -e -u www-data
```

Add the Laravel scheduler entry:
```cron
* * * * * cd /var/www/ezviz-cameras/laravel && /usr/bin/php artisan schedule:run >> /dev/null 2>&1
```

The scheduler automatically runs:
- `CheckCameraStatus` (every 15 min): Checks online/offline camera connectivity.
- `CheckCameraFootageStatus` (every 10 min): Queries recording index from cameras and schedules download jobs.
- `CheckStorageStatus` (hourly): Updates storage drive metrics.

---

### Step 9: Configure Supervisor for Queue & Camera Workers

#### 1. Laravel Queue Worker
Create `/etc/supervisor/conf.d/laravel-queue.conf`:

```ini
[program:laravel-queue]
process_name=%(program_name)s_%(process_num)02d
command=/usr/bin/php /var/www/ezviz-cameras/laravel/artisan queue:work --sleep=3 --tries=3 --max-time=3600
directory=/var/www/ezviz-cameras/laravel
autostart=true
autorestart=true
user=www-data
numprocs=2
redirect_stderr=true
stdout_logfile=/var/www/ezviz-cameras/laravel/storage/logs/queue.log
stopwaitsecs=3600
```

#### 2. Dedicated Camera Workers
The system runs a dedicated worker daemon for each camera (`php artisan camera:work {camera-id}`) to continuously process video download jobs.

Generate supervisor configs automatically for all registered cameras:
```bash
cd /var/www/ezviz-cameras/laravel
sudo php artisan generate_supervisor_configs
```

Or manually create individual supervisor configs (e.g. `/etc/supervisor/conf.d/camera-1.conf`):
```ini
[program:camera-1]
command=/usr/bin/php /var/www/ezviz-cameras/laravel/artisan camera:work 1
directory=/var/www/ezviz-cameras/laravel
user=www-data
autostart=true
autorestart=true
startretries=3
stopwaitsecs=30
stopsignal=TERM
redirect_stderr=true
stdout_logfile=/var/www/ezviz-cameras/laravel/storage/logs/camera-1.log
stdout_logfile_maxbytes=50MB
stdout_logfile_backups=5
```

Reload Supervisor:
```bash
sudo supervisorctl reread
sudo supervisorctl update
sudo supervisorctl start all
```

---

## 5. Camera Network & Firewall Configuration

### Network Access
- The VPS needs network line-of-sight to each camera's IP address.
- If cameras are on local networks behind NAT routers:
  - **Option A (Recommended)**: Establish a **VPN (WireGuard / OpenVPN / Tailscale)** connecting the VPS to the store/camera local LAN.
  - **Option B**: Configure port forwarding on the store router (forwarding port `8000` to the camera's local IP).
- Ensure port `8000` (Hikvision SDK communication port) is not blocked by local or VPS firewalls.

### VPS Firewall (UFW)
```bash
sudo ufw allow OpenSSH
sudo ufw allow 'Nginx Full'
sudo ufw enable
```

---

## 6. Testing & Verification

1. **Verify Camera CLI Connectivity**:
   ```bash
   dotnet /var/www/ezviz-cameras/console-app/publish/ConsoleApp.dll camera test <camera-ip> 8000 <username> <password>
   ```
2. **Verify Camera Worker**:
   ```bash
   php /var/www/ezviz-cameras/laravel/artisan camera:work <camera-id>
   ```
3. **Check Logs**:
   - Web application logs: `/var/www/ezviz-cameras/laravel/storage/logs/laravel.log`
   - Log Viewer Web Interface: `https://your-domain.com/log-viewer`
   - Camera Worker logs: `/var/www/ezviz-cameras/laravel/storage/logs/camera-<id>.log`
   - Queue worker logs: `/var/www/ezviz-cameras/laravel/storage/logs/queue.log`
   - Nginx error logs: `/var/log/nginx/error.log`

---

## 7. Useful Maintenance Commands

```bash
# Restart queue workers after updating code
cd /var/www/ezviz-cameras/laravel
php artisan queue:restart

# Clear and rebuild Laravel cache
php artisan optimize:clear
php artisan optimize

# Restart all supervisor processes
sudo supervisorctl restart all

# Check status of camera workers
sudo supervisorctl status
```

