# TrafficHunt

**TrafficHunt is a private AI-powered customer acquisition and outreach application.**

It discovers potential customers on YouTube, analyzes their comments using AI, identifies people
expressing a problem that a promoted application can solve, and helps the operator engage with
those prospects.

TrafficHunt is **not a SaaS** — no registration, no billing, no multi-tenancy. There is only one
operator.

The core creative principle:

> **TrafficHunt doesn't know what app we're promoting. The Campaign tells it what we're promoting,
> who we're looking for, what problem we're solving, and how we should approach them.**

Tomorrow it can promote TubeMail Gorilla; next month, point the exact same engine at another app.

---

## Architecture

Clean Architecture with four .NET projects plus a React frontend:

```
┌─────────────────────────────┐
│      TrafficHunt.React      │  React 19 + TypeScript + Vite
└──────────────┬──────────────┘
               │ REST API (SSE for discovery progress)
               ▼
┌─────────────────────────────┐
│      TrafficHunt.Web        │  Controllers + composition root only
└──────────────┬──────────────┘
               │
    ┌──────────┴───────────┐
    ▼                      ▼
┌───────────────────┐  ┌────────────────────────────┐
│ TrafficHunt       │  │ TrafficHunt.Infrastructure │
│ .Application      │  │  EF Core + Pomelo (MySQL)  │
│  Services         │  │  YoutubeExplode (search)   │
│  Interfaces       │  │  YouTube Data API (comments│
│  DTOs             │  │   + OAuth replies, M2)     │
└─────────┬─────────┘  │  Ollama (AI reasoning)     │
          ▼            └─────────────┬──────────────┘
┌───────────────────┐                │
│ TrafficHunt       │◄───────────────┘
│ .Domain           │  Entities only, no dependencies
└───────────────────┘
```

**Dependency rule**: Application has no EF Core, no HttpClient, no YoutubeExplode.
External concerns are behind interfaces, implemented in Infrastructure.

## The Pipeline

```
Campaign (product, audience, problems, keywords, AI instructions)
    ↓
DiscoveryService (orchestrator)
    ↓
YoutubeExplode → search videos per campaign keyword
    ↓
YouTube Data API v3 → collect comments
    ↓
Ollama → qualify each comment (structured JSON):
         is_target_audience / has_relevant_problem / intent_score / pain_point / reason
    ↓
Prospects (MySQL), ranked by intent score
```

Later milestones: AI reply generation with **human approval** (the AI never publishes outreach
automatically), YouTube OAuth replies, and an **MCP server** so an AI assistant can operate the
engine (`get_campaign`, `search_youtube`, `find_prospects`, …) through the same Application
services the REST API uses.

## Projects

| Project | Responsibility |
| --- | --- |
| `TrafficHunt.Domain` | Entities: Campaign, CampaignKeyword, Prospect, Video, Comment |
| `TrafficHunt.Application` | Use cases, service + repository interfaces, DTOs |
| `TrafficHunt.Infrastructure` | MySQL (Pomelo), YoutubeExplode, YouTube Data API, Ollama |
| `TrafficHunt.Web` | ASP.NET Core controllers, DI composition root |
| `TrafficHunt.React` | Dashboard, Campaigns, Prospects UI |

## Setup

### Backend

1. **MySQL** — create a database (e.g. `traffichunt`), then set the connection string in
   `TrafficHunt.Web/appsettings.json` or user secrets:

   ```powershell
   cd TrafficHunt.Web
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=traffichunt;User=root;Password=..."
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

2. **YouTube Data API key** — create an API key with YouTube Data API v3 enabled and set
   `YouTube:ApiKey` in `appsettings.json`.

3. **Ollama** — install from https://ollama.com and pull a model:

   ```powershell
   ollama pull llama3.1
   ```

   Model and base URL are configurable under `Ollama` in `appsettings.json`.

4. **Run**:

   ```powershell
   dotnet run --project TrafficHunt.Web
   ```

### Frontend

```powershell
cd TrafficHunt.React
npm install
npm run dev
```

The dev server proxies `/api` to `http://localhost:5000`.

## API Surface (Milestone 1)

| Method | Route | Purpose |
| --- | --- | --- |
| GET/POST/PUT/DELETE | `/api/campaigns` | Campaign CRUD |
| POST | `/api/campaigns/{id}/keywords` | Add discovery keyword |
| DELETE | `/api/campaigns/keywords/{id}` | Remove keyword |
| GET | `/api/campaigns/{id}/stats` | Campaign stats |
| GET | `/api/prospects?campaignId=&status=&minIntentScore=` | Filtered prospects |
| PATCH | `/api/prospects/{id}/status` | Update prospect status |
| GET | `/api/prospects/stats/global` | Global stats |
| POST | `/api/discovery/{campaignId}/run` | Run discovery (SSE progress stream) |

## Roadmap

- **Milestone 1** ✅ — Campaigns → discovery → AI qualification → prospects
- **Milestone 2** — Reply generation, review editor, YouTube OAuth, approved replies
- **Milestone 3** — MCP server: the AI as TrafficHunt operator

## Human Approval Boundary

The AI can search, analyze, score, and generate replies — but it can never publish outreach
automatically. The operator controls the final send.
