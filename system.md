This is a Camera Monitoring, Download Jobs & Storage Dashboard project. 

## Goal

Build a **production-ready dashboard** that shows:

* Camera online/offline status (with last online time)
* Which cameras are actively feeding data
* Background download job progress (live)
* Failed downloads with manual retry
* Disk usage per storage drive (used / free / total)
* A quick list of offline cameras on the dashboard

This is a **full-stack ASP.NET Core web application** running on a **dedicated VPS with one or more storage drives**.

---

## 1️⃣ Camera Status & Activity Tracking

### Backend

Extend existing Camera model to include:

* `IsOnline` (bool)
* `LastOnlineAt` (DateTime?)
* `LastDownloadedAt` (DateTime?)

Implement a **recurring background job** that:

* Checks each camera via `Hik.Api`
* Marks camera:

  * Online if reachable
  * Offline if unreachable
* Updates `LastOnlineAt` when reachable
* Fetches available footage metadata
* Compares footage timestamps with `LastDownloadedAt`
* Detects if camera has **new data available**

Rules:

* No filesystem checks to determine camera state
* DB is the source of truth
* Job must be safe to run repeatedly

---

## 2️⃣ Footage Download Job with Progress

### Backend

Implement a **download job system** that:

* Starts a download job when new footage is detected
* Uses `Hik.Api` download with progress callback
* Updates progress (percentage or bytes) in database
* Writes files atomically (temp → final)
* Updates `LastDownloadedAt` only after success

Persist job info in DB:

* JobId
* CameraId
* File name / time range
* Status (Pending / Downloading / Completed / Failed)
* ProgressPercent
* StartedAt
* FinishedAt
* ErrorMessage (nullable)

If a job fails:

* Status = Failed
* Error saved
* Job remains retryable

---

## 3️⃣ Manual Job Control

### Backend

Expose endpoints to:

* Retry a failed download job
* Manually trigger camera health check

Rules:

* Retrying must reuse the same job record
* No duplicate jobs for the same footage range

---

## 4️⃣ Storage Drive Monitoring (VERY IMPORTANT)

### Backend

Support **one or multiple storage drives**.

Create a `StorageDrive` model:

* Id
* Name
* RootPath
* TotalSpace
* UsedSpace
* FreeSpace
* LastCheckedAt

Implement a background service that:

* Periodically checks disk usage using OS APIs
* Updates used/free/total space in DB
* Blocks new downloads if disk is critically full

Thresholds:

* ≥70% → Warning
* ≥85% → Critical
* ≥95% → Stop downloads

---

## 5️⃣ Dashboard UI (Main Requirement)

### Dashboard Home Page

Must show:

#### A. System Summary Cards

* Total cameras
* Online cameras
* Offline cameras
* Active download jobs
* Failed download jobs

#### B. Offline Cameras List (Small Panel)

* Camera name
* LastOnlineAt
* Status indicator

#### C. Storage Usage Panel

For each drive:

* Drive name
* Total space
* Used space
* Free space
* Usage progress bar (green / yellow / red)

👉 **This must be visible at a glance**

---

## 6️⃣ Camera Index Page

Each camera row must show:

* Camera name
* Online / Offline status
* LastOnlineAt
* LastDownloadedAt
* Current activity (Idle / Downloading)

---

## 7️⃣ Download Jobs Page

Show:

* Running downloads with live progress
* Failed downloads with error message
* Retry button per failed job

Progress must:

* Update automatically (polling or SignalR)
* Reflect real download state from DB


## 9️⃣ Acceptance Criteria

This task is complete when:

* Dashboard shows real-time camera & disk status
* Offline cameras are immediately visible
* Disk used/free space is accurate per drive
* Download progress is visible live
* Failed jobs can be retried manually
* Camera “last online” time updates correctly
* System survives restart without losing job state
