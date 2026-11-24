# Direct Video Upload via Pre-Signed URLs (ASP.NET Core + Azure Blob Storage / Azurite)

A system design reference implementation demonstrating **decoupled, direct-to-storage media ingestion**. Instead of routing heavy video streams through an application server, the ASP.NET Core backend issues short-lived **Shared Access Signature (SAS) URLs**, allowing clients to upload large video files directly to object storage.

---

## Architecture & System Design Overview

### Why Direct-to-Storage?
In a traditional upload flow, the client sends a multi-hundred-megabyte or multi-gigabyte video to the ASP.NET Core API, which buffers or streams the payload and forwards it to object storage. This doubles network bandwidth, consumes server memory/threads, and creates a bottleneck under concurrent uploads.

With the **Pre-Signed / SAS URL (Valet Key) Pattern**:
- **Control Plane (ASP.NET Core API):** Authenticates the user, validates file metadata, generates a unique object path, and signs a time-limited, permission-scoped SAS URI. Zero video bytes touch the web server.
- **Data Plane (Azure Blob Storage / Azurite):** Handles the heavy binary `PUT` transfer directly from the client browser, scaling independently from the API tier.

### Upload Sequence

```text
┌─────────┐                                  ┌──────────────────┐                      ┌─────────────────────────┐
│ Browser │                                  │ ASP.NET Core API │                      │ Azure Blob / Azurite    │
└────┬────┘                                  └────────┬─────────┘                      └────────────┬────────────┘
     │                                                │                                             │
     │ 1. POST /api/videos/upload-url                 │                                             │
     │    { fileName, contentType }                   │                                             │
     ├───────────────────────────────────────────────>│                                             │
     │                                                │ 2. Validate & generate                      │
     │                                                │    10-min Write-Only SAS URL                │
     │ 3. 200 OK { uploadUrl, blobName }              │                                             │
     │<───────────────────────────────────────────────┤                                             │
     │                                                │                                             │
     │ 4. PUT <uploadUrl> (Direct binary video stream + x-ms-blob-type: BlockBlob)                  │
     ├─────────────────────────────────────────────────────────────────────────────────────────────>│
     │                                                │                                             │
     │ 5. 201 Created                                 │                                             │
     │<─────────────────────────────────────────────────────────────────────────────────────────────┤
```

---

## Tech Stack

- **Backend:** ASP.NET Core (.NET)
- **Storage SDK:** `Azure.Storage.Blobs`
- **Local Object Store:** Azurite (Azure Storage Emulator) on `http://127.0.0.1:10000`
- **Storage Inspection:** Microsoft Azure Storage Explorer
- **Frontend:** Vanilla HTML5 + Fetch API (`wwwroot/index.html`)

---

## Project Structure

```text
PreSignedUrlDemo/
├── Controllers/                # API endpoint generating SAS URLs
├── Properties/
│   └── launchSettings.json     # HTTP & HTTPS launch profiles
├── wwwroot/
│   └── index.html              # Client upload UI (Fetch PUT directly to Azurite)
├── appsettings.Development.json
└── Program.cs                  # DI setup, Azurite CORS bootstrap & static file hosting
```

---

## Prerequisites

1. **.NET SDK** (8.0 or later)
2. **Azurite Emulator** (bundled with Azure Storage Explorer, Visual Studio, VS Code Azurite extension, or via `npm install -g azurite` / Docker)
3. **Azure Storage Explorer** (optional, for inspecting uploaded blobs locally)

---

## Getting Started (Local Development)

### 1. Start Azurite Blob Service
Start the local Azurite emulator so it listens on default port `10000`:
- **Windows Start Menu:** Search for and launch **Start Azurite Blob Service** (installed with Azure Storage Explorer).
- **CLI:**
  ```bash
  azurite-blob --silent --location ./azurite-data
  ```
- **Docker:**
  ```bash
  docker run -p 10000:10000 mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0
  ```

### 2. Verify Configuration
Ensure `appsettings.Development.json` uses the standard Azurite development connection string:

```json
{
  "ConnectionStrings": {
    "AzureStorage": "UseDevelopmentStorage=true"
  }
}
```

### 3. Run the ASP.NET Core App (HTTP Profile)
Run the application using the **`http`** profile (so the browser origin and local Azurite `http://127.0.0.1:10000` share the same HTTP scheme without Mixed Content blocks):

```bash
dotnet run --launch-profile http
```

Open the `http://localhost:<port>` URL printed in the console.

### 4. Test an Upload
1. Choose a `.mp4` or video file on the web page and click **Upload Video**.
2. Open Browser DevTools (`F12`) → **Network** tab to verify:
   - `POST /api/videos/upload-url` returns a signed URL pointing to `127.0.0.1:10000`.
   - `PUT` sends the video payload directly to Azurite and returns `201 Created`.
3. Open **Azure Storage Explorer** → **Local & Attached** → **Storage Accounts** → **(Emulator - Default Ports)** → **Blob Containers** → **`videos`** to inspect the uploaded file.

---

## Key Implementation Notes

- **CORS Configuration:** Because the browser uploads from `http://localhost:<port>` to `http://127.0.0.1:10000`, `Program.cs` configures CORS rules on startup in `Development` allowing `PUT, OPTIONS, GET, HEAD` and custom headers (`x-ms-blob-type`).
- **Required Azure Header:** Direct single-request uploads to Azure Blob Storage must include `x-ms-blob-type: BlockBlob`.
- **Collision Prevention:** Blobs are prefixed with a server-generated `Guid` (`{guid}/{fileName}`) so users cannot overwrite existing objects.

---
