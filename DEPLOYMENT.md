# Camera Monitoring Dashboard Deployment Guide

This guide provides instructions for deploying the Camera Monitoring Dashboard application to a production environment on a VPS (Virtual Private Server).

## System Requirements

- **Operating System**: Ubuntu 22.04 LTS or Debian 11 (recommended)
- **RAM**: Minimum 2GB
- **CPU**: 2 cores or more
- **Storage**: At least 20GB for the system + additional storage for camera footage
- **Network**: Stable internet connection with access to your cameras

## Prerequisites

- SSH access to your VPS
- Root or sudo privileges
- Domain name (optional but recommended for secure access)

## Step 1: Install Required Software

Connect to your VPS via SSH and install the required software:

```bash
# Update package lists
sudo apt update
sudo apt upgrade -y

# Install .NET SDK and Runtime
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt update
sudo apt install -y dotnet-sdk-8.0
sudo apt install -y aspnetcore-runtime-8.0

# Install Nginx as a reverse proxy
sudo apt install -y nginx

# Install SQLite (if not already installed)
sudo apt install -y sqlite3

# Install additional tools
sudo apt install -y git curl
```

## Step 2: Create Application Directory

Create a directory for the application:

```bash
sudo mkdir -p /var/www/camera-dashboard
sudo mkdir -p /var/www/camera-dashboard/data
sudo mkdir -p /var/www/camera-dashboard/logs
sudo mkdir -p /var/www/camera-dashboard/downloads
```

## Step 3: Deploy the Application

### Option 1: Deploy from Source Control

```bash
# Clone the repository
git clone https://your-repository-url.git /tmp/camera-dashboard

# Build and publish the application
cd /tmp/camera-dashboard
dotnet publish -c Release -o /var/www/camera-dashboard

# Set proper permissions
sudo chown -R www-data:www-data /var/www/camera-dashboard
```

### Option 2: Deploy from Build Machine

On your development machine:

```bash
# Build and publish the application
dotnet publish -c Release -o ./publish

# Create a deployment package
cd ./publish
zip -r ../camera-dashboard.zip .
```

Transfer the zip file to your VPS using SCP:

```bash
scp camera-dashboard.zip user@your-vps-ip:/tmp/
```

On your VPS:

```bash
# Extract the deployment package
sudo unzip /tmp/camera-dashboard.zip -d /var/www/camera-dashboard

# Set proper permissions
sudo chown -R www-data:www-data /var/www/camera-dashboard
```

## Step 4: Configure the Application

Create or update the appsettings.json file:

```bash
sudo nano /var/www/camera-dashboard/appsettings.json
```

Update the configuration with your settings:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/var/www/camera-dashboard/data/database.sqlite"
  },
  "BackgroundServices": {
    "CameraHealthCheck": {
      "IntervalMinutes": 5
    },
    "StorageMonitoring": {
      "IntervalMinutes": 15
    },
    "DownloadJob": {
      "IntervalSeconds": 60,
      "MaxConcurrentDownloads": 2
    }
  },
  "AllowedHosts": "*"
}
```

## Step 5: Configure Nginx as a Reverse Proxy

Create an Nginx configuration file:

```bash
sudo nano /etc/nginx/sites-available/camera-dashboard
```

Add the following configuration:

```nginx
server {
    listen 80;
    server_name your-domain.com;  # Replace with your domain or server IP

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Enable the site and restart Nginx:

```bash
sudo ln -s /etc/nginx/sites-available/camera-dashboard /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

## Step 6: Set Up SSL with Let's Encrypt (Optional but Recommended)

If you have a domain name, secure your application with SSL:

```bash
# Install Certbot
sudo apt install -y certbot python3-certbot-nginx

# Obtain and install SSL certificate
sudo certbot --nginx -d your-domain.com
```

## Step 7: Create a Systemd Service

Create a service file to run the application as a background service:

```bash
sudo nano /etc/systemd/system/camera-dashboard.service
```

Add the following configuration:

```ini
[Unit]
Description=Camera Monitoring Dashboard
After=network.target

[Service]
WorkingDirectory=/var/www/camera-dashboard
ExecStart=/usr/bin/dotnet /var/www/camera-dashboard/HikvisionService.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=camera-dashboard
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```

Enable and start the service:

```bash
sudo systemctl enable camera-dashboard.service
sudo systemctl start camera-dashboard.service
```

## Step 8: Configure Storage Directories

Create directories for camera footage and ensure proper permissions:

```bash
# Create storage directories
sudo mkdir -p /data/camera-footage
sudo chown -R www-data:www-data /data/camera-footage
```

## Step 9: Monitor the Application

Check the application status:

```bash
# Check service status
sudo systemctl status camera-dashboard.service

# View application logs
sudo journalctl -u camera-dashboard.service -f

# Monitor system resources
sudo apt install -y htop
htop
```

## Step 10: Set Up Automatic Updates (Optional)

Create a script for updating the application:

```bash
sudo nano /usr/local/bin/update-camera-dashboard.sh
```

Add the following content:

```bash
#!/bin/bash
cd /tmp
git clone https://your-repository-url.git camera-dashboard-update
cd camera-dashboard-update
dotnet publish -c Release -o ./publish

sudo systemctl stop camera-dashboard.service
sudo cp -r ./publish/* /var/www/camera-dashboard/
sudo chown -R www-data:www-data /var/www/camera-dashboard
sudo systemctl start camera-dashboard.service

cd /tmp
rm -rf camera-dashboard-update
```

Make the script executable:

```bash
sudo chmod +x /usr/local/bin/update-camera-dashboard.sh
```

## Troubleshooting

### Application Won't Start

Check the logs for errors:

```bash
sudo journalctl -u camera-dashboard.service -n 100
```

### Database Issues

Check if the database file exists and has proper permissions:

```bash
ls -la /var/www/camera-dashboard/data/
sudo chown www-data:www-data /var/www/camera-dashboard/data/database.sqlite
```

### Nginx Configuration Issues

Check Nginx error logs:

```bash
sudo tail -f /var/log/nginx/error.log
```

### Camera Connection Issues

Ensure your VPS can reach the cameras:

```bash
ping camera-ip-address
telnet camera-ip-address camera-port
```

## Backup Strategy

Set up regular backups of your database and configuration:

```bash
# Create a backup script
sudo nano /usr/local/bin/backup-camera-dashboard.sh
```

Add the following content:

```bash
#!/bin/bash
TIMESTAMP=$(date +"%Y%m%d-%H%M%S")
BACKUP_DIR="/var/backups/camera-dashboard"

# Create backup directory if it doesn't exist
mkdir -p $BACKUP_DIR

# Backup database
cp /var/www/camera-dashboard/data/database.sqlite $BACKUP_DIR/database-$TIMESTAMP.sqlite

# Backup configuration
cp /var/www/camera-dashboard/appsettings.json $BACKUP_DIR/appsettings-$TIMESTAMP.json

# Remove backups older than 30 days
find $BACKUP_DIR -type f -name "*.sqlite" -mtime +30 -delete
find $BACKUP_DIR -type f -name "*.json" -mtime +30 -delete
```

Make the script executable and set up a cron job:

```bash
sudo chmod +x /usr/local/bin/backup-camera-dashboard.sh
sudo crontab -e
```

Add the following line to run the backup daily at 2 AM:

```
0 2 * * * /usr/local/bin/backup-camera-dashboard.sh
```

## Security Considerations

1. **Firewall Configuration**: Set up a firewall to restrict access to your VPS:

```bash
sudo apt install -y ufw
sudo ufw allow ssh
sudo ufw allow http
sudo ufw allow https
sudo ufw enable
```

2. **Regular Updates**: Keep your system and application updated:

```bash
sudo apt update && sudo apt upgrade -y
```

3. **Secure User Authentication**: Set up strong passwords and consider using SSH keys instead of password authentication.

4. **Application Security**: Ensure your application uses HTTPS and has proper authentication mechanisms.

## Conclusion

Your Camera Monitoring Dashboard should now be deployed and running on your VPS. The application will automatically monitor camera status, manage download jobs, and track storage usage as configured.

For any issues or questions, please refer to the troubleshooting section or contact the development team.