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

## The Operator Workflow

The whole point: **you type what you're promoting in plain English, and the AI does the rest.**

```
You: "A video outreach tool for freelance video editors struggling to find clients on YouTube"
    ↓
AI (via MCP): generates campaign → audience → problems → keywords
    ↓
YoutubeExplode: searches videos per keyword
    ↓
YouTube Data API v3: collects comments
    ↓
Ollama: qualifies each comment — is this person looking for your solution?
    ↓
Prospects (MySQL), ranked by intent score
```

The MCP server (`TrafficHunt.Mcp`) exposes this as a single `promote` tool — the AI calls it with your description and gets back the ranked prospect list. No manual keyword editing, no hand-crafting search terms.

## Projects

| Project | Responsibility |
| --- | --- |
| `TrafficHunt.Domain` | Entities: Campaign, CampaignKeyword, CampaignProblem, Prospect, Video, Comment |
| `TrafficHunt.Application` | Use cases, service + repository interfaces, DTOs |
| `TrafficHunt.Infrastructure` | MySQL (Pomelo), YoutubeExplode, YouTube Data API, Ollama |
| `TrafficHunt.Web` | ASP.NET Core controllers, DI composition root |
| `TrafficHunt.Mcp` | MCP server — AI operator interface (JSON-RPC over stdio) |
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

### Running the MCP Server

The MCP server is a console app that speaks JSON-RPC over stdio. Point your AI client (e.g. Claude Desktop, Cursor, VS Code) at it:

```powershell
dotnet run --project TrafficHunt.Mcp
```

Or configure it in your client's settings (example for Claude Desktop's `claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "TrafficHunt": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Users\\Xhanti\\source\\repos\\TrafficHunt\\TrafficHunt.Mcp"]
    }
  }
}
```

### MCP Tools

| Tool | Purpose |
| --- | --- |
| `promote` | **Primary entry point.** Give it a plain-English description of what you're promoting — the AI generates the campaign, runs discovery, and returns ranked prospects. |
| `find_prospects` | List prospects for a campaign (filter by intent score or status). |
| `get_prospect` | Get full details of a single prospect. |
| `generate_reply` | Generate a personalized outreach reply (Milestone 2). |
| `update_prospect_status` | Move a prospect through the pipeline (New → Contacted → Interested → Converted/Rejected). |
| `get_campaign` | Get campaign configuration and global stats. |

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

### MCP + Hangfire

The MCP `promote` tool can either run discovery inline (small campaigns) or **enqueue a Hangfire job** and return a `jobId` immediately for large-scale work — the AI then polls `get_job_status`.

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
- **Milestone 3** ✅ — MCP server: the AI as TrafficHunt operator (`promote` drives the whole pipeline)

## Human Approval Boundary

The AI can search, analyze, score, and generate replies — but it can never publish outreach
automatically. The operator controls the final send.
