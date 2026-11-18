# Authentication (Login) & Secured Access Architecture

## Overview

Internova secures user authentication and API authorization using **salted password hashing** (`PasswordHasher<User>`), **stateless HMAC-SHA256 JWT Bearer tokens**, **Role-Based Access Control (RBAC)**, and **policy-based authorization** (`RequireCompanyApproval`).

---

## End-to-End Authentication & Request Flow

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant FE as React Frontend (AuthContext + Axios)
    participant API as Internova.Api (AuthController / Middleware)
    participant Auth as AuthService
    participant DB as SQL Server (dbo.[User] / Company_Profile)

    rect rgb(240, 248, 255)
    Note over User,DB: 1. Login Flow (POST /api/auth/login)
    User->>FE: Enter email & password
    FE->>API: POST /api/auth/login { email, password }
    API->>DB: GetByEmailAsync(email.Trim().ToLowerInvariant())
    DB-->>API: User record (Id, Email, Role, PasswordHash) or null
    alt User not found OR Password verification fails
        API->>Auth: VerifyPassword(user, hash, password)
        Auth-->>API: false
        API-->>FE: 401 Unauthorized { error: "Invalid email or password." }
    else Valid credentials
        API->>Auth: VerifyPassword(...) -> true
        API->>Auth: GenerateJwtToken(user) [HS256, 8h expiry]
        Auth-->>API: Signed JWT
        API-->>FE: 200 OK { token, userId, email, role }
        FE->>FE: Store token in localStorage ("internova_token")\nDecode claims via jwt-decode
    end
    end

    rect rgb(245, 255, 245)
    Note over User,DB: 2. Authenticated & Policy-Guarded Request (e.g. POST /api/internships)
    FE->>API: POST /api/internships\nHeader: Authorization: Bearer <JWT>
    API->>API: JwtBearer Middleware validates:\n- Signature (Jwt:Key)\n- Issuer, Audience, Lifetime
    alt Token missing, expired, or tampered
        API-->>FE: 401 Unauthorized
    else Token valid (ClaimsPrincipal populated)
        API->>API: Role Check: [Authorize(Roles = "Company")]
        API->>DB: CompanyApprovalHandler checks\nCompany_Profile.Status == Active
        alt Company status != Active
            API-->>FE: 403 Forbidden
        else Company is Active
            API->>API: ClaimsPrincipalExtensions.TryGetUserId()
            API->>DB: Persist internship for companyId
            API-->>FE: 201 Created
        end
    end
    end
```

---

## Security Controls & Token Lifecycle

```mermaid
flowchart TD
    Startup["Startup Guard\n(EnsureJwtConfigured)"] -->|"Blocks boot if Jwt:Key missing\nor placeholder"| ConfigOK["Jwt:Key Loaded\n(User Secrets / Env)"]

    Reg["POST /api/auth/register"] --> RoleGuard{"Role in\n{Student, Company}?"}
    RoleGuard -- "No (e.g. Admin)" --> BadReq["400 Bad Request\n(Blocks Admin self-registration)"]
    RoleGuard -- "Yes" --> DupCheck{"Email exists\nin dbo.[User]?"}
    DupCheck -- "Yes" --> Conflict["409 Conflict"]
    DupCheck -- "No" --> Hash["AuthService.HashPassword\n(ASP.NET Identity PBKDF2)"]
    Hash --> SaveUser["Insert User (lowercase email)"]

    Protected["Protected Endpoint / SignalR Hub"] --> JwtCheck{"Valid Bearer JWT?\n(Issuer, Audience, Exp, HS256)"}
    JwtCheck -- "No" --> Unauth["401 Unauthorized"]
    JwtCheck -- "Yes" --> Rbac{"Role & Policy\nSatisfied?"}
    Rbac -- "No" --> Forbid["403 Forbidden"]
    Rbac -- "Yes" --> ClaimExtract["ClaimsPrincipalExtensions\nGetUserId() / TryGetUserId()"]
    ClaimExtract -->|"Invalid/missing user_id claim"| ExHandler["GlobalExceptionHandler -> 401"]
    ClaimExtract -->|"Valid int userId"| Controller["Execute Controller Action"]
```

---

## JWT Claims & Token Configuration

| Claim | Source | Purpose |
|---|---|---|
| `user_id` | `user.Id.ToString()` | Primary numeric user identifier extracted by `ClaimsPrincipalExtensions` and `NotificationHub` |
| `ClaimTypes.Email` & `email` | `user.Email` | User email address |
| `ClaimTypes.Role` | `user.Role` | Enforces `[Authorize(Roles = "...")]` (`Student`, `Company`, `Admin`) |
| `sub` | `user.Id.ToString()` | Standard JWT subject claim |
| `jti` | `Guid.NewGuid()` | Unique token identifier |
| `exp` | `DateTime.UtcNow.AddHours(8)` | 8-hour token expiration enforced via `ValidateLifetime = true` |

---

## Key Security Properties Implemented

1. **Startup Fail-Fast Secret Guard**: `EnsureJwtConfigured()` in [ServiceCollectionExtensions.cs](file:///C:/Users/sandi/Documents/Internova/internova-backend/Internova.Api/Presentation/Extensions/ServiceCollectionExtensions.cs) prevents the API from starting if `Jwt:Key` is missing or left as a template placeholder.
2. **Account Enumeration Prevention**: `AuthController.Login` returns the exact same `401 Unauthorized` (`"Invalid email or password."`) whether the email does not exist or the password is wrong.
3. **Privilege Escalation Prevention**: `AuthController.Register` restricts self-registration strictly to `Student` and `Company` roles; `Admin` accounts cannot self-register.
4. **Password Hashing**: `AuthService` uses ASP.NET Core Identity's `PasswordHasher<User>` (salted PBKDF2 with SHA-256/SHA-512 depending on format version).
5. **Authorization Policies**: Beyond role checks, [CompanyApprovalRequirement.cs](file:///C:/Users/sandi/Documents/Internova/internova-backend/Internova.Api/Presentation/Authorization/CompanyApprovalRequirement.cs) enforces that a `Company` must have `CompanyStatus.Active` in `dbo.Company_Profile` before creating internships (`[Authorize(Roles = "Company", Policy = "RequireCompanyApproval")]`).
6. **Sanitized Error Responses**: [GlobalExceptionHandler.cs](file:///C:/Users/sandi/Documents/Internova/internova-backend/Internova.Api/Presentation/Middleware/GlobalExceptionHandler.cs) catches unhandled exceptions centrally, logging full details server-side while returning a generic `{ "error": "Internal server error" }` to clients (and `401` on `UnauthorizedAccessException`).
7. **SignalR Authentication**: [NotificationHub.cs](file:///C:/Users/sandi/Documents/Internova/internova-backend/Internova.Api/Presentation/Hubs/NotificationHub.cs) requires `[Authorize]` and isolates real-time notification delivery to per-user groups (`User_{userId}`).
