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

Clean Architecture with four .NET projects. The operator uses the MVC system UI — no separate SPA and no MCP server:

```
┌─────────────────────────────┐
│      TrafficHunt.Web        │  ASP.NET Core MVC system UI (Razor)
│  Controllers + Views + DI   │  Dashboard / Campaigns / Prospects /
│  composition root only      │  Reply Campaigns / Background Jobs
└──────────────┬──────────────┘
               │
    ┌──────────┴───────────┐
    ▼                      ▼
┌───────────────────┐  ┌────────────────────────────┐
│ TrafficHunt       │  │ TrafficHunt.Infrastructure │
│ .Application      │  │  EF Core + Pomelo (MySQL)  │
│  Services         │  │  YoutubeExplode (search)   │
│  Interfaces       │  │  YouTube Data API (comments)│
│  DTOs             │  │  Ollama (AI reasoning)     │
└─────────┬─────────┘  └─────────────┬──────────────┘
          ▼                          │
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
automatically) and YouTube OAuth replies. Everything is operated from the built-in MVC console —
the same Application services behind every page.

## The Operator Workflow

The whole point: **you type what you're promoting in plain English, and the AI does the rest.**

```
You: "A video outreach tool for freelance video editors struggling to find clients on YouTube"
    ↓
AI Planner (Campaigns → AI Plan): generates campaign → audience → problems → keywords
    ↓
YoutubeExplode: searches videos per keyword
    ↓
YouTube Data API v3: collects comments
    ↓
Ollama: qualifies each comment — is this person looking for your solution?
    ↓
Prospects (MySQL), ranked by intent score
```

Open **Campaigns → AI Plan from Description**, paste your plain-English description, and the AI
writes the campaign skeleton for you. Then run discovery from the campaign page and manage the
resulting prospects and reply campaigns in the console.

## Projects

| Project | Responsibility |
| --- | --- |
| `TrafficHunt.Domain` | Entities: Campaign, CampaignKeyword, CampaignProblem, Prospect, Video, Comment, ReplyCampaign |
| `TrafficHunt.Application` | Use cases, service + repository interfaces, DTOs |
| `TrafficHunt.Infrastructure` | MySQL (Pomelo), YoutubeExplode, YouTube Data API, Ollama; Hangfire jobs |
| `TrafficHunt.Web` | ASP.NET Core **MVC system UI** (controllers, Razor views, DI composition root) |

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


### System UI

There is no separate frontend — run the Web project and open the browser to the MVC console:

```
/                  Dashboard — global stats, recent campaigns, quick actions
/campaigns         Campaign list (create, edit, delete, AI plan)
/campaigns/{id}    Campaign detail — keywords, discovery, top prospects, reply campaigns
/prospects         Prospects, filtered by campaign / status / intent score
/replycampaigns    Reply campaigns — create from prospects, start/pause, delivery + AI analytics
/jobs              Hangfire overview — queues, recurring jobs, enqueue discovery
/hangfire          Hangfire dashboard (dev only)
```

### Running the MCP Server

Removed. TrafficHunt is operated entirely through the MVC console above.

## Hangfire Job Pipeline

Long-running work (video discovery, comment import, AI analysis) runs as **Hangfire background jobs** using the same MySQL database for persistence — jobs survive app restarts.

### Job pipeline

```
Discover → Import Comments → Analyze (Ollama) → Detect Opportunities → Notify
```

| Job | Purpose |
| --- | --- |
| `YouTubeDiscoveryJob` | Searches videos per campaign keyword (YoutubeExplode) |
| `CommentImportJob` | Collects comments via YouTube Data API |
| `CommentAnalysisJob` | Ollama AI qualification |
| `OpportunityDetectionJob` | Identifies traffic opportunities |
| `ChannelMonitoringJob` | Recurring channel monitoring |
| `NotificationJob` | User notifications |
| `MaintenanceJob` | Cleanup and reports |

### Queue separation

```
youtube queue  → 10 workers  (YouTube fetching, not the bottleneck)
ai queue       →  2 workers  (Ollama — the bottleneck)
notifications  →  5 workers
maintenance    →  1 worker
```

This prevents 100 AI jobs from overwhelming Ollama while YouTube fetching runs at full speed.

### Recurring jobs

```csharp
// Monitor a channel every 6 hours
RecurringJob.AddOrUpdate<ChannelMonitoringJob>(
    $"channel-{channelId}",
    job => job.RunAsync(channelId, campaignId),
    Cron.Hourly(6));
```

From the console, discovery is always enqueued as a Hangfire job (`BackgroundJob.Enqueue<YouTubeDiscoveryJob>`)
from the campaign detail page or the Background Jobs page — it returns a `jobId` immediately for
large-scale work, and the job page shows progress.

## System UI Pages

| Route | Purpose |
| --- | --- |
| `/` | Dashboard — global stats, recent campaigns, quick actions |
| `/campaigns` | Campaign CRUD + **AI Plan from Description** |
| `/campaigns/{id}` | Campaign detail — stats, keywords, run discovery, top prospects |
| `/prospects` | Filtered prospects by campaign / status / intent score; status updates |
| `/replycampaigns` | Reply campaigns — create, start/pause, delivery breakdown, AI analytics |
| `/jobs` | Hangfire overview — queues, recurring jobs, recent jobs, enqueue discovery |

## Roadmap

- **Milestone 1** ✅ — Campaigns → discovery → AI qualification → prospects (MVC console)
- **Milestone 2** — Reply generation, review editor, YouTube OAuth, approved replies

## Human Approval Boundary

The AI can search, analyze, score, and generate replies — but it can never publish outreach
automatically. The operator controls the final send.
