# TrafficHunt.Web

Private AI-powered customer acquisition and outreach — an **ASP.NET Core MVC system console** for a single
operator (no auth, no tenancy).

## Stack

- ASP.NET Core MVC (Razor views) on .NET 10
- MySQL + EF Core (Pomelo), Code First
- YoutubeExplode (discovery — video search) + YouTube Data API v3 (comment collection)
- Ollama (AI qualification + reply generation + campaign planning)
- Hangfire (background jobs: discovery, analysis, reply rotation, maintenance)

## Responsibilities

This project is the **composition root and the presentation layer** only:

- MVC controllers + Razor views (the system UI)
- DI wiring: `AddApplication()` → `AddInfrastructure()`, Hangfire server + jobs, recurring schedules
- `wwwroot/css/site.css` + `wwwroot/js/site.js` — self-contained dark "console" styling (no CDN)

Pages: Dashboard (`/`), Campaigns (list / detail / create / edit / AI plan), Prospects, Reply Campaigns,
Background Jobs (`/jobs`), plus the Hangfire dashboard (`/hangfire`, dev only).

The project references `TrafficHunt.Application` and `TrafficHunt.Infrastructure`. Domain logic lives
in the other Clean-Architecture projects; nothing in the Web layer bypasses the Application services.

## Run

```powershell
dotnet run --project TrafficHunt.Web
```

Point a browser at the `launchSettings` URL (https://localhost:52955 by default).
Requires MySQL reachable at the connection string in `appsettings.json` and, for AI features, an
Ollama instance configured under `Ollama:`.

> Note for XAMPP MySQL: the connection string uses `root` with an empty password by default — override
> with `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."` in this project.