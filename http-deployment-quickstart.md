# HTTP Deployment Quick Start Guide

This guide provides streamlined instructions for deploying the Hikvision Service application using HTTP with a real IP address.

## Quick Deployment Steps

### 1. Build the Application

```bash
# Navigate to project directory
cd /path/to/ezviz-camera/main-app

# Publish the application
dotnet publish HikvisionService/HikvisionService.csproj -c Release -o ./publish
```

### 2. Configure for HTTP with Real IP

Create or modify `appsettings.Production.json` in the publish directory:

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
  "AllowedHosts": "*"
}
```

> **Important**: Replace `YOUR_SERVER_IP` with your server's actual IP address.

### 3. Disable HTTPS Redirection

Edit `Program.cs` in the publish directory to comment out or remove the HTTPS redirection line:

```csharp
// Find and comment out this line:
// app.UseHttpsRedirection();
```

### 4. Run the Application

#### For Linux/macOS:

```bash
cd /path/to/publish/directory
export ASPNETCORE_ENVIRONMENT=Production
./HikvisionService
```

#### For Windows:

```cmd
cd C:\path\to\publish\directory
set ASPNETCORE_ENVIRONMENT=Production
HikvisionService.exe
```

### 5. Set Up as a Service (Linux)

Create a systemd service file for automatic startup:

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

Save as `/etc/systemd/system/hikvision-service.service` and enable:

```bash
sudo systemctl enable hikvision-service
sudo systemctl start hikvision-service
```

### 6. Verify Deployment

Access the application at:
```
http://YOUR_SERVER_IP:5000
```

## Security Note

Since you're using HTTP (not HTTPS), consider these security measures:

1. Deploy on a private network or behind a firewall
2. Configure IP restrictions to only accept connections from trusted sources
3. Ensure authentication is properly configured
4. Regularly update the application and server with security patches

## Troubleshooting

If the application doesn't start or you can't access it:

1. Check logs in the `logs` directory
2. Verify the server's firewall allows traffic on port 5000
3. Ensure the IP address in configuration is correct
4. Check if another service is already using port 5000