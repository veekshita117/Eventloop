# Eventloop

A college event registration and check-in platform. Clubs publish events, students register and receive a QR ticket, and organizers scan tickets at the door. When an event is full, students join a waitlist and are promoted automatically when a spot opens.

Built with **ASP.NET Core (.NET 8)**, **Entity Framework Core**, **SQL Server**, **Azure Queue Storage and Azure Functions**, and **React + TypeScript**, with CI/CD on GitHub Actions and hosting on Azure.

---

## Features

**Students**
- Register and log in with JWT authentication.
- Browse published events with title search, date filters, a "has spots" filter, sorting and pagination.
- Register for an event and get a confirmed ticket, or join the waitlist when it is full and see your queue position.
- Cancel a registration at any time before check-in.
- View tickets in "My Tickets", each with a QR code and a copyable ticket code.
- Receive emails for ticket confirmation, waitlist promotion, cancellation, and a reminder within 24 hours of the event.

**Organizers**
- Create, edit, publish, cancel and delete (drafts only) their own events.
- Raise an event's capacity after publishing; waitlisted students are promoted for the new spots.
- View the attendee roster with status filters and search, and export it as CSV.
- Check attendees in at the door by scanning the QR code with a camera, or by typing the ticket code.
- See attendance statistics: confirmed, checked-in, waitlisted and cancelled counts, no-show rate, check-ins per 15-minute interval and sign-ups per day.

**Admins**
- Everything organizers can do, across all events, plus promoting users to the Organizer role.

---

## Tech Stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core Web API (.NET 8) |
| Data access | Entity Framework Core 8, EF Core migrations, hand-written SQL for statistics |
| Database | SQL Server (Docker locally, Azure SQL in production) |
| Background jobs | Azure Functions (isolated worker, Queue and Timer triggers) |
| Messaging | Azure Queue Storage (Azurite locally) |
| Email | Azure Communication Services Email, or a console logger for development |
| Auth | JWT bearer tokens, BCrypt.Net, role-based policies |
| Validation | FluentValidation |
| API docs | Swagger / OpenAPI with JWT support (development environment) |
| Telemetry | Application Insights (API and Functions) |
| Frontend | React 18, TypeScript, Vite, React Router 6, TanStack Query 5, Tailwind CSS |
| Frontend libraries | qrcode.react (ticket QR), html5-qrcode (camera scanner), Recharts (charts), Lucide icons |
| Backend tests | xUnit, FluentAssertions, Moq, EF Core InMemory, Coverlet |
| Frontend tests | Vitest, React Testing Library, jsdom |
| CI/CD | GitHub Actions |
| Hosting | Azure App Service, Azure Functions, Azure Static Web Apps, Azure SQL Database |

---

## Architecture

```
                    React SPA (Vite)
                          |
                  HTTPS + JWT (REST)
                          v
   +---------------- ASP.NET Core API ----------------+
   | Controllers -> Validators -> Services -> EF Core  |
   +---------+-------------------------------+--------+
             |                               |
             | enqueue after commit          v
             v                         SQL Server / Azure SQL
   Azure Queue Storage                       ^
   (registration-events)                     |
             |                               |
             v                               |
   +----------------- Azure Functions -------+--------+
   | Queue trigger: send ticket / promotion /          |
   |                cancellation emails                |
   | Timer trigger: daily 24-hour reminders (08:00 UTC)|
   +-----------------------+---------------------------+
                           |
                           v
                  Email (ACS or console)
```

The API never sends email itself. After a registration changes, it puts a small message on a queue, and a separate Azure Functions app reads it and sends the email. A slow or failing email provider therefore cannot slow down or break registration.

### Project structure

```
eventloop/
├── .github/workflows/          ci.yml, deploy.yml
├── backend/
│   ├── Eventloop.sln
│   ├── Eventloop.Api/              controllers, validators, middleware, Program.cs
│   ├── Eventloop.Core/             entities, enums, DTOs, interfaces, messages, exceptions
│   ├── Eventloop.Infrastructure/   DbContext, configurations, migrations, services, email
│   ├── Eventloop.Functions/        Azure Functions (queue and timer triggers)
│   └── Eventloop.Tests/            xUnit tests
├── frontend/
│   └── src/
│       ├── api/                    typed API client modules
│       ├── components/             shared UI and layout
│       ├── context/                auth and toast providers
│       ├── features/               auth, events, tickets, organizer
│       └── test/                   Vitest tests
├── docs/AZURE_DEPLOYMENT_GUIDE.md
└── docker-compose.yml              SQL Server and Azurite for local development
```

### Design rules

- Controllers are thin and call services through interfaces such as `IRegistrationService` and `ICheckInService`.
- API input and output use DTOs; EF entities are never exposed.
- A global exception middleware converts domain exceptions into consistent `ProblemDetails` responses.
- Authorization policies: `RequireOrganizer` (Organizer or Admin) and `RequireAdmin`.
- Event ownership is enforced in the service layer, so organizers can only manage their own events.

---

## Data Model

| Table | Key columns and rules |
|---|---|
| `Users` | `Id`, `Name`, `Email` (unique), `PasswordHash`, `Role` (Student, Organizer, Admin), `CreatedAt` |
| `Events` | `Id`, `OrganizerId`, `Title`, `Description`, `Venue`, `StartTime`, `EndTime`, `Capacity` (check constraint `Capacity > 0`), `Status` (Draft, Published, Cancelled, Completed), `CreatedAt`, `RowVersion` |
| `Registrations` | `Id`, `EventId`, `UserId`, `Status` (Confirmed, Waitlisted, Cancelled, CheckedIn), `WaitlistPosition`, `TicketCode` (unique), `RegisteredAt`, `CheckedInAt`, `CheckedInBy`, `ReminderSentAt` |

Indexes and constraints on `Registrations`: a unique `(EventId, UserId)` so a student can't register twice, a unique `TicketCode` for fast scanner lookup, `(EventId, Status)` for capacity checks and roster filters, and indexes on every foreign key. `ReminderSentAt` makes sure each registration gets at most one reminder email.

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/) (SQL Server and Azurite)
- [Node.js 20 or later](https://nodejs.org/) and npm
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local) (optional, for `func start`)

### 1. Start SQL Server and Azurite

```bash
docker-compose up -d
```

This starts SQL Server 2022 on `localhost:1433` and the Azurite storage emulator (blob 10000, queue 10001, table 10002). The default SA password is set in `docker-compose.yml`; override it with the `MSSQL_SA_PASSWORD` environment variable.

### 2. Configure secrets

```bash
cd backend/Eventloop.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=EventloopDb;User Id=sa;Password=<your-sa-password>;TrustServerCertificate=True;MultipleActiveResultSets=true"
dotnet user-secrets set "Jwt:SecretKey" "<a-random-string-of-at-least-32-characters>"
```

### 3. Create the database

From the repository root:

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update --project backend/Eventloop.Infrastructure --startup-project backend/Eventloop.Api
```

### 4. Run the API

```bash
dotnet run --project backend/Eventloop.Api/Eventloop.Api.csproj --urls http://localhost:5000
```

- Swagger UI: `http://localhost:5000/swagger`
- Health checks: `http://localhost:5000/health` and `http://localhost:5000/health/live`

Port 5000 matters because the frontend dev server proxies `/api` there.

### 5. Run the background jobs

In a second terminal:

```bash
cd backend/Eventloop.Functions
func start
```

By default the Functions app reads the local Azurite queue and prints emails to the console instead of sending them.

### 6. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

The app runs at `http://localhost:5173`.

### 7. Optional: load sample data

```bash
dotnet run --project backend/Eventloop.Api -- --seed
```

This creates sample users, events, waitlists and check-in records so there is something to look at right away. It does nothing if the data already exists. The same seeding is also available as `POST /api/demo/seed`.

### 8. Run the tests

```bash
# Backend, with coverage
dotnet test backend/Eventloop.Tests/Eventloop.Tests.csproj --collect:"XPlat Code Coverage"

# Frontend
cd frontend
npm test
```

---

## Configuration

| Setting | Used by | Purpose |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | API, Functions | SQL Server connection string (Functions also accepts `SqlConnectionString`) |
| `Jwt:SecretKey` | API | Token signing key; set your own, at least 32 characters |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryInMinutes` | API | Token settings (default expiry is 24 hours) |
| `Cors:AllowedOrigins` | API | Origins allowed to call the API (defaults to localhost dev ports) |
| `AzureWebJobsStorage` | API, Functions | Storage connection string for the `registration-events` queue (the API falls back to `ConnectionStrings:AzureQueueStorage`, then to local Azurite) |
| `Email:Provider` | API, Functions | `Console` (default) or `AzureCommunicationServices` |
| `Email:AzureCommunicationServices:ConnectionString`, `:SenderAddress` | Functions | Azure Communication Services email settings |
| `VITE_API_URL` | Frontend build | API base URL for production builds; leave empty in development to use the proxy |

---

## API Overview

Interactive documentation is available through Swagger when the API runs in the development environment.

| Method | Endpoint | Access | Description |
|---|---|---|---|
| `GET` | `/health` | Public | Service health |
| `POST` | `/api/auth/register` | Public | Register a new student; returns a JWT |
| `POST` | `/api/auth/login` | Public | Log in; returns a JWT with the role claim |
| `GET` | `/api/auth/me` | Authenticated | Current user profile |
| `POST` | `/api/users/{id}/promote-organizer` | Admin | Promote a user to Organizer |
| `GET` | `/api/events` | Public | Published events with search, date filter, "has spots", sorting and paging |
| `GET` | `/api/events/{id}` | Public | Event details with confirmed count, waitlist count and spots left |
| `GET` | `/api/events/mine` | Organizer, Admin | Events owned by the current user |
| `POST` | `/api/events` | Organizer, Admin | Create an event (starts as Draft) |
| `PUT` | `/api/events/{id}` | Organizer, Admin | Update an event and its capacity |
| `DELETE` | `/api/events/{id}` | Organizer, Admin | Delete a draft event |
| `POST` | `/api/events/{id}/publish` | Organizer, Admin | Publish a draft event |
| `POST` | `/api/events/{id}/cancel` | Organizer, Admin | Cancel an event |
| `POST` | `/api/events/{id}/register` | Authenticated | Register (confirmed ticket or next waitlist position) |
| `DELETE` | `/api/events/{id}/register` | Authenticated | Cancel my registration; promotes the next waitlisted student |
| `GET` | `/api/me/registrations` | Authenticated | My tickets and waitlist positions |
| `GET` | `/api/events/{id}/registrations` | Organizer, Admin | Attendee roster with filters |
| `GET` | `/api/registrations/{id}/ticket` | Owner, event organizer, Admin | Ticket data for the QR code |
| `POST` | `/api/events/{id}/check-in` | Organizer, Admin | Check in a ticket code |
| `GET` | `/api/events/{id}/stats` | Organizer, Admin | Attendance statistics |
| `GET` | `/api/events/{id}/attendees.csv` | Organizer, Admin | Attendee export |
| `POST` | `/api/demo/seed` | Public | Load sample data |

Check-in responses: `200` on success, `409` if the ticket was already scanned (with the original time), `400` for a wrong-event, cancelled or waitlisted ticket, `404` for an unknown ticket code, and `403` if the scanner is not the event's organizer or an admin.

---

## Design Decisions

### Registration without overbooking
When several students try to take the last spot at once, only one may succeed. Registration works in three layers:
1. **Database lock.** On SQL Server, the event row is read inside a transaction with `WITH (UPDLOCK, ROWLOCK)`, so competing registrations for the same event wait their turn and the capacity check is safe.
2. **Per-event semaphore.** An in-process lock keyed by event ID serializes registrations within an instance, which also makes the logic testable with the EF Core InMemory provider.
3. **Unique constraint.** A unique `(EventId, UserId)` index rejects duplicate registrations even if a request slips past the application checks.

A parallel test registers many students against a small event and checks that exactly the capacity number are confirmed.

### Waitlist promotion
Waitlisted registrations carry a sequential position. When a confirmed student cancels, the lowest position is promoted and the remaining positions are renumbered so the queue has no gaps. Raising an event's capacity promotes as many waitlisted students as there are new spots.

### Queue-based email
After a registration is confirmed, promoted or cancelled, the API publishes a message to the `registration-events` queue only after the database change is saved. If enqueueing fails, the error is logged and the registration still stands. The Functions app processes each message, checks that the registration is still in the expected state before sending (so retries don't send stale or duplicate emails), and relies on the queue's retry policy: after 5 failed attempts a message moves to the poison queue.

### Reminders
A timer function runs daily at 08:00 UTC and emails confirmed attendees of published events that start within the next 24 hours. It records `ReminderSentAt` so nobody gets a second reminder.

### Safe check-in
Check-in takes an in-process lock per event, so a double scan of the same QR code produces one success and one clear "already checked in" response.

### Explicit SQL for statistics
The statistics endpoint uses hand-written SQL on SQL Server:
- Check-ins per 15-minute interval: `(DATEDIFF(minute, 0, CheckedInAt) / 15) * 15`, converted back with `DATEADD`, then grouped.
- Sign-ups per day: `CAST(RegisteredAt AS DATE)` grouped with `COUNT(*)`.

Tests run against an equivalent LINQ fallback because the InMemory provider can't run raw SQL. The no-show rate is the share of expected attendees (confirmed plus checked-in) who never checked in.

---

## CI/CD and Deployment

### GitHub Actions

- **`ci.yml`** builds and tests the backend (with coverage), then type-checks, tests and builds the frontend.
- **`deploy.yml`** runs on pushes to `main`. After validation it applies EF Core migrations to Azure SQL using a migration bundle, deploys the API to Azure App Service, deploys the background jobs to an Azure Function App, and deploys the frontend to Azure Static Web Apps.

### Azure resources

Resource group, Azure SQL Database, Storage Account (with the `registration-events` queue), App Service (Linux, .NET 8), Function App (Consumption, .NET 8 isolated), Static Web App, Application Insights, and optionally Azure Communication Services for email. Step-by-step setup and cost-friendly tiers are in [docs/AZURE_DEPLOYMENT_GUIDE.md](docs/AZURE_DEPLOYMENT_GUIDE.md).

### Required GitHub secrets

| Secret | Used for |
|---|---|
| `AZURE_SQL_CONNECTION_STRING` | Applying migrations |
| `AZURE_WEBAPP_NAME`, `AZURE_WEBAPP_PUBLISH_PROFILE` | Deploying the API |
| `AZURE_FUNCTIONAPP_NAME`, `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` | Deploying the background jobs |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Deploying the frontend |
| `VITE_API_URL` | API URL baked into the frontend build |

---

## License

Released under the MIT License.
