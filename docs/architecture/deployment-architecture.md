# Deployment Architecture

## Overview

Internova uses separate repositories and CI/CD pipelines for the frontend and backend, deploying to **Azure Static Web Apps** and **Azure App Service** respectively, backed by **Azure SQL Database**, **Azure Blob Storage**, and **Daily.co**.

---

## CI/CD & Cloud Deployment Diagram

```mermaid
flowchart TB
    subgraph GitHub["GitHub & GitHub Actions"]
        FE_Repo["internova-frontend Repo"]
        BE_Repo["internova-backend Repo"]

        FE_CI["Frontend CI (ci.yml)\nPush/PR -> main, develop\nnpm ci -> npm run lint -> npm run build"]
        FE_CD["Azure SWA Deploy\nPush/PR -> staging\nInjects VITE_API_BASE_URL\nUploads dist/"]

        BE_CI[".NET CI (dotnet-ci.yml)\nPush/PR -> develop, staging\ndotnet restore -> build (Release)\ndotnet test --filter Category!=LocalDb"]
        BE_CD["Backend Deploy (backend-deploy.yml)\nPush -> staging\ndotnet publish Internova.Api -c Release"]

        FE_Repo --> FE_CI
        FE_Repo --> FE_CD
        BE_Repo --> BE_CI
        BE_Repo --> BE_CD
    end

    subgraph Azure["Microsoft Azure Cloud"]
        SWA["Azure Static Web Apps\n(React SPA - dist/)"]
        AppSvc["Azure App Service\n(internovaServer - .NET 10)\nSSL Termination + ForwardedHeaders"]
        AzSQL[("Azure SQL Database\n(Schema auto-verified by\nDatabaseInitializer at startup)")]
        AzBlob[("Azure Blob Storage\n- resumes (private)\n- profiles (public blob)")]
    end

    subgraph ThirdParty["External SaaS"]
        Daily["Daily.co API\n(Breakout Room Meetings)"]
    end

    User["End User Browser"] -->|"HTTPS"| SWA
    User -->|"HTTPS /api/* & WSS /hubs/notifications"| AppSvc

    FE_CD -->|"Azure/static-web-apps-deploy@v1"| SWA
    BE_CD -->|"azure/webapps-deploy@v3"| AppSvc

    AppSvc -->|"SQL / TLS (DefaultConnection)"| AzSQL
    AppSvc -->|"Azure.Storage.Blobs"| AzBlob
    AppSvc -->|"HTTPS + DailyCo:ApiKey"| Daily
```

---

## Environment Comparison (Local Development vs. Azure Staging/Production)

| Component | Local Development | Azure Cloud (`staging` / Production) |
|---|---|---|
| **Frontend Host** | Vite dev server (`http://localhost:5173`) with `/api` proxy to `http://localhost:5128` | Azure Static Web Apps (`dist/`) with `VITE_API_BASE_URL` set to `https://internovaserver-bcc5adh0bqbtbjdv.eastasia-01.azurewebsites.net` |
| **Backend Host** | Kestrel (`http://localhost:5128` / `https://localhost:7044`) | Azure App Service (`internovaServer`, East Asia) behind reverse proxy SSL termination |
| **CORS Policy** | `ViteDev` (`localhost:5173`, `localhost:5174`, `localhost:5128`) | `DeployedCorsPolicy` (`AllowedOrigins` array from App Settings) |
| **HTTPS Redirection** | Disabled when `ASPNETCORE_ENVIRONMENT=Development` | Enabled (`app.UseHttpsRedirection()` + `UseForwardedHeaders`) |
| **Database** | Local SQL Server / SQLEXPRESS (`appsettings.Development.json`) | Azure SQL Database via App Service Connection Strings |
| **Secrets** | `.NET User Secrets` (`internova-api-secrets`) | Azure App Service Application Settings / Connection Strings |

---

## Startup & Database Bootstrap Flow

When the backend starts on Azure App Service (or locally), `Program.cs` executes the following sequence before accepting traffic:

```mermaid
flowchart TD
    Start["App Startup (Program.cs)"] --> Guard{"Jwt:Key configured\n& not placeholder?"}
    Guard -- "No" --> Exit["Log fatal error & Environment.Exit(1)"]
    Guard -- "Yes" --> DI["Register API Services\n& Infrastructure (Dapper)"]
    DI --> EnvCheck{"Environment == Testing?"}
    EnvCheck -- "Yes" --> Listen["Start Kestrel / TestHost"]
    EnvCheck -- "No" --> Init["DatabaseInitializer.InitializeAsync"]
    Init --> Tables["Execute embedded scripts/schema.sql\n(tables, columns & indexes)"]
    Tables --> Seed["Seed default Admin user\n(admin@internova.com) if missing"]
    Seed --> Listen
```

---

## Required Runtime Configuration

| Key | Purpose | Where Configured |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server / Azure SQL connection string | `appsettings.Development.json` / Azure Connection Strings |
| `Jwt:Key` | 32+ char symmetric HMAC-SHA256 signing key | `dotnet user-secrets` / Azure App Settings |
| `Jwt:Issuer` | Token issuer (`Internova`) | `appsettings.json` / User Secrets |
| `Jwt:Audience` | Token audience (`InternovaUsers`) | `appsettings.json` / User Secrets |
| `AzureBlob:ConnectionString` | Azure Storage account connection string | User Secrets / Azure App Settings |
| `AzureBlob:ContainerName` | Resume container name (default `resumes`) | `appsettings.json` |
| `DailyCo:ApiKey` | Daily.co API key (falls back to mock URL if empty) | User Secrets / Azure App Settings |
| `AllowedOrigins` | Allowed frontend origins in non-dev environments | Azure App Settings (`AllowedOrigins__0`, etc.) |
