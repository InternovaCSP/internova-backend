# internova-backend

ASP.NET Core Web API (.NET 10) for **Internova – University Internship & Industry Matching Portal**, structured using **Clean Architecture inside a single API project**.

---

## Project Structure

```text
internova-backend/
├── Internova.slnx                             # Internova.Api + tests/Internova.Tests
├── scripts/
│   ├── schema.sql                             # Single canonical SQL Server schema (embedded in API)
│   └── seed_data.sql                          # Local development seed data
├── docs/
│   ├── Guides/
│   └── architecture/                          # System, deployment, and authentication architecture docs
├── Internova.Api/                             # Single Web API project (Clean Architecture layers)
│   ├── Domain/                                # Innermost layer (zero framework/DB dependencies)
│   │   ├── Entities/                          # User, StudentProfile, CompanyProfile, Internship, etc.
│   │   └── Enums/                             # ApplicationStatus, CompanyStatus
│   ├── Application/                           # Contracts & DTOs (depends only on Domain)
│   │   ├── DTOs/                              # Feature DTO files (AuthDtos, InternshipDtos, etc.)
│   │   └── Interfaces/                        # Repository and service contracts
│   ├── Infrastructure/                      # Dapper data access & external integrations
│   │   ├── Data/                              # DbConnectionFactory, DatabaseInitializer
│   │   ├── Repositories/                      # 12 Dapper repositories
│   │   ├── Services/                          # AuthService, BlobStorageService, EmailService, MeetingService
│   │   └── DependencyInjection.cs             # AddInfrastructure()
│   ├── Presentation/                          # HTTP & SignalR boundary
│   │   ├── Authorization/                     # CompanyApprovalRequirement
│   │   ├── Controllers/                       # 15 thin REST controllers
│   │   ├── Extensions/                        # ClaimsPrincipalExtensions, ServiceCollectionExtensions
│   │   ├── Hubs/                              # NotificationHub
│   │   ├── Middleware/                        # GlobalExceptionHandler
│   │   └── Requests/                          # Multipart form payloads
│   └── Program.cs                             # Minimal composition root
└── tests/
    └── Internova.Tests/                       # xUnit unit & integration tests
        ├── Helpers/
        ├── Integration/
        └── Unit/
```

### Dependency Direction

```text
Presentation ──► Application ◄── Infrastructure
                      │                │
                      ▼                │
                    Domain ◄───────────┘
```

---

## Local Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server / LocalDB (or Azure SQL)

### 1. Configure Secrets & Connection String

```powershell
cd Internova.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "<your-32+-character-random-secret>"
dotnet user-secrets set "Jwt:Issuer" "Internova"
dotnet user-secrets set "Jwt:Audience" "InternovaUsers"
```

Copy `Internova.Api/appsettings.Development.example.json` to `Internova.Api/appsettings.Development.json` and update `ConnectionStrings:DefaultConnection`.

### 2. Build & Test

```powershell
dotnet build --configuration Release
dotnet test --configuration Release --filter "Category!=LocalDb"
```

### 3. Run the API

```powershell
dotnet run --project Internova.Api
```

- Swagger UI: `/swagger`
- Health check: `GET /api/health/ping`
