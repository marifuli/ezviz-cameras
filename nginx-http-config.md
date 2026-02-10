# Nginx HTTP Configuration Guide

This guide provides detailed instructions for deploying the Hikvision Service application using Nginx as a reverse proxy with HTTP and a real IP address.

## Why Use Nginx?

Using Nginx as a reverse proxy offers several advantages:

1. Better performance handling static files
2. Protection against common web vulnerabilities
3. Ability to host multiple applications on the same server
4. Load balancing capabilities if needed
5. Better control over request limits and timeouts

## Deployment Steps

### 1. Install Nginx

#### On Ubuntu/Debian:

```bash
sudo apt update
sudo apt install nginx
```

#### On CentOS/RHEL:

```bash
sudo yum install nginx
```

### 2. Configure the Application

Create or modify `appsettings.Production.json` in your publish directory:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./database.sqlite"
  },
  "AllowedHosts": "*"
}
```

> Note: The application listens only on localhost, as Nginx will handle external connections.

### 3. Configure Nginx

Create a new Nginx configuration file:

```nginx
server {
    listen 80;
    server_name stex-cameras.prosoftbd.com;  # Change to your domain or use _ for all

    location / {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Save this file as `/etc/nginx/sites-available/ezviz-camera`

### 4. Enable the Nginx Configuration

```bash
# Create a symbolic link to enable the site
sudo ln -s /etc/nginx/sites-available/ezviz-camera /etc/nginx/sites-enabled/

# Test the configuration
sudo nginx -t

# If the test is successful, restart Nginx
sudo systemctl restart nginx
```

### 5. Configure Firewall (if enabled)

Allow HTTP traffic through your firewall:

#### For UFW (Ubuntu):

```bash
sudo ufw allow 80/tcp
```

#### For FirewallD (CentOS):

```bash
sudo firewall-cmd --permanent --add-service=http
sudo firewall-cmd --reload
```

### 6. Run the Application as a Service

Create a systemd service file:

```
[Unit]
Description=Hikvision Service
After=network.target

[Service]
WorkingDirectory=/path/to/publish/directory
ExecStart=/path/to/publish/directory/HikvisionService
Restart=always
RestartSec=10
SyslogIdentifier=ezviz-camera
User=<your-user>
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

Save this file as `/etc/systemd/system/ezviz-camera.service` and enable it:

```bash
sudo systemctl enable ezviz-camera
sudo systemctl start ezviz-camera
```

### 7. Verify the Deployment

Access your application at:
```
http://YOUR_SERVER_IP
```

## Monitoring and Logs

### Nginx Logs

Nginx logs are typically located at:
- Access logs: `/var/log/nginx/access.log`
- Error logs: `/var/log/nginx/error.log`

Monitor these logs for HTTP errors or issues:

```bash
sudo tail -f /var/log/nginx/error.log
```

### Application Logs

The application logs are in the `logs` directory of your publish folder:

```bash
tail -f /path/to/publish/directory/logs/log-*.txt
```

## Performance Tuning

For better performance with video streaming and large file uploads:

```nginx
# Add to the http section of your nginx.conf
http {
    # Increase buffer sizes for large requests
    client_body_buffer_size 10M;
    client_max_body_size 500M;
    
    # Optimize for video streaming
    proxy_buffers 16 4k;
    proxy_buffer_size 2k;
    
    # Timeouts
    proxy_connect_timeout 300s;
    proxy_send_timeout 300s;
    proxy_read_timeout 300s;
}
```

## Troubleshooting

1. **502 Bad Gateway errors**:
   - Check if the application is running: `sudo systemctl status ezviz-camera`
   - Verify the application is listening on port 5000: `netstat -tulpn | grep 5000`

2. **Permission issues**:
   - Ensure Nginx has permission to proxy to your application
   - Check SELinux settings if applicable: `sudo setsebool -P httpd_can_network_connect 1`

3. **Connection refused**:
   - Verify the application is running and listening on the correct port
   - Check for any firewall rules blocking internal communication