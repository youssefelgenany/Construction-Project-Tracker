# Construction Project Tracker

A full-stack construction project management system built with **Angular 19** and **ASP.NET Core 8 Web API**.

## Tech Stack

| Layer | Technologies |
|-------|-------------|
| Frontend | Angular 19, Angular Material, Chart.js, ng2-charts, SCSS |
| Backend | ASP.NET Core 8, Entity Framework Core, SQL Server |
| Auth | JWT Bearer tokens with role-based authorization |
| Patterns | Repository + Service, AutoMapper, FluentValidation |

## Project Structure

```
ConstructionProjectTracker/
├── Backend/
│   └── ConstructionProjectTracker.API/
├── Frontend/
│   └── construction-project-tracker/
├── Database/
├── Documents/
└── README.md
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) and npm
- [Angular CLI 19](https://angular.dev/tools/cli)
- [SQL Server](https://www.microsoft.com/sql-server) (SSMS recommended)

## Backend Setup

### 1. Navigate to the API project

```bash
cd Backend/ConstructionProjectTracker.API
```

### 2. Restore packages

```bash
dotnet restore
```

### 3. Update connection string

Edit `appsettings.json` and set your SQL Server connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=ConstructionProjectTrackerDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 4. Create and apply database migration

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 5. Run the API

```bash
dotnet run --launch-profile https
```

The API runs at:
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

## Frontend Setup

### 1. Navigate to the Angular project

```bash
cd Frontend/construction-project-tracker
```

### 2. Install dependencies

```bash
npm install
```

### 3. Run the development server

```bash
ng serve
```

The app runs at `http://localhost:4200`

## Environment Configuration

The frontend API URL is configured in:

- `src/environments/environment.ts` (production)
- `src/environments/environment.development.ts` (development)

```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api'
};
```

## API Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/auth/login` | User login | Public |
| POST | `/api/auth/register` | User registration | Public |
| GET | `/api/dashboard` | Dashboard metrics | JWT |
| GET/POST/PUT/DELETE | `/api/projects` | Project CRUD | JWT (Admin for write) |
| GET/POST/PUT/DELETE | `/api/engineers` | Engineer CRUD | JWT (Admin for write) |
| GET/POST/PUT/DELETE | `/api/tasks` | Task CRUD | JWT |
| GET/POST/DELETE | `/api/documents` | Document management | JWT |

## Migration Commands

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project Backend/ConstructionProjectTracker.API

# Apply migrations to database
dotnet ef database update --project Backend/ConstructionProjectTracker.API

# Remove last migration (if not applied)
dotnet ef migrations remove --project Backend/ConstructionProjectTracker.API
```

## Run Commands (Quick Start)

Open two terminals:

**Terminal 1 — Backend:**
```bash
cd Backend/ConstructionProjectTracker.API
dotnet run --launch-profile https
```

**Terminal 2 — Frontend:**
```bash
cd Frontend/construction-project-tracker
ng serve
```

## Backend Architecture

```
Controllers/     → API endpoints
Services/        → Business logic layer (interfaces in Interfaces/)
Repositories/    → Generic repository pattern
Entities/        → EF Core domain models
DTOs/            → Request/response models
Mappings/        → AutoMapper profiles
Validators/      → FluentValidation rules
Middleware/      → Cross-cutting concerns
Helpers/         → JWT and utility helpers
Enums/           → UserRole, ProjectStatus, TaskStatus
Data/            → ApplicationDbContext
```

## Frontend Architecture

```
src/app/
├── core/           → App-wide constants and utilities
├── shared/         → Reusable components
├── features/       → Feature modules (auth, projects, tasks, etc.)
├── layouts/        → Main and auth layouts
├── models/         → TypeScript interfaces and enums
├── services/       → HTTP API services
├── guards/         → auth.guard, admin.guard
└── interceptors/   → JWT bearer token interceptor
```

## Notes

- This is the **initial scaffolding** — UI forms and full business logic are not yet implemented.
- JWT secret is configured in `appsettings.json` — change it before production deployment.
- CORS is configured to allow `http://localhost:4200`.
