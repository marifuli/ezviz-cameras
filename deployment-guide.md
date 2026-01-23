# Deployment Guide for Hikvision Service

This guide provides step-by-step instructions for deploying the Hikvision Service application to a production environment using HTTP with a real IP address.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Server Requirements](#server-requirements)
3. [Preparing the Application](#preparing-the-application)
4. [Deployment Options](#deployment-options)
   - [Option 1: Self-hosted with Kestrel](#option-1-self-hosted-with-kestrel)
   - [Option 2: Using a Reverse Proxy (Nginx)](#option-2-using-a-reverse-proxy-nginx)
   - [Option 3: Using IIS on Windows](#option-3-using-iis-on-windows)
5. [Database Configuration](#database-configuration)
6. [Environment Configuration](#environment-configuration)
7. [Security Considerations](#security-considerations)
8. [Monitoring and Maintenance](#monitoring-and-maintenance)
9. [Troubleshooting](#troubleshooting)

## Prerequisites

- .NET 6.0 SDK or Runtime installed on the server
- SQLite (included in the application)
- Git (optional, for source control)
- Access to server with a static IP address
- Basic knowledge of server administration

## Server Requirements

- OS: Windows Server, Linux (Ubuntu, Debian, CentOS), or macOS
- RAM: Minimum 2GB (4GB recommended)
- CPU: 2+ cores recommended
- Storage: At least 10GB free space (more depending on video storage needs)
- Network: Stable internet connection with access to Hikvision cameras

## Preparing the Application

### 1. Build the Application

```bash
# Clone the repository (if using Git)
git clone <repository-url>
cd ezviz-camera/main-app

# Publish the application
dotnet publish HikvisionService/HikvisionService.csproj -c Release -o ./publish
```

### 2. Prepare the Deployment Package

The published output will be in the `./publish` directory. This contains all the necessary files for deployment, including:

- Application binaries
- Configuration files
- Hikvision SDK libraries

## Deployment Options

### Option 1: Self-hosted with Kestrel

This is the simplest deployment method, using the built-in Kestrel web server.

#### Steps:

1. Transfer the published files to your server
2. Configure the application to use HTTP with your server's IP address

Create a `appsettings.Production.json` file in the publish directory:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://YOUR_SERVER_IP:5000"
      }
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./database.sqlite"
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

3. Run the application:

```bash
cd /path/to/publish/directory
export ASPNETCORE_ENVIRONMENT=Production
./HikvisionService
```

4. For running as a service on Linux, create a systemd service file:

```
[Unit]
Description=Hikvision Service
After=network.target

[Service]
WorkingDirectory=/path/to/publish/directory
ExecStart=/path/to/publish/directory/HikvisionService
Restart=always
RestartSec=10
SyslogIdentifier=hikvision-service
User=<your-user>
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

Save this file as `/etc/systemd/system/hikvision-service.service` and enable it:

```bash
sudo systemctl enable hikvision-service
sudo systemctl start hikvision-service
```

### Option 2: Using a Reverse Proxy (Nginx)

For better performance and security, you can use Nginx as a reverse proxy in front of Kestrel.

#### Steps:

1. Install Nginx:

```bash
# Ubuntu/Debian
sudo apt update
sudo apt install nginx

# CentOS/RHEL
sudo yum install nginx
```

2. Configure Kestrel to listen on localhost only:

In `appsettings.Production.json`:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  }
}
```

3. Create an Nginx configuration file:

```nginx
server {
    listen 80;
    server_name YOUR_SERVER_IP;

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

    # Increase max upload size for video files
    client_max_body_size 500M;
}
```

Save this file as `/etc/nginx/sites-available/hikvision-service` and enable it:

```bash
sudo ln -s /etc/nginx/sites-available/hikvision-service /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

4. Set up the application as a service as described in Option 1.

### Option 3: Using IIS on Windows

If deploying to a Windows server, you can use IIS.

#### Steps:

1. Install the .NET 6.0 Hosting Bundle on the server
2. Install IIS and the ASP.NET Core Module
3. Create a new IIS site pointing to the publish directory
4. Configure the application pool to use No Managed Code
5. Set up the binding to use your server's IP address on port 80

## Database Configuration

The application uses SQLite by default, which doesn't require additional setup. The database file will be created automatically in the application directory.

If you want to change the database location, update the connection string in `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/path/to/your/database.sqlite"
  }
}
```

## Environment Configuration

### Production Settings

Create or modify `appsettings.Production.json` with production-specific settings:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./database.sqlite"
  },
  "BackgroundServices": {
    "CameraHealthCheck": {
      "IntervalMinutes": 10
    },
    "StorageMonitoring": {
      "IntervalMinutes": 30
    },
    "DownloadJob": {
      "IntervalSeconds": 60,
      "MaxConcurrentDownloads": 2
    }
  },
  "AllowedHosts": "*"
}
```

### Environment Variables

You can also configure the application using environment variables:

```bash
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Data Source=./database.sqlite"
```

## Security Considerations

Since you're deploying with HTTP (not HTTPS), consider the following security measures:

1. **Network Security**: Deploy the application on a private network or behind a firewall
2. **IP Restrictions**: Configure your server to only accept connections from trusted IP addresses
3. **Authentication**: Ensure the built-in authentication is properly configured
4. **Regular Updates**: Keep the application and server updated with security patches

## Monitoring and Maintenance

### Logging

The application uses Serilog for logging. Logs are stored in the `logs` directory. Monitor these logs regularly for errors or issues.

### Backup

Regularly backup the SQLite database file to prevent data loss.

```bash
# Example backup script
cp /path/to/database.sqlite /path/to/backups/database-$(date +%Y%m%d).sqlite
```

### Health Checks

The application includes background services that monitor camera health and storage. Check the logs and dashboard regularly to ensure everything is functioning correctly.

## Troubleshooting

### Common Issues

1. **Application won't start**:
   - Check logs in the `logs` directory
   - Verify the correct .NET runtime is installed
   - Ensure all required Hikvision SDK files are present

2. **Cannot connect to cameras**:
   - Verify network connectivity to the cameras
   - Check camera credentials in the application
   - Ensure the Hikvision SDK libraries are properly deployed

3. **Database errors**:
   - Check file permissions on the database file
   - Verify the connection string is correct
   - Ensure the application has write access to the database directory

4. **Performance issues**:
   - Monitor server resources (CPU, memory, disk)
   - Adjust background service intervals in configuration
   - Consider increasing server resources if needed

### Getting Help

If you encounter issues not covered in this guide, check:
- Application logs in the `logs` directory
- Server logs (e.g., `/var/log/syslog` on Linux)
- IIS logs (if using IIS)
- Nginx logs (if using Nginx)