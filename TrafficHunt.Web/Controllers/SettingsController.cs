using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using TrafficHunt.Web.Services;
using TrafficHunt.Web.ViewModels;

namespace TrafficHunt.Web.Controllers;

/// <summary>
/// System settings page — view and edit API keys and service endpoints
/// (YouTube, Ollama, database connection). Changes are written to appsettings.json.
/// </summary>
public class SettingsController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public SettingsController(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _env = env;
    }

    private string SettingsPath => Path.Combine(_env.ContentRootPath, "appsettings.json");

    public IActionResult Index()
    {
        var model = new SettingsViewModel
        {
            YouTubeApiKey = _configuration["YouTube:ApiKey"] ?? string.Empty,
            OllamaBaseUrl = _configuration["Ollama:BaseUrl"] ?? string.Empty,
            OllamaModel = _configuration["Ollama:Model"] ?? string.Empty,
            ConnectionString = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
            OAuthClientId = _configuration["GoogleAuth:ClientId"] ?? _configuration["YouTube:OAuth:ClientId"] ?? string.Empty,
            OAuthClientSecret = _configuration["GoogleAuth:ClientSecret"] ?? _configuration["YouTube:OAuth:ClientSecret"] ?? string.Empty,
            OAuthConnected = !string.IsNullOrEmpty(_configuration["YouTube:OAuth:RefreshToken"]),
            RedirectUri = $"{Request.Scheme}://{Request.Host}/oauth/callback"
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Save(SettingsViewModel model)
    {
        var json = JsonNode.Parse(System.IO.File.ReadAllText(SettingsPath));
        if (json is not JsonObject root)
        {
            TempData["Error"] = "Could not parse appsettings.json.";
            return RedirectToAction(nameof(Index));
        }

        root["YouTube"] ??= new JsonObject();
        root["YouTube"]!["ApiKey"] = model.YouTubeApiKey?.Trim();

        root["Ollama"] ??= new JsonObject();
        root["Ollama"]!["BaseUrl"] = model.OllamaBaseUrl?.Trim();
        root["Ollama"]!["Model"] = model.OllamaModel?.Trim();

        root["ConnectionStrings"] ??= new JsonObject();
        root["ConnectionStrings"]!["DefaultConnection"] = model.ConnectionString?.Trim();

        System.IO.File.WriteAllText(SettingsPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        TempData["Success"] = "Settings saved. Restart the app for the LLM/YouTube changes to take effect.";
        return RedirectToAction(nameof(Index));
    }
}