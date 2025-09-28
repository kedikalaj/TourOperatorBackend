 # TourPricingUploader

**A Clean-Architecture .NET 8 backend** for ingesting large CSV pricing & seat-allocation files from tour operators.
Features: JWT authentication, role-based auth (Admin / TourOperator), CSV streaming + validation, high-performance bulk insert (SqlBulkCopy), SignalR progress notifications, Redis caching, structured logging with Serilog, EF Core migrations, seeded users (lightweight custom user table — *no* ASP.NET Identity).

---

## Overview

This service ingests daily pricing & seat allocation CSVs per route and booking class. Files can be large (many rows). The system streams and validates CSV rows, emits real-time progress to the uploading client via SignalR, and performs batched bulk inserts into SQL Server for performance. Admins can query uploaded pricing (with Redis caching for repeat calls). Authentication uses stateless JWTs and roles are enforced in controllers.

---

## Quick feature list

* .NET 8 Web API (Clean Architecture)
* Streaming CSV parse using **CsvHelper**
* Batched `SqlBulkCopy` for high-throughput inserts
* SignalR hub `/hubs/upload` to notify client progress (per connectionId)
* JWT Bearer authentication (role claims: `Admin`, `TourOperator`)
* Lightweight seeded users (no Identity)
* Redis: caching + token blacklist (logout)
* Serilog structured logging
* Pagination for GET endpoints
* EF Core migrations

---

## Architecture diagram & layers

Layers (top-down):

1. **Clients** — Web/Mobile (React / React Native)
2. **WebApi Layer** — Controllers, SignalR hub, authentication, request validation
3. **Application Layer** — Service interfaces, DTOs, business logic, validation
4. **Infrastructure Layer** — EF Core `AppDbContext`, SqlBulkCopy insertion, Redis cache, CsvHelper parsing, Serilog logging
5. **Database** — SQL Server (PricingRecords, Users, TourOperators, __EFMigrationsHistory)

#Architecture diagram

<img width="205" height="307" alt="Screenshot Image Sep 28, 2025, 01_50_08 PM" src="https://github.com/user-attachments/assets/248b8108-675d-49b2-83ca-fa1a70bd9f6e" />

---

## Design decisions & trade-offs

**Why SQL Server + EF Core?**

* Structured, relational pricing data with indexing, queries, and transactions — good fit.
* EF Core simplifies migrations and schema evolution.
* For heavy write throughput, combined EF Core + `SqlBulkCopy` gives the best of both worlds.

**Why CsvHelper?**

* Robust parsing, mapping, streaming, culture-aware parsing (decimal/dates).

**Why SignalR?**

* Provides real-time progress updates for long-running uploads.

**Why Redis?**

* Caching of read-heavy admin queries and storing revoked JWT JTI keys (logout blacklist).

**Trade-offs**

* Using relational DB and EF Core is simpler to start with but if your dataset grows to enormous scale, a time-series DB or data warehouse might be preferable.
* SignalR across multiple instances requires a backplane (Redis) to scale; that adds operational complexity.
* Seeded users (custom table) is lightweight but lacks built-in identity features (password resets, lockouts) — trade convenience for simplicity.

---

## Key components / important files

* `Domain/Entities/User.cs` — custom user entity (no Identity)
* `Domain/Entities/PricingRecord.cs` — main pricing row entity
* `Infrastructure/Data/AppDbContext.cs` — EF Core DbContext + seeded data via `OnModelCreating`
* `Api/Hubs/UploadProgressHub.cs` — SignalR hub
* `Api/Services/CsvProcessingService.cs` — streaming CSV validation + SqlBulkCopy batching with SignalR progress updates
* `Api/Controllers/AuthController.cs` — register/login/logout (JWT issuance & blacklist)
* `Api/Controllers/PricingUploadController.cs` — multipart upload endpoint
* `Api/Controllers/AdminDataController.cs` — paged admin GET endpoint (with Redis caching)
* `Program.cs` — DI wiring, authentication/authorization, Redis, Serilog, EF DbContext
* `postman_collection.yaml` — example requests

---

## Getting started (local dev)

### Prerequisites

* .NET 8 SDK
* SQL Server (local or container) with an accessible connection string
* Redis (local or container)

---

### Configuration (`appsettings.json`)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=TourOperator;Trusted_Connection=True;",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Key": "S4gq7z9Wdz5hY3G8nFhVo2Rflgf6Xz7lrJvVlf2uVZ4=",
    "Issuer": "https://localhost:7071",
    "Audience": "https://localhost:7071"
  },
  "AllowedHosts": "*"
}

```

Store secrets (like `Jwt:Key`) in environment variables or KeyVault for production.


## Seeding Users (Lightweight, Non-Identity)

This project seeds two users in the `OnModelCreating` method of `AppDbContext` for testing purposes. These users are created with static values for email, role, and hashed password. The users are seeded using the `modelBuilder.Entity<User>().HasData(...)` method.

### Seeded Users:

#### Admin User
- **Email**: `admin@example.com`
- **Password**: `Safepwd@1` (hashed using BCrypt)
- **Role**: `Admin`
- **CreatedAt**: A fixed timestamp

#### TourOperator User
- **Email**: `operator@example.com`
- **Password**: `Safepwd@2` (hashed using BCrypt)
- **Role**: `TourOperator`
- **CreatedAt**: A fixed timestamp




### SignalR hub

* Hub endpoint: `/hubs/upload`
* Client flow:

  1. Connect to SignalR hub, obtain `connectionId`
  2. Upload CSV to `/api/touroperators/{id}/pricing-upload` including `connectionId`
  3. Server sends progress messages using `_hub.Clients.Client(connectionId).SendAsync("Progress", "message")`

**JS client minimal example:**

```js
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>SignalR Upload Progress</title>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/7.0.5/signalr.min.js"></script>
</head>
<body>
    <h1>SignalR Upload Progress Example</h1>
    <button id="connectBtn">Connect to Hub</button>
    <p id="connectionId">Connection ID: not connected</p>
    <p id="progress">Progress: N/A</p>

    <script>
        let connection;

        document.getElementById("connectBtn").addEventListener("click", async () => {
const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:7071/hubs/upload", { withCredentials: false })
    .configureLogging(signalR.LogLevel.Information)
    .build();


            connection.on("Progress", (message) => {
                document.getElementById("progress").innerText = "Progress: " + message;
            });

            try {
                await connection.start();
                console.log("Connected to SignalR Hub");

                const connectionId = await connection.invoke("GetConnectionId");
                document.getElementById("connectionId").innerText = "Connection ID: " + connectionId;
                console.log("My connection ID:", connectionId);

            } catch (err) {
                console.error(err);
            }
        });
    </script>
</body>
</html>

```

<img width="1361" height="760" alt="image" src="https://github.com/user-attachments/assets/d5cf48f6-ee53-481c-a172-b086f2db02aa" />


---

## CSV format & samples

**Header must match these names exactly (case-sensitive mapping in `PricingCsvMap`)**:

```
RouteCode,SeasonCode,EconomySeats,BusinessSeats,Date,EconomyPrice,BusinessPrice
```

### Small sample CSV

`pricing-sample.csv`:

```csv
RouteCode,SeasonCode,EconomySeats,BusinessSeats,Date,EconomyPrice,BusinessPrice
TIR-ROM,SUMMER24,120,20,2024-06-01,150.00,450.00
TIR-ROM,SUMMER24,118,20,2024-06-02,155.00,460.00
TIR-ROM,SUMMER24,115,18,2024-06-03,160.00,470.00
TIR-MIL,SUMMER24,130,25,2024-06-01,140.00,400.00
TIR-MIL,SUMMER24,125,25,2024-06-02,145.00,410.00
TIR-MIL,SUMMER24,120,24,2024-06-03,150.00,420.00
ROM-PAR,AUTUMN24,100,15,2024-09-10,180.00,500.00
ROM-PAR,AUTUMN24,98,15,2024-09-11,185.00,510.00
ROM-PAR,AUTUMN24,95,14,2024-09-12,190.00,520.00
```
---

## Caching (Redis)

* Admin GET responses are cached using `IDistributedCache` (StackExchangeRedis).
* Token revocation uses Redis: when a user logs out, server extracts token `jti` and stores a blacklist key `bl_jti:{jti}` with TTL up to token expiry. JWT validation includes checking Redis for that `jti`.
* The **GetPricing** endpoint also leverages Redis caching. Results are cached per `tourOperatorId`, `page`, and `pageSize` with a 30-minute expiration, reducing database load and improving response times. Cache hits and misses are logged for monitoring.

---

## Logging & observability

* Serilog used for structured logs; `UseSerilogRequestLogging()` in `Program.cs`.
* CSV processing logs:

  * Upload start
  * Row-level validation warnings (bad date/price/etc.)
  * Batched bulk insert completions
  * Total processed rows and errors

---

## Bulk insert & performance notes

* CSV is processed in streaming fashion (row-by-row) and rows are added to `DataTable`. When batch size reaches threshold (e.g., 5k rows) the `DataTable` is written to SQL Server with `SqlBulkCopy`.
* Benefits:

  * Low memory footprint (you only keep a batch in memory).
  * Fast writes.
* Considerations:

  * Maintain mapping between DataTable columns and DB columns.
  * Handle duplicate logic / deduping if necessary (e.g., unique index on `{TourOperatorId, Date, RouteCode}` and decide upsert/delete strategy).
  * For extremely large files, consider background queue (e.g., an enqueued job with status endpoint) to fully decouple upload request from processing.

---

## Postman collection

A `postman_collection.yaml` (included in repo) contains example requests:

* Register
* Login
* Upload (multipart)
* Admin GET

Sample entry (upload):

```yaml
- name: Upload pricing CSV (multipart)
  request:
    method: POST
    header:
      - key: Authorization
        value: "Bearer {{jwt}}"
    url:
      raw: "{{baseUrl}}/api/touroperators/{{tourOperatorId}}/pricing-upload"
    body:
      mode: formdata
      formdata:
        - key: file
          type: file
          src: "./sample.csv"
        - key: connectionId
          value: "REPLACE_WITH_SIGNALR_CONNECTION_ID"
```

---

## To-do / improvements

* Add rate limiting & upload quotas.
* Add CI/CD Pipeline
* Add refresh tokens for JWT rotation.

