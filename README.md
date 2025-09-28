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
- **CreatedAt**: A fixed timestamp (`fixedDateTime`)

#### TourOperator User
- **Email**: `operator@example.com`
- **Password**: `Safepwd@2` (hashed using BCrypt)
- **Role**: `TourOperator`
- **CreatedAt**: A fixed timestamp (`fixedDateTime`)


