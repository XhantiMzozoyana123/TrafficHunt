using Hangfire;
using TrafficHunt.Application;
using TrafficHunt.Infrastructure;
using TrafficHunt.Infrastructure.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ---- Composition root: layers wired here only ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---- Hangfire: background job orchestration ----
builder.Services.AddHangfire(config => config
    .UseRecommendedSerializerSettings()
    .UseInMemoryStorage());

// Add Hangfire server with queue configuration
// Queues: youtube (fetching), ai (Ollama - limited workers), notifications, maintenance, default
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 5;
    options.Queues = new[] { "youtube", "ai", "notifications", "maintenance", "default" };
    options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
});

// Register all job classes (so they can be resolved by Hangfire via DI)
builder.Services.AddScoped<YouTubeDiscoveryJob>();
builder.Services.AddScoped<CommentImportJob>();
builder.Services.AddScoped<CommentAnalysisJob>();
builder.Services.AddScoped<OpportunityDetectionJob>();
builder.Services.AddScoped<NotificationJob>();
builder.Services.AddScoped<ChannelMonitoringJob>();
builder.Services.AddScoped<MaintenanceJob>();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
          .AllowAnyHeader()
          .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.MapControllers();

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
