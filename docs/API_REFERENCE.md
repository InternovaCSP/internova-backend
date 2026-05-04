# Internova API Reference

Welcome to the Internova API documentation. This document provides a comprehensive overview of the RESTful API that powers the Internova University Internship & Industry Matching Portal.

## Architecture Overview

The backend is built using **ASP.NET Core 10** following a layered architecture:

- **Internova.Api**: The entry point, containing Controllers, Hubs (SignalR), and Middleware.
- **Internova.Core**: The heart of the application, containing Entities, Interfaces, DTOs, and Enums.
- **Internova.Infrastructure**: Handles data persistence (ADO.NET) and external service integrations.

## Authentication & Authorization

The API uses **JWT (JSON Web Tokens)** for secure communication.

### Flow
1. **Login**: Send credentials to `/api/auth/login`.
2. **Receive Token**: On success, you receive a JWT.
3. **Authorized Requests**: Include the token in the `Authorization` header as a Bearer token:
   `Authorization: Bearer <your_token_here>`

### Roles
The system supports three primary roles:
- `Student`: Can browse internships, apply, and manage their profile.
- `Company`: Can post internships, review applications, and manage company details.
- `Admin`: Full system access, including user management and platform analytics.

## Standard Error Responses

The API uses standard HTTP status codes:

| Status Code | Description |
| :--- | :--- |
| `200 OK` | Success. |
| `201 Created` | Resource created successfully. |
| `204 No Content` | Success, but no data returned. |
| `400 Bad Request` | Validation error or invalid input. |
| `401 Unauthorized` | Missing or invalid authentication token. |
| `403 Forbidden` | Authenticated, but lacking required permissions/role. |
| `404 Not Found` | The requested resource does not exist. |
| `500 Internal Server Error` | Something went wrong on the server. |

## Core Modules

### 1. Authentication (`/api/auth`)
- `POST /register`: Create a new Student or Company account.
- `POST /login`: Authenticate and receive a JWT.
- `GET /me`: Get details of the current authenticated user.
- `POST /change-password`: Update your password.

### 2. Internships (`/api/internships`)
- `GET /`: List all active/published internships.
- `GET /{id}`: Get details of a specific internship.
- `POST /`: Create a new internship (Company only).
- `PUT /{id}`: Update an internship (Company owner only).
- `DELETE /{id}`: Remove an internship (Company owner only).

### 3. Applications (`/api/applications`)
- `POST /apply`: Submit an application (Student only).
- `GET /student`: List applications submitted by the current student.
- `GET /company`: List applications received by the current company.
- `PATCH /{id}/status`: Update application status (Company only).

## Real-time Notifications

The API provides real-time updates via **SignalR**.
- **Hub URL**: `/api/hubs/notifications`
- **Events**: `ReceiveNotification` (sent to specific users when status changes).

## Developer Tools

### Swagger UI
Access the interactive API explorer at:
`http://localhost:5128/swagger` (Local Development)

**Enhancements:**
- **Full Type Names**: To avoid schema collisions (e.g., duplicate `UpdateStatusRequest` classes), the API is configured to use full namespace names in the Swagger schema.
- **Interactive Testing**: You can use the "Authorize" button to paste your JWT token and test protected endpoints directly.

### Postman
You can import the Swagger definition into Postman:
1. Open Postman -> Import.
2. Enter the URL: `http://localhost:5128/swagger/v1/swagger.json`.
3. Postman will automatically generate a collection with all endpoints, including documentation.
