# MathTutor-Agent

An AI-assisted math tutoring platform built with **.NET 9**, **Blazor Server**, **EF Core**, **SignalR**, and **ML.NET**.

It supports adaptive quiz delivery, student progress tracking, admin management workflows, and lightweight ML-backed recommendations.

## Highlights

- Adaptive quiz loop with timed questions
- Interactive geometry practice (count sides/vertices)
- CrossMath milestone challenges
- Student profile dashboards and PDF export
- Admin management for students, questions, and ML operations
- Background work queue + SignalR updates
- Dual database support: SQLite (default) or SQL Server

## Tech Stack

- .NET 9 (`net9.0`)
- ASP.NET Core + Blazor Server
- Entity Framework Core (SQLite / SQL Server)
- SignalR
- ML.NET
- Radzen Blazor
- Serilog

## Solution Layout

- `AiAgents.MathTutorAgent.Web` - web UI, API controllers, startup, SignalR hub
- `AiAgents.MathTutorAgent` - application services, domain logic, infrastructure integration
- `AiAgents.Core` - shared primitives/core library
- `AiAgents.MathTutor.sln` - root solution

## Quick Start

```bash
dotnet restore
dotnet build
cd AiAgents.MathTutorAgent.Web
dotnet run
```

Default local endpoints:

- `http://localhost:5297`
- `https://localhost:7152`

## Configuration

Main config file:

- `AiAgents.MathTutorAgent.Web/appsettings.json`

Important settings:

- `DatabaseProvider`: `Sqlite` or `SqlServer`
- `ConnectionStrings:SqliteConnection`
- `ConnectionStrings:SqlServerConnection`
- `App:PublicBaseUrl`
- `Email:*` (SMTP options)

## Database Behavior

- **SQLite**: schema is ensured on startup.
- **SQL Server**: EF Core migrations are applied on startup.
- Seed routines run automatically at startup.

## Security Notes

- Authentication is cookie-based with role checks (`Admin` / `Student`).
- CSRF protection is enabled for auth-modifying operations.
- API routes use `snake_case` naming.
- Internal exception details are not returned in API responses.
- Do **not** commit real secrets into `appsettings.json`; use environment variables or user-secrets for sensitive values.

## Useful Commands

```bash
# build everything
dotnet build AiAgents.MathTutor.sln

# run web app directly
dotnet run --project AiAgents.MathTutorAgent.Web/AiAgents.MathTutorAgent.Web.csproj
```

## Logging

Serilog outputs to:

- Console
- `AiAgents.MathTutorAgent.Web/logs/` (daily rolling files)

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
