# Eventloop

**Eventloop** is a college event registration and check-in platform. Clubs publish events, students register and receive a secure QR ticket, and organizers scan tickets at the door. When an event reaches maximum capacity, registrations queue onto a sequential waitlist and automatically promote when a spot becomes available.

---

## Architecture Overview

Eventloop is built as a clean, intermediate-level monorepo with separation of concerns:

```
eventloop/
├── backend/
│   ├── Eventloop.sln
│   ├── Eventloop.Core/           # Domain Entities, Enums, Interfaces
│   ├── Eventloop.Infrastructure/ # EF Core DbContext, Fluent Configurations, Migrations
│   ├── Eventloop.Api/            # ASP.NET Core Web API, Controllers, Middleware, Swagger
│   ├── Eventloop.Functions/      # Azure Functions (Isolated Worker Model) for background jobs
│   └── Eventloop.Tests/          # xUnit, FluentAssertions unit & integration test suite
├── frontend/                     # React + TypeScript (Vite) [Scaffolded in Phase 7]
├── docker-compose.yml            # Local development dependencies (SQL Server + Azurite)
└── README.md
```

### Tech Stack
- **Backend Framework**: ASP.NET Core Web API (.NET 8 LTS)
- **Data Access**: Entity Framework Core with SQL Server (LocalDB / Docker locally, Azure SQL in production)
- **Background Jobs**: Azure Functions isolated worker (Queue & Timer triggers) + Azure Queue Storage (Azurite locally)
- **Authentication**: JWT Bearer Tokens with BCrypt password hashing
- **Frontend**: React + TypeScript (Vite), React Router, TanStack Query, Tailwind CSS
- **Testing**: xUnit + FluentAssertions for backend; Vitest + React Testing Library for frontend

---

## Local Development Setup

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/) (for SQL Server and Azurite)
- [Node.js 20+ & npm](https://nodejs.org/) (for frontend)

---

### Step 1: Start Local Infrastructure (SQL Server & Azurite)
Run Docker Compose from the repository root:

```bash
docker-compose up -d
```

This spins up:
- **SQL Server 2022**: `localhost:1433` (User: `sa`, Password: `YourStrong@Passw0rd!`)
- **Azurite Storage Emulator**: Blob `10000`, Queue `10001`, Table `10002`

---

### Step 2: Configure Secrets (No Secrets in Source Control)
Connection strings and JWT keys can be safely configured using .NET User Secrets:

```bash
cd backend/Eventloop.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=EventloopDb;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;MultipleActiveResultSets=true"
dotnet user-secrets set "Jwt:Key" "A-Very-Secure-Key-With-At-Least-32-Characters-Long!"
dotnet user-secrets set "Jwt:Issuer" "EventloopApi"
dotnet user-secrets set "Jwt:Audience" "EventloopApp"
```

---

### Step 3: Run Database Migrations
Apply the initial migration to your local SQL Server instance:

```bash
dotnet tool run dotnet-ef database update --project backend/Eventloop.Infrastructure --startup-project backend/Eventloop.Api
```

---

### Step 4: Run the Web API
Start the ASP.NET Core Web API:

```bash
dotnet run --project backend/Eventloop.Api/Eventloop.Api.csproj
```

The API will listen on `http://localhost:5000` (or `https://localhost:5001`).

- **Swagger UI**: Visit [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **Health Check**: Visit [http://localhost:5000/health](http://localhost:5000/health)

---

### Step 5: Run Automated Tests
Execute backend unit and integration tests:

```bash
dotnet test backend/Eventloop.Tests/Eventloop.Tests.csproj
```

---

## Data Model & Constraints Summary

| Entity | Fields | Key Rules & Constraints |
|---|---|---|
| **Users** | `Id`, `Name`, `Email`, `PasswordHash`, `Role`, `CreatedAt` | Unique index on `Email`; enum conversion for `Role` (`Student`, `Organizer`, `Admin`). |
| **Events** | `Id`, `OrganizerId`, `Title`, `Description`, `Venue`, `StartTime`, `EndTime`, `Capacity`, `Status`, `CreatedAt`, `RowVersion` | Check constraint `Capacity > 0`; `RowVersion` byte array mapped to `rowversion` for optimistic concurrency; index on `OrganizerId`. |
| **Registrations** | `Id`, `EventId`, `UserId`, `Status`, `WaitlistPosition`, `TicketCode`, `RegisteredAt`, `CheckedInAt`, `CheckedInBy` | Unique composite constraint on `(EventId, UserId)`; unique index on `TicketCode`; composite index on `(EventId, Status)`. |

---

## API Endpoints (Phase 1, 2 & 3)

| Method | Endpoint | Access | Description |
|---|---|---|---|
| `GET` | `/health` | Public | Service health and environment status |
| `POST` | `/api/auth/register` | Public | Register new user (Student by default); returns JWT |
| `POST` | `/api/auth/login` | Public | Authenticate user; returns JWT with role claim |
| `GET` | `/api/auth/me` | Authenticated | Retrieve current user profile from JWT claims |
| `POST` | `/api/users/{id}/promote-organizer` | Admin | Promote a user to Organizer role |
| `GET` | `/api/events` | Public | List published events with search, date filters, "has spots", sorting & pagination |
| `GET` | `/api/events/{id}` | Public | Event details with confirmed count, waitlist count, and spots left |
| `POST` | `/api/events` | Organizer / Admin | Create a new event (starts as Draft) |
| `PUT` | `/api/events/{id}` | Organizer / Admin | Update event details & capacity (validates ownership & capacity constraints) |
| `DELETE` | `/api/events/{id}` | Organizer / Admin | Delete event (Draft only; validates ownership) |
| `POST` | `/api/events/{id}/publish` | Organizer / Admin | Publish draft event (validates ownership) |
| `POST` | `/api/events/{id}/cancel` | Organizer / Admin | Cancel event (validates ownership) |

---

## Authorization Policies

- **RequireAdmin**: User must have the `Admin` role. Can manage all events, registrations, and users.
- **RequireOrganizer**: User must have either `Organizer` or `Admin` role. Can create and manage events.
- **Student**: Default role upon registration. Can view published events, register, and check into events.

---

## Implemented Phases
- [x] **Phase 1: Backend Foundation**
  - Monorepo directory structure and solution setup
  - Core domain model, entities, enums, and EF Core configurations
  - Initial database migration (`InitialCreate`) with all indexes, constraints, and concurrency token
  - Global RFC 7807 `ProblemDetails` exception handling middleware
  - Swagger/OpenAPI with JWT Bearer scheme
  - CORS for frontend dev server (`http://localhost:5173`)
  - Health check endpoint (`/health`)
  - Docker Compose for SQL Server and Azurite
  - xUnit & FluentAssertions test suite
- [x] **Phase 2: Authentication and Roles**
  - BCrypt password hashing (`IPasswordHasher`, `BCryptPasswordHasher`)
  - JWT token generation (`IJwtTokenGenerator`, `JwtTokenGenerator`) with configurable secret and expiry
  - Current user accessor (`ICurrentUserService`, `CurrentUserService`) extracting claims via `IHttpContextAccessor`
  - Auth endpoints: `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me`
  - Admin promotion endpoint: `POST /api/users/{id}/promote-organizer`
  - FluentValidation for register and login requests
  - Conflict handling (409 ProblemDetails) on duplicate email registration
- [x] **Phase 3: Events API**
  - Event CRUD and lifecycle operations (`Create`, `Update`, `Delete`, `Publish`, `Cancel`)
  - Ownership enforcement: Organizers may only manage their own events; Admins can manage all
  - State machine restrictions: Only `Draft` events can be deleted or published; completed/cancelled events cannot be modified
  - Capacity rules: Enforced `Capacity > 0`; capacity can be increased post-publish, but cannot be reduced below confirmed registrations
  - TODO hook for waitlist auto-promotion upon capacity increase (Phase 4 integration)
  - Public events feed with title search, date range filters, "has spots" filter, dynamic sorting, and pagination
  - Public event details with real-time `confirmedCount`, `waitlistCount`, `spotsLeft`, and `isSoldOut` calculations
  - 52 unit, validation, service, and controller tests passing
