# MathTutor-Agent

AI-driven math tutoring web application built with .NET 9, Blazor Server, EF Core, SignalR, and ML.NET.

## Features

- Adaptive quiz flow with dynamic question generation
- Geometry click practice (count sides/vertices)
- CrossMath milestone challenges
- Student profile insights and progress tracking
- Admin dashboard for questions/students and ML operations
- Background agent processing via SignalR + work queue
- SQLite (default) or SQL Server database support

## Tech Stack

- .NET 9 (`net9.0`)
- ASP.NET Core + Blazor Server
- Entity Framework Core (SQLite / SQL Server)
- SignalR
- ML.NET
- Radzen Blazor UI components
- Serilog logging

## Project Structure

- `AiAgents.MathTutorAgent.Web` - web app (UI + API + SignalR + startup)
- `AiAgents.MathTutorAgent` - application/domain/services layer
- `AiAgents.Core` - shared core library
- `AiAgents.MathTutor.sln` - solution file

## Requirements

- .NET SDK 9.0+
- (Optional) SQL Server if using `DatabaseProvider=SqlServer`

## Quick Start

```bash
cd /Users/merzuksisic/Desktop/MathTutor-Agent
dotnet restore
dotnet build
cd AiAgents.MathTutorAgent.Web
dotnet run
```

Default URLs:

- `http://localhost:5297`
- `https://localhost:7152`

## Configuration

Primary config file:

- `AiAgents.MathTutorAgent.Web/appsettings.json`

Important keys:

- `DatabaseProvider`: `Sqlite` (default) or `SqlServer`
- `ConnectionStrings:SqliteConnection`
- `ConnectionStrings:SqlServerConnection`
- `App:PublicBaseUrl`
- `Email:*` (SMTP settings)

### Database Notes

- For SQLite, schema is ensured automatically on startup.
- For SQL Server, EF Core migrations are applied on startup.
- Seed data is populated during application startup.

## Useful Commands

From repository root:

```bash
dotnet build AiAgents.MathTutor.sln
dotnet run --project AiAgents.MathTutorAgent.Web/AiAgents.MathTutorAgent.Web.csproj
```

## Logging

Serilog writes logs to:

- Console
- `AiAgents.MathTutorAgent.Web/logs/` (rolling daily files)

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
