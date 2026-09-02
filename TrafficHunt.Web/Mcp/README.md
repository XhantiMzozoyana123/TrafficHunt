# Milestone 3 — MCP Server

TrafficHunt exposes its capabilities to an AI through a .NET MCP server so the AI
can operate the engine as an operator:

- `get_campaign` — campaign configuration and promotion context
- `search_youtube` — search YouTube using a campaign keyword
- `collect_video_comments` — collect comments from a discovered video
- `analyze_comment` — qualify a comment against campaign targeting
- `find_prospects` — find prospects matching campaign criteria
- `get_prospect` — full prospect information
- `generate_reply` — personalized outreach reply
- `update_prospect` — change prospect status
- `prepare_youtube_reply` — stage a reply for user approval
- `send_youtube_reply` — REQUIRES explicit user confirmation (never automated)

MCP is another interface into the same C# services used by the REST API —
it does not replace them:

    React  -> API  -> Service
    AI     -> MCP  -> Service
