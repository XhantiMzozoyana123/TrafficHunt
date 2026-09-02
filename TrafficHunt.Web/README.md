# TrafficHunt Backend

Private AI-powered customer acquisition and outreach API. Not a SaaS — single operator, no auth/tenancy.

## Stack

- ASP.NET Core Web API (.NET 10)
- MySQL + EF Core (Pomelo)
- YoutubeExplode (discovery — search videos, collect comments)
- Ollama (AI qualification + reply generation)
- MCP server stubs in `Mcp/` (Milestone 3)

## Setup

1. Create the database (EF migrations or `context.Database.EnsureCreated()`):

   ```powershell
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

2. Edit `appsettings.json` — set your MySQL password. For local secrets:

   ```powershell
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=traffichunt;User=root;Password=..."
   ```

3. Install Ollama (https://ollama.com) and pull a model:

   ```powershell
   ollama pull llama3.1
   ```

4. Run:

   ```powershell
   dotnet run
   ```

## API surface (Milestone 1)

- `GET/POST/PUT/DELETE /api/campaigns` — campaign CRUD
- `POST /api/campaigns/{id}/keywords` · `DELETE /api/campaigns/keywords/{id}`
- `GET /api/campaigns/{id}/stats`
- `GET /api/prospects?campaignId=&status=&minIntentScore=`
- `PATCH /api/prospects/{id}/status`
- `GET /api/prospects/stats/global`
- `POST /api/discovery/{campaignId}/run` — SSE stream of discovery progress

## Pipeline

```
Campaign keywords → YoutubeExplode search → collect comments
    → Ollama qualification (structured JSON) → Prospects (MySQL)
```

## Human approval boundary

The AI can search, analyze, score and generate replies, but never publishes
outreach automatically. Sending replies (Milestone 2, YouTube Data API + OAuth)
always requires explicit user approval.
