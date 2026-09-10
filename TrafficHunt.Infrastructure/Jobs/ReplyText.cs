/// <summary>Shared reply-text helpers (also used by the service fallback path).</summary>
internal static class ReplyText
{
    internal static string EnsureCampaignLink(string message, string? productUrl)
    {
        if (string.IsNullOrWhiteSpace(productUrl)) return message;
        var url = productUrl.Trim();
        if (message.Contains(url, StringComparison.OrdinalIgnoreCase)) return message;
        var domain = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? url[(url.IndexOf("//", StringComparison.Ordinal) + 2)..] : url;
        domain = domain.TrimEnd('/');
        if (message.Contains(domain, StringComparison.OrdinalIgnoreCase)) return message;
        if (string.IsNullOrWhiteSpace(message)) return url;
        var tail = message.Length <= 80 ? message : message[^80..];
        if (tail.Contains("my site", StringComparison.OrdinalIgnoreCase) ||
            tail.Contains("this site", StringComparison.OrdinalIgnoreCase) ||
            tail.Contains("the guide", StringComparison.OrdinalIgnoreCase) ||
            tail.Contains("write-up", StringComparison.OrdinalIgnoreCase))
            return $"{message.Trim()} {url}";
        return $"{message.Trim()}\n\nI wrote up how I fixed this on my site — {url}";
    }

    internal static string Clean(string reply)
    {
        reply = reply.Trim();
        if (reply.StartsWith("```"))
        {
            var start = reply.IndexOf('\n');
            if (start >= 0) reply = reply[(start + 1)..];
            var end = reply.LastIndexOf("```", StringComparison.Ordinal);
            if (end > 0) reply = reply[..end];
            reply = reply.Trim();
        }
        if (reply.StartsWith('{') || reply.StartsWith('['))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(reply);
                var longest = Longest(doc.RootElement);
                if (!string.IsNullOrWhiteSpace(longest)) reply = longest;
            }
            catch { /* not JSON — keep as-is */ }
        }
        if (reply.Length > 1 && reply[0] == '"' && reply[^1] == '"')
            reply = reply[1..^1];
        return reply.Trim();
    }

    private static string? Longest(System.Text.Json.JsonElement element)
    {
        string? best = null;
        void Visit(System.Text.Json.JsonElement el)
        {
            switch (el.ValueKind)
            {
                case System.Text.Json.JsonValueKind.String:
                    var s = el.GetString();
                    if (!string.IsNullOrWhiteSpace(s) && (best == null || s.Length > best.Length)) best = s;
                    break;
                case System.Text.Json.JsonValueKind.Object:
                    foreach (var p in el.EnumerateObject()) Visit(p.Value);
                    break;
                case System.Text.Json.JsonValueKind.Array:
                    foreach (var item in el.EnumerateArray()) Visit(item);
                    break;
            }
        }
        Visit(element);
        return best;
    }
}
