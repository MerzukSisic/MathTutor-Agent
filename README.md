# MathTutor Agent

AI-assisted math tutoring platform built with **.NET 9**, **Blazor Server**, **EF Core**, **SignalR**, and **ML.NET**.

It provides adaptive quizzes, geometry click practice, milestone challenges, student analytics, and an admin panel for content and operations.

## What You Get

- Adaptive quiz flow with timed questions and mastery updates
- Interactive practice blocks (arithmetic visuals + geometry side/vertex counting)
- CrossMath milestone challenges
- Student profile insights and PDF export
- Admin workflows for students, questions, and ML training triggers
- Background work queue with SignalR push updates
- Dual DB support: SQLite (default) and SQL Server
- UI localization support (Bosnian / English)

## Tech Stack

- .NET 9 (`net9.0`)
- ASP.NET Core + Blazor Server
- Entity Framework Core (SQLite / SQL Server)
- SignalR
- ML.NET
- Radzen Blazor
- Serilog

## Repository Layout

- `AiAgents.MathTutorAgent.Web` - Blazor UI, API controllers, startup, SignalR hub
- `AiAgents.MathTutorAgent` - domain/application services, runners, infrastructure
- `AiAgents.Core` - shared primitives
- `AiAgents.MathTutor.sln` - root solution

## Configuration

Primary config file:

- `AiAgents.MathTutorAgent.Web/appsettings.json`

Key settings:

- `DatabaseProvider`: `Sqlite` or `SqlServer`
- `ConnectionStrings:SqliteConnection`
- `ConnectionStrings:SqlServerConnection`
- `App:PublicBaseUrl`
- `Email:*` (SMTP)

## Database Startup Behavior

- **SQLite**: startup calls `EnsureCreated` and applies `CREATE TABLE IF NOT EXISTS` for missing tables.
- **SQL Server**: startup runs EF Core migrations.
- Seed routines run on startup.
- Existing DB is **not dropped** during normal startup.

Default SQLite file:

- `AiAgents.MathTutorAgent.Web/mathtutor.db`

## Auth and Default Admin (Development)

On startup, the app ensures a default admin account exists.

- Default email: `admin@mathtutor.local`
- Default password: `Admin123!`

Override via environment variables:

- `MATH_TUTOR_DEFAULT_ADMIN_EMAIL`
- `MATH_TUTOR_DEFAULT_ADMIN_PASSWORD`

For non-local environments, set strong secrets via environment variables or secret manager.

## Logging

Serilog writes to:

- Console
- `AiAgents.MathTutorAgent.Web/logs/` (daily rolling files)

## Troubleshooting

- If app appears to "start with empty data", verify you are running from expected working directory and using the intended SQLite file path.
- If package restore warns about Radzen exact version resolution, restore still succeeds with the nearest available compatible version.

## License

This project is proprietary and confidential. All rights reserved.

No use, copying, modification, distribution, or resale is allowed without prior written permission from the owner.
