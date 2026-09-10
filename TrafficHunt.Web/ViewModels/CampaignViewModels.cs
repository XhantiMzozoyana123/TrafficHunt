using TrafficHunt.Application.Dtos;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Web.ViewModels;

/// <summary>
/// Campaign detail page (config, stats, keywords, problems, prospects, reply campaigns).
/// </summary>
public class CampaignDetailViewModel
{
    public Campaign Campaign { get; set; } = null!;
    public CampaignStats Stats { get; set; } = new();
    public List<Prospect> TopProspects { get; set; } = new();
    public List<ReplyCampaign> ReplyCampaigns { get; set; } = new();

    /// <summary>New keyword text entered on the details page.</summary>
    public string NewKeyword { get; set; } = string.Empty;
}

/// <summary>
/// Campaign list page.
/// </summary>
public class CampaignIndexViewModel
{
    public List<CampaignSummary> Campaigns { get; set; } = new();
    public int TotalProspects { get; set; }
}

/// <summary>
/// Create/edit form for a campaign. Textareas take one problem / keyword per line.
/// </summary>
public class CampaignFormViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public string ValueProposition { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public string PrimaryProblem { get; set; } = string.Empty;

    public string QualificationRules { get; set; } = string.Empty;
    public string OutreachInstructions { get; set; } = string.Empty;
    public string ReplyInstructions { get; set; } = string.Empty;

    public string Status { get; set; } = "Active";

    /// <summary>One per line (create only).</summary>
    public string ProblemsText { get; set; } = string.Empty;
    /// <summary>One per line (create only).</summary>
    public string KeywordsText { get; set; } = string.Empty;

    public bool IsEdit => Id > 0;
}

/// <summary>
/// AI-assisted campaign planning ("what are you promoting?" in plain English).
/// </summary>
public class CampaignPlanViewModel
{
    public string Description { get; set; } = string.Empty;
    public int? VideosPerKeyword { get; set; } = 3;
    public CampaignPlan? Plan { get; set; }
    public string? Error { get; set; }
    /// <summary>Hangfire job id tracking the background AI planning run.</summary>
    public string? PlanningId { get; set; }
}