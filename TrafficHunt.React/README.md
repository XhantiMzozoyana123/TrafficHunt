# TrafficHunt React Frontend

React 19 + TypeScript + Vite frontend for the TrafficHunt engine.

## Run

```powershell
npm install
npm run dev
```

Dev server proxies `/api` to `http://localhost:5000` (set `VITE_API_URL` to override).
Start the backend first: `dotnet run` in `../TrafficHunt.Web`.

## Structure

```
src/
├── pages/          Dashboard, Campaigns, CampaignDetails, Prospects
├── components/     Sidebar, CampaignCard, ProspectCard, IntentScore
├── services/       api.ts, campaignService.ts, prospectService.ts
└── types.ts
```
