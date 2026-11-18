# System Architecture

## Overview

**Internova** is a university internship and industry matching portal composed of a React single-page application (`internova-frontend`) and a .NET 10 ASP.NET Core Web API (`internova-backend`) structured with **single-project Clean Architecture**, backed by SQL Server / Azure SQL, Azure Blob Storage, Daily.co, and SignalR real-time notifications.

---

## High-Level System Architecture

```mermaid
flowchart LR
    subgraph Client["Client Tier (Browser)"]
        UI["React 19 + Vite SPA\n(internova-frontend)"]
        Axios["Axios Client\n(/api/* + Bearer JWT)"]
        SignalRClient["SignalR Client\n(/hubs/notifications)"]
        UI --> Axios
        UI --> SignalRClient
    end

    subgraph API["Internova.Api (.NET 10 - Single-Project Clean Architecture)"]
        subgraph Pres["Presentation Layer"]
            Middleware["HTTP Pipeline\n(ExceptionHandler, CORS,\nForwardedHeaders, JWT Auth)"]
            Controllers["REST Controllers (15)"]
            Hub["NotificationHub\n(SignalR)"]
            Policy["CompanyApprovalHandler\n(RequireCompanyApproval)"]
        end

        subgraph App["Application Layer"]
            DTOs["Feature DTOs"]
            Interfaces["Repository & Service\nInterfaces"]
        end

        subgraph Dom["Domain Layer"]
            Entities["Entities & Enums"]
        end

        subgraph Infra["Infrastructure Layer"]
            Repos["Dapper Repositories (12)\n+ DbConnectionFactory"]
            DbInit["DatabaseInitializer\n(Executes embedded scripts/schema.sql)"]
            Services["AuthService, BlobStorageService,\nMeetingService, EmailService"]
        end

        Middleware --> Controllers
        Middleware --> Hub
        Controllers --> Policy
        Pres --> App
        Pres --> Infra
        Infra --> App
        App --> Dom
        Infra --> Dom
    end

    subgraph External["Data & External Services"]
        SQL[("SQL Server /\nAzure SQL")]
        Blob[("Azure Blob Storage\n(resumes / profiles)")]
        Daily["Daily.co REST API\n(Video Rooms)"]
    end

    Axios -->|"HTTPS REST / JSON"| Middleware
    SignalRClient -->|"WebSockets / SignalR"| Middleware
    Repos -->|"Dapper / SqlConnection"| SQL
    DbInit -->|"Startup Schema & Admin Seed"| SQL
    Services -->|"Azure.Storage.Blobs"| Blob
    Services -->|"HttpClient + Bearer Key"| Daily
```

---

## Single-Project Clean Architecture Dependency Rule

Inside `Internova.Api`, dependencies flow strictly inward toward `Domain`:

```mermaid
flowchart TD
    Pres["Presentation\n(Controllers, Hubs, Middleware, Extensions, Authorization, Requests)"]
    Infra["Infrastructure\n(Dapper Repositories, Services, DbConnectionFactory, DatabaseInitializer)"]
    App["Application\n(DTOs, Repository & Service Interfaces)"]
    Dom["Domain\n(Entities, Enums - zero framework/DB dependencies)"]
    Tests["tests/Internova.Tests\n(Unit & Integration Tests)"]

    Pres --> App
    Pres --> Infra
    Infra --> App
    App --> Dom
    Infra --> Dom
    Tests --> Pres
    Tests --> App
    Tests --> Dom
```

---

## Request Processing Pipeline (`Internova.Api`)

Every incoming HTTP request traverses the middleware pipeline configured in `Internova.Api/Program.cs`:

```mermaid
flowchart LR
    Req["Incoming Request"] --> Ex["UseExceptionHandler\n(GlobalExceptionHandler)"]
    Ex --> Swag["Swagger UI\n(/swagger)"]
    Swag --> Fwd["UseForwardedHeaders\n(X-Forwarded-For / Proto)"]
    Fwd --> Https["UseHttpsRedirection\n(Non-Dev only)"]
    Https --> Cors["UseCors\n(ViteDev / DeployedCorsPolicy)"]
    Cors --> AuthN["UseAuthentication\n(JWT Bearer)"]
    AuthN --> AuthZ["UseAuthorization\n(Roles & RequireCompanyApproval)"]
    AuthZ --> Route["Controllers (/api/*)\n& SignalR (/hubs/notifications)"]
```

---

## Core Functional Modules

| Module | Controller | Repository / Service | Key Tables |
|---|---|---|---|
| **Authentication & Account** | `AuthController` | `IUserRepository`, `IAuthService` | `[User]` |
| **User & Student Profiles** | `ProfilesController`, `StudentsController`, `SettingsController` | `IUserRepository`, `IStudentProfileRepository`, `IBlobStorageService` | `[User]`, `Student_Profile` |
| **Company & Admin Approvals** | `AdminCompaniesController`, `AnalyticsController` | `ICompanyProfileRepository`, `IInternshipRepository`, `IAnalyticsRepository` | `Company_Profile`, `Internship`, `Internship_Application` |
| **Internships & Applications** | `InternshipsController`, `ApplicationsController`, `InterviewsController` | `IInternshipRepository`, `IInternshipApplicationRepository`, `IInterviewRepository` | `Internship`, `Internship_Application`, `Interview` |
| **Competitions & Projects** | `CompetitionsController`, `ProjectsController` | `ICompetitionRepository`, `IProjectRepository` | `Competition`, `Project`, `Project_Participation` |
| **Seminars & Breakout Rooms** | `SeminarsController`, `BreakoutRoomsController` | `ISeminarRepository`, `IBreakoutRoomRepository`, `IMeetingService`, `IEmailService` | `Seminar_Request`, `Seminar_Vote`, `BreakoutRoom` |
| **Notifications & Health** | `NotificationsController`, `NotificationHub`, `HealthController` | `INotificationRepository` | `Notification` |
