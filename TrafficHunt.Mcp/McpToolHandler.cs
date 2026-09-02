using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TrafficHunt.Application.Interfaces;

namespace TrafficHunt.Mcp;

/// <summary>
/// Implements the MCP JSON-RPC 2.0 protocol over stdio with Content-Length framing.
/// Exposes TrafficHunt's Application services as MCP tools so an AI can operate the system.
/// </summary>
public partial class McpToolHandler
{
    private readonly ICampaignPlanner _planner;
    private readonly IDiscoveryService _discovery;
    private readonly IProspectService _prospects;
    private readonly ILogger<McpToolHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public McpToolHandler(
        ICampaignPlanner planner,
        IDiscoveryService discovery,
        IProspectService prospects,
        ILogger<McpToolHandler> logger)
    {
        _planner = planner;
        _discovery = discovery;
        _prospects = prospects;
        _logger = logger;
    }

    public async Task RunAsync(Stream input, Stream output, CancellationToken ct)
    {
        var reader = new StreamReader(input, Encoding.UTF8);
        var writer = new StreamWriter(output, Encoding.UTF8) { AutoFlush = true };

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;
            if (!line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) continue;

            var lengthStr = line["Content-Length:".Length..].Trim();
            if (!int.TryParse(lengthStr, out var length)) continue;

            // Read blank line
            await reader.ReadLineAsync(ct);

            var buffer = new char[length];
            var read = 0;
            while (read < length)
            {
                var r = await reader.ReadAsync(buffer, read, length - read);
                if (r == 0) break;
                read += r;
            }

            var requestJson = new string(buffer, 0, read);
            var response = await HandleRequestAsync(requestJson, ct);

            if (response != null)
            {
                var responseBytes = Encoding.UTF8.GetBytes(response);
                await writer.WriteAsync($"Content-Length: {responseBytes.Length}\r\n\r\n");
                await writer.WriteAsync(response);
                await writer.FlushAsync(ct);
            }
        }
    }

    private async Task<string?> HandleRequestAsync(string requestJson, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(requestJson);
            var root = doc.RootElement;

            var id = root.TryGetProperty("id", out var idElem) ? idElem : default;
            var method = root.GetProperty("method").GetString();
            var paramsElem = root.TryGetProperty("params", out var p) ? p : default;

            _logger.LogDebug("MCP method: {Method}", method);

            object? result = method switch
            {
                "initialize" => HandleInitialize(),
                "notifications/initialized" => null,
                "tools/list" => HandleToolsList(),
                "tools/call" => await HandleToolsCallAsync(paramsElem, ct),
                _ => new { error = new { code = -32601, message = $"Method not found: {method}" } }
            };

            if (id.ValueKind == JsonValueKind.Undefined)
                return null;

            return JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = id.ValueKind == JsonValueKind.Number ? (object?)id.GetInt64() : id.GetString(),
                result
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP request handling failed");
            return JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = (object?)null,
                error = new { code = -32603, message = ex.Message }
            }, JsonOptions);
        }
    }

    private object HandleInitialize() => new
    {
        protocolVersion = "2024-11-05",
        capabilities = new
        {
            tools = new { listChanged = false }
        },
        serverInfo = new { name = "TrafficHunt", version = "1.0.0" }
    };

    private object HandleToolsList() => new
    {
        tools = BuildToolsList()
    };

        private static object[] BuildToolsList() => new object[]
    {
        new
        {
            name = "promote",
            description = "From a plain-English description of what you're promoting, the AI generates a full campaign (product, audience, problems, discovery keywords), persists it, runs YouTube discovery to find prospects actively looking for that solution, and returns the ranked prospect list. This is the primary entry point — call it first with just a description.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    description = new { type = "string", description = "Plain-English description of what you're promoting and who it's for. E.g. 'A video outreach tool for freelance video editors struggling to find clients on YouTube'." },
                    videos_per_keyword = new { type = "integer", description = "How many videos to search per keyword (default 3).", default_value = 3 },
                    comments_per_video = new { type = "integer", description = "How many comments to collect per video (default 50).", default_value = 50 }
                },
                required = new[] { "description" }
            }
        },
        new
        {
            name = "find_prospects",
            description = "List prospects for a campaign, optionally filtered by minimum intent score or status.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    campaign_id = new { type = "integer", description = "Campaign ID." },
                    min_intent_score = new { type = "integer", description = "Minimum intent score (0-100). Defaults to 80." },
                    status = new { type = "string", description = "Filter by status: New, Contacted, Interested, Converted, Rejected." }
                },
                required = new[] { "campaign_id" }
            }
        },
        new
        {
            name = "get_prospect",
            description = "Get full details of a single prospect.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    prospect_id = new { type = "integer", description = "Prospect ID." }
                },
                required = new[] { "prospect_id" }
            }
        },
        new
        {
            name = "generate_reply",
            description = "Generate a personalized outreach reply for a prospect using the campaign context.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    prospect_id = new { type = "integer", description = "Prospect ID." }
                },
                required = new[] { "prospect_id" }
            }
        },
        new
        {
            name = "update_prospect_status",
            description = "Update a prospect's status in the pipeline.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    prospect_id = new { type = "integer", description = "Prospect ID." },
                    status = new { type = "string", description = "New status: New, Contacted, Interested, Converted, Rejected." }
                },
                required = new[] { "prospect_id", "status" }
            }
        },
        new
        {
            name = "get_campaign",
            description = "Get campaign configuration and stats.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    campaign_id = new { type = "integer", description = "Campaign ID." }
                },
                required = new[] { "campaign_id" }
            }
        }
    };

    private async Task<object> HandleToolsCallAsync(JsonElement paramsElem, CancellationToken ct)
    {
        var name = paramsElem.GetProperty("name").GetString();
        var arguments = paramsElem.TryGetProperty("arguments", out var args) ? args : default;

        _logger.LogInformation("MCP tool call: {Tool}", name);

        try
        {
            return name switch
            {
                "promote" => await CallPromoteAsync(arguments, ct),
                "find_prospects" => await CallFindProspectsAsync(arguments, ct),
                "get_prospect" => await CallGetProspectAsync(arguments, ct),
                "generate_reply" => await CallGenerateReplyAsync(arguments, ct),
                "update_prospect_status" => await CallUpdateProspectStatusAsync(arguments, ct),
                "get_campaign" => await CallGetCampaignAsync(arguments, ct),
                _ => new { content = new object[] { new { type = "text", text = $"Unknown tool: {name}" } }, isError = true }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool {Tool} failed", name);
            return new { content = new object[] { new { type = "text", text = ex.Message } }, isError = true };
        }
    }

    private async Task<object> CallPromoteAsync(JsonElement args, CancellationToken ct)
    {
        var description = GetString(args, "description") ?? throw new ArgumentException("description is required");
        var videosPerKeyword = GetInt(args, "videos_per_keyword", 3);
        var commentsPerVideo = GetInt(args, "comments_per_video", 50);

        var plan = await _planner.PlanAsync(description, ct);

        var discoveredCount = 0;
        await foreach (var _ in _discovery.RunDiscoveryAsync(plan.CampaignId, videosPerKeyword, commentsPerVideo, ct))
        {
            discoveredCount++;
        }

        var topProspects = await _prospects.GetByCampaignAsync(plan.CampaignId, minIntentScore: 80, ct: ct);

        return new
        {
            content = new object[]
            {
                new
                {
                    type = "text",
                    text = JsonSerializer.Serialize(new
                    {
                        message = "Campaign created and discovery complete.",
                        campaign_id = plan.CampaignId,
                        campaign_name = plan.Draft.Name,
                        product = plan.Draft.ProductName,
                        target_audience = plan.Draft.TargetAudience,
                        keywords_used = plan.Draft.Keywords,
                        total_prospects_found = discoveredCount,
                        high_intent_prospects = topProspects.Count,
                        top_prospects = topProspects.Select(p => new
                        {
                            p.Id,
                            p.AuthorName,
                            p.CommentText,
                            p.IntentScore,
                            p.PainPoint,
                            p.Status
                        })
                    }, JsonOptions)
                }
            }
        };
    }

    private async Task<object> CallFindProspectsAsync(JsonElement args, CancellationToken ct)
    {
        var campaignId = GetInt(args, "campaign_id", -1);
        var minScore = args.TryGetProperty("min_intent_score", out var s) ? s.GetInt32() : (int?)null;
        var status = GetString(args, "status");

        var prospects = await _prospects.GetByCampaignAsync(campaignId, status, minScore, ct);
        return new { content = new object[] { new { type = "text", text = JsonSerializer.Serialize(prospects, JsonOptions) } } };
    }

    private async Task<object> CallGetProspectAsync(JsonElement args, CancellationToken ct)
    {
        var id = GetInt(args, "prospect_id", -1);
        var prospect = await _prospects.GetByIdAsync(id, ct);
        return new { content = new object[] { new { type = "text", text = prospect != null ? JsonSerializer.Serialize(prospect, JsonOptions) : "Prospect not found." } } };
    }

    private async Task<object> CallGenerateReplyAsync(JsonElement args, CancellationToken ct)
    {
        var prospectId = GetInt(args, "prospect_id", -1);
        return new { content = new object[] { new { type = "text", text = $"Reply generation for prospect {prospectId} will use campaign context and Ollama. (Milestone 2)" } } };
    }

    private async Task<object> CallUpdateProspectStatusAsync(JsonElement args, CancellationToken ct)
    {
        var prospectId = GetInt(args, "prospect_id", -1);
        var status = GetString(args, "status") ?? throw new ArgumentException("status is required");

        var updated = await _prospects.UpdateStatusAsync(prospectId, status, ct);
        return new { content = new object[] { new { type = "text", text = updated != null ? JsonSerializer.Serialize(updated, JsonOptions) : "Prospect not found." } } };
    }

    private async Task<object> CallGetCampaignAsync(JsonElement args, CancellationToken ct)
    {
        var campaignId = GetInt(args, "campaign_id", -1);
        var stats = await _prospects.GetGlobalStatsAsync(ct);
        return new { content = new object[] { new { type = "text", text = JsonSerializer.Serialize(new { campaign_id = campaignId, global_stats = stats }, JsonOptions) } } };
    }

    private static string? GetString(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString();
        return null;
    }

    private static int GetInt(JsonElement element, string property, int defaultValue)
    {
        if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number)
            return value.GetInt32();
        return defaultValue;
    }
}
