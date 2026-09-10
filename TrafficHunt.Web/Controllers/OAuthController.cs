using Microsoft.AspNetCore.Mvc;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Infrastructure.YouTube;

namespace TrafficHunt.Web.Controllers;

/// <summary>
/// Google OAuth 2.0 authorization-code flow for YouTube reply publishing.
/// Redirects to Google consent, exchanges the code and stores the tokens in appsettings.json.
/// Scope: youtube.force-ssl (comment insert/reply). Operator must register the redirect URI
/// (e.g. http://localhost:52956/oauth/callback) on the Google Cloud OAuth client.
/// </summary>
public class OAuthController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ISettingsStoreFactory _storeFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public OAuthController(
        IConfiguration configuration,
        ISettingsStoreFactory storeFactory,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _storeFactory = storeFactory;
        _httpClientFactory = httpClientFactory;
    }

    private string? ClientId =>
        _configuration["GoogleAuth:ClientId"] ?? _configuration["YouTube:OAuth:ClientId"];
    private string? ClientSecret =>
        _configuration["GoogleAuth:ClientSecret"] ?? _configuration["YouTube:OAuth:ClientSecret"];

    private string CallbackUri =>
        $"{Request.Scheme}://{Request.Host}/oauth/callback";

    /// <summary>Redirects the browser to Google's consent screen.</summary>
    [HttpGet]
    public IActionResult Login()
    {
        if (string.IsNullOrEmpty(ClientId))
        {
            TempData["Error"] = "No OAuth client id configured. Add it on the Settings page first.";
            return RedirectToAction("Index", "Settings");
        }

        var state = GoogleOAuthHelper.RandomState();
        TempData["oauth_state"] = state;

        var url = GoogleOAuthHelper.BuildAuthorizeUrl(ClientId!, CallbackUri, state);
        return Redirect(url);
    }

    /// <summary>Handles Google's redirect: exchanges the code and stores access + refresh tokens.</summary>
    [HttpGet]
    public async Task<IActionResult> Callback(string? code, string? state, string? error, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(error))
        {
            TempData["Error"] = $"Google OAuth was declined: {error}";
            return RedirectToAction("Index", "Settings");
        }

        var expectedState = TempData["oauth_state"] as string;
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state) || state != expectedState)
        {
            TempData["Error"] = "OAuth state mismatch or missing code — login aborted.";
            return RedirectToAction("Index", "Settings");
        }

        if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
        {
            TempData["Error"] = "OAuth client id/secret missing — complete them on the Settings page.";
            return RedirectToAction("Index", "Settings");
        }

        try
        {
            var http = _httpClientFactory.CreateClient();
            var tokens = await GoogleOAuthHelper.ExchangeCodeAsync(
                http, ClientId!, ClientSecret!, CallbackUri, code, ct);

            var store = _storeFactory.Create();
            store.Set("YouTube:OAuth:AccessToken", tokens["access_token"]?.GetValue<string>());
            var refresh = tokens["refresh_token"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(refresh))
                store.Set("YouTube:OAuth:RefreshToken", refresh); // Google only returns it on first consent
            var expiresIn = tokens["expires_in"]?.GetValue<int>() ?? 3600;
            store.Set("YouTube:OAuth:ExpiresAtUtc",
                DateTime.UtcNow.AddSeconds(expiresIn - 60).ToString("O"));
            store.Save();

            TempData["Success"] = "Google account connected. Approved replies will now publish to YouTube.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"OAuth token exchange failed: {ex.Message}";
        }

        return RedirectToAction("Index", "Settings");
    }

    /// <summary>Disconnects: clears stored tokens.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Disconnect()
    {
        var store = _storeFactory.Create();
        store.Set("YouTube:OAuth:AccessToken", null);
        store.Set("YouTube:OAuth:RefreshToken", null);
        store.Set("YouTube:OAuth:ExpiresAtUtc", null);
        store.Save();
        TempData["Success"] = "Google account disconnected.";
        return RedirectToAction("Index", "Settings");
    }
}