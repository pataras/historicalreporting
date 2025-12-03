# Historical Reporting

This project will investigate the options for reporting on historic changes.
It will use a chatgpt approach to take a prompt to deliver historical reporting.

## The Figures (Live)

- c. 60 million driver audit entries
- c. 750 thousand permit to drive changes

## Tech Stack

| Layer | Technology |
|-------|------------|
| Backend | .NET 8, ASP.NET Core Web API |
| ORM | Entity Framework Core 8 |
| Database | SQL Server Express (LocalDB) |
| Frontend | Vite 5, React 18, TypeScript |
| UI Library | MUI (Material UI) 7 |
| State Management | TanStack Query |

## Project Structure

```
historicalreporting/
├── src/
│   ├── HistoricalReporting.Api/          # ASP.NET Core Web API
│   ├── HistoricalReporting.Core/         # Domain/Business logic
│   └── HistoricalReporting.Infrastructure/ # Data access, EF Core
├── frontend/                              # Vite React application
├── database/                              # SQL scripts
│   └── scripts/
├── HistoricalReporting.sln
└── readme.md
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or LocalDB

## Getting Started

### 1. Database Setup

```bash
# Option A: Use SQL Server Express
sqlcmd -S localhost\SQLEXPRESS -i database/scripts/001_InitialSchema.sql

# Option B: Use EF Core migrations
cd src/HistoricalReporting.Api
dotnet ef database update
```

### 2. Backend

```bash
cd src/HistoricalReporting.Api
dotnet restore
dotnet run
```

The API will be available at:
- HTTP: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger

### 3. Frontend

```bash
cd frontend
npm install
npm run dev
```

The frontend will be available at http://localhost:5173

## Development

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/health | Health check |
| GET | /api/reports | Get all reports |
| GET | /api/reports/{id} | Get report by ID |

### Running Both Apps

Open two terminals:

**Terminal 1 - Backend:**
```bash
cd src/HistoricalReporting.Api && dotnet run
```

**Terminal 2 - Frontend:**
```bash
cd frontend && npm run dev
```

## Stage 1 Checklist

- [x] .NET 8 Web API setup with controllers
- [x] Entity Framework Core with SQL Server
- [x] Clean architecture (API, Core, Infrastructure)
- [x] Vite + React + TypeScript frontend
- [x] MUI 7 component library
- [x] TanStack Query for server state
- [x] React Router for navigation
- [x] API proxy configuration
- [x] Health check endpoint
- [x] Basic CRUD structure for reports

## Next Stages

- **Stage 2**: Authentication & Authorization (JWT)
- **Stage 3**: Report builder and query engine
- **Stage 4**: ChatGPT-style prompt interface
- **Stage 5**: Data visualization and exports
