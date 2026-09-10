using Hangfire;
using Google.Apis.Auth.AspNetCore3;
using Google.Apis.YouTube.v3;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TrafficHunt.Application;
using TrafficHunt.Infrastructure;
using TrafficHunt.Infrastructure.Jobs;
using TrafficHunt.Web.Logging;

var builder = WebApplication.CreateBuilder(args);

// ---- MVC system UI ----
builder.Services.AddControllersWithViews();

// ---- Google authentication: cookie sign-in + Google OpenID Connect with the
// ---- youtube.force-ssl scope. IGoogleAuthProvider is injectable in controllers
// ---- for interactive Google API calls; background jobs use the stored refresh
// ---- token via GoogleReplySender instead.
builder.Services
    .AddAuthentication(o =>
    {
        o.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        o.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = GoogleOpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddGoogleOpenIdConnect(options =>
    {
        options.ClientId = builder.Configuration["GoogleAuth:ClientId"];
        options.ClientSecret = builder.Configuration["GoogleAuth:ClientSecret"];
        options.Scope.Add(YouTubeService.Scope.YoutubeForceSsl);
    });

// ---- Composition root: layers wired here only ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---- Settings persistence (API keys + OAuth tokens in appsettings.json) ----
builder.Services.AddSingleton<TrafficHunt.Application.Interfaces.ISettingsStoreFactory>(_ =>
    new TrafficHunt.Web.Services.AppSettingsStoreFactory(
        Path.Combine(builder.Environment.ContentRootPath, "appsettings.json")));

// ---- Hangfire: background job orchestration ----
builder.Services.AddHangfire(config => config
    .UseRecommendedSerializerSettings()
    .UseInMemoryStorage());

// Add Hangfire server with queue configuration
// Queues: youtube (fetching), ai (Ollama - limited workers), outreach, notifications, maintenance, default
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 10;
    options.Queues = new[] { "youtube", "ai", "outreach", "notifications", "maintenance", "default" };
    options.SchedulePollingInterval = TimeSpan.FromSeconds(5);
});

// Register all job classes (so they can be resolved by Hangfire via DI)
builder.Services.AddScoped<YouTubeDiscoveryJob>();
builder.Services.AddScoped<CommentImportJob>();
builder.Services.AddScoped<CommentAnalysisJob>();
builder.Services.AddScoped<OpportunityDetectionJob>();
builder.Services.AddScoped<NotificationJob>();
builder.Services.AddScoped<ChannelMonitoringJob>();
builder.Services.AddScoped<MaintenanceJob>();

// ---- Live log streaming (UI request logs + background-job logs) ----
// The store is a singleton shared by the request middleware, the ILogger
// provider below, and the Hangfire job filter (added further down).
var logStore = new LogStore();
builder.Services.AddSingleton(logStore);

// Mirror every ILogger<T> record from TrafficHunt.* code (jobs, services, …)
// into the in-memory store so they show up live in the operator console.
// Console/Debug output is left untouched.
builder.Logging.AddProvider(new LogStoreLoggerProvider(
    logStore, LogLevel.Information));

// Record Hangfire job lifecycle (started/completed/failed) with the Hangfire
// job id and the reply-campaign id, so the live log shows a real-time timeline.
GlobalJobFilters.Filters.Add(new HangfireJobLogFilter(logStore));

var app = builder.Build();

// Apply pending EF migrations on startup when AutoMigrate=true (container-friendly).
// The app uses EF Core Code-First with committed migrations; a fresh VPS database
// needs them applied before the first request. Disabled by default.
if (string.Equals(builder.Configuration["AutoMigrate"], "true", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TrafficHunt.Infrastructure.Persistence.TrafficHuntDbContext>();
    await db.Database.MigrateAsync();
}

// Outer-most: capture every inbound HTTP request, including failed ones.
app.UseMiddleware<RequestLoggingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ---- HTTP -> HTTPS redirect ----
// In containers/reverse-proxy setups TLS is usually terminated upstream, so the
// app itself runs on HTTP. Allow opting out of the redirect via an env var — the
// middleware throws if no HTTPS endpoint is configured (it would crash a
// plain-HTTP container otherwise). Set DisableHttpsRedirect=true for those setups.
if (!string.Equals(builder.Configuration["DisableHttpsRedirect"], "true", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ---- Hangfire Dashboard (dev only) ----
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter() }
    });
}

// ---- Schedule recurring jobs ----
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // Daily maintenance at 2 AM
    recurringJobManager.AddOrUpdate<MaintenanceJob>(
        "daily-cleanup",
        job => job.RunDailyCleanupAsync(),
        Cron.Daily(2, 0),
        new RecurringJobOptions { QueueName = "maintenance" });
}

app.Run();
