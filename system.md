
---

## 3. Core Components

### 3.1 Laravel Web Application

**Responsibilities**
- User authentication & authorization
- Camera list and metadata
- Time-range selection for footage
- File listing and downloads
- UI for live streaming
- Scheduling background download requests

**Does NOT**
- Communicate with cameras directly
- Load native SDK libraries
- Handle long-running device connections

**Technology**
- Laravel (PHP)
- SqLite
- Redis (queues / cache)
- Nginx

---

### 3.2 Camera SDK Microservice

**Purpose**
A long-running internal service that interacts directly with the Hikvision SDK.

**Technology**
- C# (.NET 6 LTS)
- ASP.NET Core (Minimal API)
- Hikvision / EZVIZ SDK (.so)
- FFmpeg

**Responsibilities**
- Maintain persistent camera sessions
- Download recordings by time range
- Handle live stream sessions
- Pipe raw video to FFmpeg
- Generate HLS streams (.m3u8)
- Report job status to Laravel

**Key Characteristics**
- Runs as a daemon / systemd service
- Stateless API, stateful internal workers
- No public internet exposure

---

## 4. Communication Flow

### 4.1 Footage Download

1. User selects camera + time range
2. Laravel sends request to SDK service
3. SDK service downloads footage
4. File saved to storage
5. Metadata stored in database
6. User downloads file via Laravel

---

### 4.2 Live Streaming

1. User requests live view
2. Laravel requests SDK service to start stream
3. SDK pulls live feed via SDK
4. FFmpeg converts to HLS
5. Browser plays `.m3u8` stream

---

## 5. Why This Architecture

### Separation of Concerns
- Web app stays fast and stable
- Native SDK runs in controlled environment

### Scalability
- SDK service can be scaled independently
- Multiple camera workers possible

### Stability
- SDK crashes do not affect web users
- Automatic restart via systemd

---

## 6. Deployment Model

### Production Server
- Ubuntu x86_64
- Laravel + SDK service on same private network
- SDK service bound to localhost or internal IP

### Development
- Same Linux environment as production
- No SDK execution on macOS

---

## 7. Security Notes

- SDK service not publicly exposed
- Internal authentication (API keys / IP allowlist)
- No camera credentials stored in frontend

---

## 8. Future Extensions

- Horizontal scaling of SDK service
- Queue-based download orchestration
- S3-compatible storage
- Stream concurrency limits

