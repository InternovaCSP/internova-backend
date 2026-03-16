# internova-backend

ASP.NET Core Web API for **Internova – University Internship & Industry Matching Portal**

---

## Project Structure

```
internova-backend/
├── Internova.Core/           ← Domain layer (no external dependencies)
│   ├── Entities/User.cs
│   └── Interfaces/IUserRepository.cs
├── Internova.Infrastructure/ ← Data access + external services
│   ├── Data/AppDbContext.cs
│   ├── Repositories/UserRepository.cs
│   └── DependencyInjection/ServiceRegistration.cs
└── Internova.Api/            ← Web API entry point
    ├── Controllers/HealthController.cs
    ├── Program.cs
    └── appsettings.json
```

### Dependency Direction

```
Internova.Api
    ├── → Internova.Core          (domain contracts)
    └── → Internova.Infrastructure
              └── → Internova.Core
```

`Internova.Core` has **zero** external dependencies — pure domain logic only.

---

## NuGet Packages

| Project | Package | Version |
|---|---|---|
| Infrastructure | Pomelo.EntityFrameworkCore.MySql | 9.0.0 |
| Infrastructure | Microsoft.EntityFrameworkCore.Design | 9.0.0 |
| Infrastructure | Azure.Storage.Blobs | 12.27.0 |
| Infrastructure | BCrypt.Net-Next | 4.1.0 |
| Api | Swashbuckle.AspNetCore | 10.1.4 |

> **Note on EF Core version:** All EF Core packages are pinned to **9.0.0** to align with Pomelo's constraints. Fully forward-compatible with .NET 10.

---

## ⚙️ Local Run (All Platforms)

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- MySQL 8+ running locally (or via Docker)

### 1. Set up local configuration

```bash
# From the repo root
cp Internova.Api/appsettings.Development.example.json Internova.Api/appsettings.Development.json
```

> **Windows (PowerShell):**
> ```powershell
> Copy-Item Internova.Api\appsettings.Development.example.json Internova.Api\appsettings.Development.json
> ```

Then open `appsettings.Development.json` and update your MySQL password:

```json
{
  "ConnectionStrings": {
    "Default": "server=localhost;port=3306;database=internova_db;user=root;password=YOUR_MYSQL_PASSWORD"
  }
}
```

> `appsettings.Development.json` is gitignored — your credentials will never be committed.

### 2. Build

```bash
dotnet build
```

Expected output: `Build succeeded` with **0 errors, 0 warnings**.

### 3. Run EF Core migrations (first time only)

From the `internova-backend/` directory:

```bash
dotnet ef migrations add InitialCreate --project Internova.Infrastructure --startup-project Internova.Api
dotnet ef database update --project Internova.Infrastructure --startup-project Internova.Api
```

### 4. Run the API

```bash
dotnet run --project Internova.Api
```

### 5. Verify

| Endpoint | URL |
|---|---|
| Swagger UI | http://localhost:5000/swagger |
| Health check | GET http://localhost:5000/api/health/ping → `{"status":"ok"}` |

---

## Key Design Decisions

- **Primary constructors** (C# 12) used for `AppDbContext` and `UserRepository` — minimal boilerplate
- **BCrypt hashing** via `BCrypt.Net-Next` — use `BCrypt.HashPassword()` / `BCrypt.Verify()` in the auth service
- **Azure.Storage.Blobs** installed and ready for document/CV upload features
- **`AsNoTracking()`** used in read-only queries for better EF Core performance
- **Unique index on Email** enforced at both DB and model level

---

## Verified Build & Runtime

| Check | Result |
|---|---|
| `dotnet build` | ✅ 0 errors, 0 warnings |
| Swagger UI at `/swagger` | ✅ HTTP 200, UI loads |
| `GET /api/health/ping` | ✅ `{"status":"ok"}` |
