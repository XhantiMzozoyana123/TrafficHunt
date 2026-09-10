using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using TrafficHunt.Application.Interfaces;

namespace TrafficHunt.Infrastructure.YouTube;

/// <summary>
/// Publishes YouTube comment replies via the official Google APIs client library:
///   YouTubeService.Comments.Insert  ->  POST /youtube/v3/comments?part=snippet
///   { "snippet": { "parentId": "<comment id>", "textOriginal": "<plain text>" } }
///
/// Authentication uses the stored OAuth2 refresh token (scope youtube.force-ssl)
/// wrapped in a <see cref="UserCredential"/>, which refreshes access tokens
/// automatically â€” including from background jobs with no HTTP context.
/// </summary>
public class GoogleReplySender : IYouTubeReplySender
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly ISettingsStoreFactory _settingsStoreFactory;

    public GoogleReplySender(HttpClient http, IConfiguration configuration, ISettingsStoreFactory settingsStoreFactory)
    {
        _http = http;
        _configuration = configuration;
        _settingsStoreFactory = settingsStoreFactory;
    }

    private string? AccessToken => _configuration["YouTube:OAuth:AccessToken"];
    private string? RefreshToken => _configuration["YouTube:OAuth:RefreshToken"];

    // Credentials: prefer the GoogleAuth section, fall back to YouTube:OAuth keys.
    private string? ClientId =>
        _configuration["GoogleAuth:ClientId"] ?? _configuration["YouTube:OAuth:ClientId"];
    private string? ClientSecret =>
        _configuration["GoogleAuth:ClientSecret"] ?? _configuration["YouTube:OAuth:ClientSecret"];

    public bool IsConnected => !string.IsNullOrEmpty(RefreshToken) && !string.IsNullOrEmpty(ClientId);

    public async Task<string> SendReplyAsync(string videoId, string parentCommentId, string textOriginal, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(parentCommentId))
            throw new ArgumentException("Parent comment id is required.", nameof(parentCommentId));
        if (string.IsNullOrWhiteSpace(textOriginal))
            throw new ArgumentException("Reply text is required.", nameof(textOriginal));

        // YouTube's comments.insert only supports one reply level: the parentId must
        // be a TOP-LEVEL comment id. Prospects collected from reply threads have ids
        // like "Ugx....ABAg.ADkwSHu4MyzAWfunoQFdXf" (reply-to-reply); strip the
        // ".replyId" suffix so we reply to the parent thread instead — otherwise the
        // API rejects the request with 400 BadRequest.
        var dot = parentCommentId.IndexOf('.');
        var topLevelId = dot > 0 ? parentCommentId[..dot] : parentCommentId;

        var credential = await GetCredentialAsync(ct);

        using var service = new YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "TrafficHunt"
        });

        var comment = new Google.Apis.YouTube.v3.Data.Comment
        {
            Snippet = new Google.Apis.YouTube.v3.Data.CommentSnippet
            {
                ParentId = topLevelId,
                TextOriginal = textOriginal
            }
        };

        try
        {
            var response = await service.Comments.Insert(comment, "snippet").ExecuteAsync(ct);
            return response.Id ?? string.Empty;
        }
        catch (Google.GoogleApiException ex)
        {
            throw new InvalidOperationException(
                $"YouTube comments.insert failed ({ex.HttpStatusCode}): {Truncate(ex.Message, 400)}", ex);
        }
    }

    /// <summary>
    /// Builds a <see cref="UserCredential"/> from the stored refresh token. The Google
    /// client library refreshes the access token transparently whenever it expires.
    /// </summary>
    private Task<UserCredential> GetCredentialAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(RefreshToken) || string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
            throw new InvalidOperationException(
                "YouTube OAuth is not connected. Open Settings and connect a Google account.");

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = ClientId, ClientSecret = ClientSecret },
            });

        var token = new TokenResponse
        {
            AccessToken = AccessToken,
            RefreshToken = RefreshToken
        };

        var expiryRaw = _configuration["YouTube:OAuth:ExpiresAtUtc"];
        if (DateTime.TryParse(expiryRaw, out var expiry) && expiry > DateTime.UtcNow)
            token.IssuedUtc = expiry.AddSeconds(-3600); // issued an hour before stored expiry

        return Task.FromResult(new UserCredential(flow, "operator", token));
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "\u2026";
}

/// <summary>Static helpers for the OAuth authorization-code flow (used by the Web OAuth controller).</summary>
public static class GoogleOAuthHelper
{
    public const string Scope = "https://www.googleapis.com/auth/youtube.force-ssl";

    public static string BuildAuthorizeUrl(string clientId, string redirectUri, string state) =>
        "https://accounts.google.com/o/oauth2/v2/auth" +
        $"?client_id={Uri.EscapeDataString(clientId)}" +
        $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
        "&response_type=code" +
        $"&scope={Uri.EscapeDataString(Scope)}" +
        "&access_type=offline&prompt=consent&include_granted_scopes=true" +
        $"&state={Uri.EscapeDataString(state)}";

    public static string RandomState(int bytes = 16) =>
        Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(bytes));

    /// <summary>Exchanges an authorization code for tokens. Returns the raw token JSON.</summary>
    public static async Task<JsonObject> ExchangeCodeAsync(
        HttpClient http, string clientId, string clientSecret, string redirectUri, string code, CancellationToken ct = default)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri
        });
        using var resp = await http.PostAsync("https://oauth2.googleapis.com/token", content, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"OAuth token exchange failed ({(int)resp.StatusCode}): {(body.Length > 400 ? body[..400] + "\u2026" : body)}");
        return JsonNode.Parse(body)!.AsObject();
    }
}
