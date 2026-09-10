using Microsoft.AspNetCore.Mvc;
using TrafficHunt.Web.Logging;

namespace TrafficHunt.Web.ViewModels;

/// <summary>Initial server-rendered payload for the /Logs page.</summary>
public class LogsIndexViewModel
{
    public IList<LogEntry> Entries { get; set; } = new List<LogEntry>();
    public const int DefaultPageSize = 200;
}
