# 📦 Camera Recording Sync System

*(Laravel + .NET Microservice)*

## 🎯 Scope (Current Phase)

**Included**

* Periodic camera recording discovery
* Automated recording download
* Progress & status sync to Laravel SQLite
* File browsing & download from Laravel
* 7-day archive & admin notification

**Explicitly Excluded (for now)**

* Live view
* Playback streaming
* Real-time camera control

---

## 🧱 Architecture Overview (Final)

```text
Laravel (Control + UI)
│
│ HTTP (Internal API)
▼
.NET Camera Worker (SDK Access)
│
│ Native SDK (.so / .dll)
▼
EZVIZ / Hikvision Cameras
```

* **Laravel owns SQLite**
* **.NET never touches DB directly**
* **.NET only reports status via HTTP**
* **Laravel is the single source of truth**

---

# 1️⃣ Task: Database & Data Model Preparation (Laravel)

### Goal

Prepare SQLite tables to track recordings, progress, and archive state.

---

### Micro Tasks

#### 1.1 Camera Table (already exists)

Ensure it has:

* [ ] `id`
* [ ] `ip`
* [ ] `port`
* [ ] `username`
* [ ] `password`
* [ ] `last_checked_at`
* [ ] `status` (online/offline/error)

---

#### 1.2 Camera Recordings Table

Create a new table: `camera_recordings`

**Fields**

* [ ] `id`
* [ ] `camera_id`
* [ ] `remote_file_id` (SDK file identifier)
* [ ] `start_time`
* [ ] `end_time`
* [ ] `file_size`
* [ ] `download_status` (`pending/downloading/completed/failed`)
* [ ] `download_progress` (0–100)
* [ ] `local_path`
* [ ] `error_message`
* [ ] `created_at`

---

#### 1.3 Archive Table

Create table: `video_archives`

**Fields**

* [ ] `id`
* [ ] `from_date`
* [ ] `to_date`
* [ ] `file_path`
* [ ] `size`
* [ ] `status` (`ready/downloaded/cleared`)
* [ ] `created_at`

---

# 2️⃣ Task: .NET Camera Worker Service (Core)

### Goal

A headless worker that **loops cameras**, finds recordings, and downloads them.

---

## 2.1 Service Skeleton

### Micro Tasks

* [ ] Create ASP.NET Core **Minimal API**
* [ ] Add `/health` endpoint
* [ ] Add `/sync/start` endpoint
* [ ] Add API key authentication
* [ ] Add graceful shutdown handling

---

## 2.2 Camera Fetch Loop

### Responsibility

Laravel tells .NET **which cameras exist**.

### Micro Tasks

* [ ] Create endpoint `/cameras`
* [ ] Laravel exposes `/internal/cameras/list`
* [ ] .NET fetches all cameras periodically
* [ ] Skip cameras recently checked

---

## 2.3 Recording Discovery (SDK)

### Responsibility

Ask camera **what recordings exist**.

### Micro Tasks

* [ ] Login to camera via SDK
* [ ] Query recordings by date range
* [ ] Normalize SDK response
* [ ] Ignore already-known recordings
* [ ] Return recording metadata to Laravel

---

## 2.4 Recording Download

### Responsibility

Download files **one by one**, resumable.

### Micro Tasks

* [ ] Request download from SDK
* [ ] Save to structured folder:

  ```
  storage/cameras/{camera_id}/YYYY/MM/DD/
  ```
* [ ] Track byte progress
* [ ] Handle network interruption
* [ ] Mark failed downloads

---

## 2.5 Status Reporting to Laravel

### Micro Tasks

* [ ] POST `/internal/recordings/update`
* [ ] Send:

  * status
  * progress
  * error (if any)
* [ ] Laravel updates SQLite

---

# 3️⃣ Task: Laravel ↔ .NET Communication Layer

### Goal

Reliable, secure internal communication.

---

### Micro Tasks

#### 3.1 Authentication

* [ ] Shared API key (`X-INTERNAL-KEY`)
* [ ] Reject non-authorized requests

---

#### 3.2 Laravel Internal APIs

Create routes under `/internal/*`:

* [ ] `/internal/cameras/list`
* [ ] `/internal/recordings/store`
* [ ] `/internal/recordings/update`

---

#### 3.3 Error Handling

* [ ] Retry failed requests
* [ ] Log failures
* [ ] Alert admin on repeated failures

---

# 4️⃣ Task: Laravel Frontend – File Browsing & Download

### Goal

Let admin **browse and download recordings** easily.

---

## 4.1 Recording Browser UI

### Micro Tasks

* [ ] Camera → Recordings list
* [ ] Filter by date
* [ ] Show download status
* [ ] Show file size

---

## 4.2 File Download Endpoint

### Micro Tasks

* [ ] Secure download route
* [ ] Stream file (no memory load)
* [ ] Permission checks
* [ ] Download logs

---

# 5️⃣ Task: Weekly Archive & Cleanup (Laravel Job)

### Goal

Control disk usage and notify admin.

---

## 5.1 Archive Job (Laravel Scheduler)

### Micro Tasks

* [ ] Select recordings older than 7 days
* [ ] Zip by date range
* [ ] Save archive
* [ ] Record in `video_archives`

---

## 5.2 Admin Notification

### Micro Tasks

* [ ] Email admin
* [ ] Include:

  * Date range
  * Archive size
  * Download link

---

## 5.3 Cleanup Flow

### Micro Tasks

* [ ] Admin downloads archive
* [ ] Admin clicks “Clear Archive”
* [ ] Laravel deletes old recordings
* [ ] Marks archive as `cleared`

---

# 6️⃣ Execution Order (Very Important)

Implement **strictly in this order**:

1. Laravel DB + models
2. .NET service skeleton
3. Camera list fetch
4. Recording discovery
5. Recording download
6. Status sync
7. Laravel UI
8. Archive job
