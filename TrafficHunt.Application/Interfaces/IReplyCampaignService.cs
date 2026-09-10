using TrafficHunt.Application.Dtos;
using TrafficHunt.Domain.Entities;

namespace TrafficHunt.Application.Interfaces;

public interface IReplyCampaignService
{
    Task<List<ReplyCampaign>> GetAllAsync(CancellationToken ct = default);
    Task<ReplyCampaign> CreateAsync(CreateReplyCampaignRequest request, CancellationToken ct = default);
    Task<ReplyCampaign?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<ReplyCampaign>> GetByCampaignAsync(int campaignId, CancellationToken ct = default);
    Task StartAsync(int replyCampaignId, CancellationToken ct = default);
    Task PauseAsync(int replyCampaignId, CancellationToken ct = default);

    /// <summary>
    /// Resumes a paused reply campaign. Skips re-drafting already-approved replies
    /// (unlike StartAsync which re-approves everything), so it returns quickly.
    /// Only drafts replies still in Pending status before going live.
    /// </summary>
    Task ResumeAsync(int replyCampaignId, CancellationToken ct = default);

    /// <summary>
    /// Sends exactly one approved reply for the campaign and updates its stats,
    /// directly (no background job). Returns the sent record, or null when the
    /// campaign is not running or there is nothing approved to send.
    /// </summary>
    Task<ReplyRecord?> SendNextReplyAsync(int replyCampaignId, CancellationToken ct = default);

    /// <summary>
    /// Runs the campaign directly (no Hangfire background job): while the campaign is
    /// Running it drafts + sends the next reply (AI drafting happens just-in-time and
    /// interleaves with sending) and paces each send with the configured anti-spam delay.
    /// Marks the campaign Completed when there is nothing left to send. Returns when the
    /// campaign pauses, runs out of work, or finishes.
    /// </summary>
    Task RunAllAsync(int replyCampaignId, CancellationToken ct = default);
    Task<ReplyCampaignStats> GetStatsAsync(int replyCampaignId, CancellationToken ct = default);
    Task<CampaignAnalytics> GetAnalyticsAsync(int replyCampaignId, CancellationToken ct = default);
    Task<ReplyRecord?> GetNextPendingAsync(int replyCampaignId, CancellationToken ct = default);

    /// <summary>
    /// Draft + approve a pending reply: picks a template via rotation, personalizes it for the
    /// prospect and stores the final message. Human-approved replies move to Approved status and
    /// are the only records the send job will publish.
    /// </summary>
    Task<ReplyRecord?> ApproveAsync(int replyRecordId, CancellationToken ct = default);

    /// <summary>Drafts + approves all pending replies in a reply campaign. Returns how many were approved.</summary>
    Task<int> ApproveAllAsync(int replyCampaignId, CancellationToken ct = default);

    /// <summary>
    /// Resets all Failed reply records back to Pending so they are re-drafted (with a
    /// fresh AI message) and re-sent. Adjusts the campaign counters and flips the
    /// campaign back to Running. Returns how many records were reset.
    /// </summary>
    Task<int> RetryFailedAsync(int replyCampaignId, CancellationToken ct = default);

    /// <summary>
    /// Updates a reply campaign's editable properties (name + anti-spam delay range).
    /// Returns false when the campaign does not exist; throws ArgumentException on
    /// invalid input.
    /// </summary>
    Task<bool> UpdateAsync(int replyCampaignId, string name, int minDelaySeconds, int maxDelaySeconds, CancellationToken ct = default);

    /// <summary>
    /// Deletes a reply campaign and all its templates + reply records. Pauses a
    /// Running campaign first so the background send loop stops cleanly.
    /// Returns false when the campaign does not exist.
    /// </summary>
    Task<bool> DeleteAsync(int replyCampaignId, CancellationToken ct = default);
}
