# Database Setup

This folder contains SQL scripts for setting up and maintaining the Historical Reporting database.

## Prerequisites

- SQL Server Express (LocalDB or full Express edition)
- SQL Server Management Studio (SSMS) or Azure Data Studio (optional, for running scripts)

## Connection String

The default connection string for local development:

```
Server=localhost\SQLEXPRESS;Database=HistoricalReporting;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

For LocalDB:
```
Server=(localdb)\MSSQLLocalDB;Database=HistoricalReporting;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

## Running Scripts

### Option 1: Using SQLCMD
```bash
sqlcmd -S localhost\SQLEXPRESS -i scripts/001_InitialSchema.sql
```

### Option 2: Using Entity Framework Core Migrations
The application uses EF Core migrations. To apply migrations:

```bash
cd src/HistoricalReporting.Api
dotnet ef database update
```

To create a new migration:
```bash
dotnet ef migrations add MigrationName -p ../HistoricalReporting.Infrastructure
```

## Scripts

| Script | Description |
|--------|-------------|
| 001_InitialSchema.sql | Creates the initial database schema (Users, Reports tables) |
