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

### Why comments come from the Data API

Modern YoutubeExplode (6.x) removed comment extraction entirely — verified against the
shipped assemblies (no comment types or endpoints remain in the library). So the split is:

- **YoutubeExplode** — passive discovery only (video search). It has *no write capability*.
- **YouTube Data API v3** — comment collection (`commentThreads`, 1 quota unit per call)
  and, in Milestone 2, the **only** write path (OAuth replies, gated by user approval).

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

1. **MySQL** — XAMPP ships MySQL on port 3306 (`root`, no password by default). The connection
   string in `TrafficHunt.Web/appsettings.json` already matches that. Override locally with user
   secrets to keep credentials out of the repo:

   ```powershell
   cd TrafficHunt.Web
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=127.0.0.1;Port=3306;Database=traffichunt;Uid=root;Pwd=YOUR_PASSWORD;"
   ```

2. **Code First** — the data model lives in `TrafficHunt.Domain` entities; migrations live in
   `TrafficHunt.Infrastructure`. Apply from the repo root:

   ```powershell
   dotnet ef database update --project TrafficHunt.Infrastructure --startup-project TrafficHunt.Web
   ```

   The committed `20260902165822_InitialCreate` migration already covers `Campaigns`,
   `CampaignKeywords`, `Prospects`, `Videos`, `Comments`, and their indexes — one command to
   create the schema. To add a new migration after a model change:

   ```powershell
   dotnet ef migrations add <Name> --project TrafficHunt.Infrastructure --startup-project TrafficHunt.Web
   dotnet ef database update --project TrafficHunt.Infrastructure --startup-project TrafficHunt.Web
   ```

3. **YouTube Data API key** — create one with the YouTube Data API v3 enabled, set
   `YouTube:ApiKey` in `appsettings.json` (or user secrets).

4. **Ollama** — the backend sends AI requests to an Ollama instance configured under `Ollama:`
   in `appsettings.json` (a hosted instance is pre-configured):

   ```json
   "Ollama": {
     "BaseUrl": "http://63.141.255.202:11434",
     "Model": "llama3"
   }
   ```

5. **Run**:

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
