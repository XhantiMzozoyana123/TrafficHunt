import React, { useState } from 'react';
import { 
  Users, 
  ExternalLink, 
  Target, 
  MessageSquare, 
  Sparkles, 
  SendHorizontal, 
  CheckCircle2, 
  Clock, 
  Filter,
  Youtube,
  AlertCircle
} from 'lucide-react';
import { Prospect, ProspectStatus, Campaign } from '../types';

interface ProspectsViewProps {
  prospects: Prospect[];
  campaigns: Campaign[];
  selectedCampaignId?: string;
  onSelectCampaignId: (id: string | undefined) => void;
  onUpdateStatus: (id: string, status: ProspectStatus) => void;
  onOpenReview: (prospect: Prospect) => void;
  onDraftReply: (prospect: Prospect) => void;
}

export const ProspectsView: React.FC<ProspectsViewProps> = ({
  prospects,
  campaigns,
  selectedCampaignId,
  onSelectCampaignId,
  onUpdateStatus,
  onOpenReview,
  onDraftReply
}) => {
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [minScoreFilter, setMinScoreFilter] = useState<number>(75);

  const filteredProspects = prospects.filter((p) => {
    if (selectedCampaignId && p.campaignId !== selectedCampaignId) return false;
    if (statusFilter !== 'all' && p.status !== statusFilter) return false;
    if (p.intentScore < minScoreFilter) return false;
    return true;
  });

  const getScoreBadge = (score: number) => {
    if (score >= 90) {
      return (
        <span className="px-2 py-0.5 rounded-full bg-emerald-500/20 text-emerald-300 border border-emerald-500/30 text-xs font-mono font-bold">
          {score}% Intent
        </span>
      );
    }
    if (score >= 80) {
      return (
        <span className="px-2 py-0.5 rounded-full bg-cyan-500/20 text-cyan-300 border border-cyan-500/30 text-xs font-mono font-bold">
          {score}% Intent
        </span>
      );
    }
    return (
      <span className="px-2 py-0.5 rounded-full bg-amber-500/20 text-amber-300 border border-amber-500/30 text-xs font-mono font-bold">
        {score}% Intent
      </span>
    );
  };

  const getStatusBadge = (status: ProspectStatus) => {
    switch (status) {
      case 'new':
        return <span className="px-2 py-0.5 rounded bg-blue-500/20 text-blue-300 text-[10px] font-mono font-semibold uppercase">New Lead</span>;
      case 'contacted':
        return <span className="px-2 py-0.5 rounded bg-purple-500/20 text-purple-300 text-[10px] font-mono font-semibold uppercase">Contacted</span>;
      case 'interested':
        return <span className="px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-300 text-[10px] font-mono font-semibold uppercase">Interested</span>;
      case 'converted':
        return <span className="px-2 py-0.5 rounded bg-amber-500/20 text-amber-300 text-[10px] font-mono font-semibold uppercase">Converted</span>;
      case 'rejected':
        return <span className="px-2 py-0.5 rounded bg-zinc-700 text-zinc-400 text-[10px] font-mono font-semibold uppercase">Rejected</span>;
    }
  };

  return (
    <div className="flex-1 flex flex-col h-full bg-[#0d0f15] overflow-y-auto">
      {/* Top Header & Filters */}
      <div className="p-6 md:p-8 border-b border-white/10 bg-[#12141c]/50">
        <div className="max-w-6xl mx-auto flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 text-xs font-mono text-amber-400 mb-1">
              <Users className="w-4 h-4" />
              <span>PROSPECT PIPELINE & INTENT QUALIFICATION</span>
            </div>
            <h1 className="text-2xl font-bold tracking-tight text-white">
              Qualified YouTube Prospects
            </h1>
            <p className="text-xs text-zinc-400 mt-1">
              Extracted via YouTube Data API v3 and qualified through Ollama Llama 3.1 structured JSON analysis.
            </p>
          </div>

          <div className="flex items-center gap-3">
            <span className="text-xs text-zinc-400 font-mono">
              Showing <strong className="text-white">{filteredProspects.length}</strong> of {prospects.length} leads
            </span>
          </div>
        </div>

        {/* Filter Controls */}
        <div className="max-w-6xl mx-auto grid grid-cols-1 md:grid-cols-3 gap-3 mt-6">
          {/* Campaign Selector */}
          <div className="p-2.5 rounded-xl bg-white/[0.03] border border-white/10 flex items-center gap-2">
            <Filter className="w-4 h-4 text-zinc-500 ml-1" />
            <span className="text-xs text-zinc-400 font-medium whitespace-nowrap">Campaign:</span>
            <select
              value={selectedCampaignId || ''}
              onChange={(e) => onSelectCampaignId(e.target.value || undefined)}
              className="bg-transparent border-none text-xs text-white focus:outline-none w-full cursor-pointer"
            >
              <option value="" className="bg-zinc-900 text-zinc-200">All Campaigns</option>
              {campaigns.map((c) => (
                <option key={c.id} value={c.id} className="bg-zinc-900 text-zinc-200">{c.name}</option>
              ))}
            </select>
          </div>

          {/* Status Filter */}
          <div className="p-2.5 rounded-xl bg-white/[0.03] border border-white/10 flex items-center gap-2">
            <span className="text-xs text-zinc-400 font-medium ml-1 whitespace-nowrap">Status:</span>
            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              className="bg-transparent border-none text-xs text-white focus:outline-none w-full cursor-pointer"
            >
              <option value="all" className="bg-zinc-900 text-zinc-200">All Stages</option>
              <option value="new" className="bg-zinc-900 text-zinc-200">New Leads</option>
              <option value="contacted" className="bg-zinc-900 text-zinc-200">Contacted</option>
              <option value="interested" className="bg-zinc-900 text-zinc-200">Interested</option>
              <option value="converted" className="bg-zinc-900 text-zinc-200">Converted</option>
              <option value="rejected" className="bg-zinc-900 text-zinc-200">Rejected</option>
            </select>
          </div>

          {/* Min Intent Score Slider */}
          <div className="p-2.5 rounded-xl bg-white/[0.03] border border-white/10 flex items-center justify-between gap-3">
            <div className="flex items-center gap-1.5 text-xs text-zinc-400 whitespace-nowrap">
              <Target className="w-3.5 h-3.5 text-amber-400" />
              <span>Min Score:</span>
              <strong className="text-white font-mono">{minScoreFilter}%</strong>
            </div>
            <input
              type="range"
              min={50}
              max={95}
              step={5}
              value={minScoreFilter}
              onChange={(e) => setMinScoreFilter(Number(e.target.value))}
              className="w-28 accent-amber-500 cursor-pointer"
            />
          </div>
        </div>
      </div>

      {/* Prospect Cards Grid */}
      <div className="p-6 md:p-8 max-w-6xl mx-auto w-full space-y-4">
        {filteredProspects.length === 0 ? (
          <div className="text-center py-16 p-8 rounded-2xl bg-white/[0.02] border border-white/10 space-y-3">
            <AlertCircle className="w-8 h-8 text-zinc-500 mx-auto" />
            <h3 className="text-base font-semibold text-zinc-300">No prospects match filters</h3>
            <p className="text-xs text-zinc-500 max-w-sm mx-auto">
              Lower your minimum intent threshold or trigger discovery in the AI Operator chat.
            </p>
          </div>
        ) : (
          filteredProspects.map((prospect) => (
            <div
              key={prospect.id}
              id={`prospect-item-${prospect.id}`}
              className="p-5 rounded-2xl bg-white/[0.03] hover:bg-white/[0.05] border border-white/10 transition-all space-y-4"
            >
              {/* Header Info */}
              <div className="flex flex-col md:flex-row md:items-center justify-between gap-3">
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 rounded-full bg-gradient-to-tr from-amber-500/20 to-red-500/20 border border-amber-500/30 flex items-center justify-center font-bold text-amber-400 text-sm">
                    {prospect.authorName.charAt(0)}
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <h3 className="text-sm font-bold text-white">{prospect.authorName}</h3>
                      <a
                        href={prospect.authorChannelUrl}
                        target="_blank"
                        rel="noreferrer"
                        className="text-xs text-zinc-400 hover:text-amber-400 font-mono transition-colors flex items-center gap-1"
                      >
                        {prospect.authorHandle}
                        <ExternalLink className="w-3 h-3" />
                      </a>
                    </div>
                    <div className="flex items-center gap-2 mt-0.5 text-[11px] text-zinc-400">
                      <span>Video: <strong className="text-zinc-300">{prospect.videoTitle}</strong></span>
                      <span>•</span>
                      <span>{prospect.commentPublishedAt}</span>
                    </div>
                  </div>
                </div>

                <div className="flex items-center gap-2.5">
                  {getScoreBadge(prospect.intentScore)}
                  {getStatusBadge(prospect.status)}
                </div>
              </div>

              {/* Comment Quote Context */}
              <div className="p-3.5 rounded-xl bg-black/40 border border-white/5 space-y-2">
                <div className="flex items-center gap-1.5 text-[11px] font-mono text-zinc-400">
                  <Youtube className="w-3.5 h-3.5 text-red-500" />
                  <span>Original YouTube Comment:</span>
                </div>
                <p className="text-xs text-zinc-200 italic leading-relaxed pl-2 border-l-2 border-amber-500/40">
                  &quot;{prospect.commentText}&quot;
                </p>
              </div>

              {/* AI Qualification Breakdown */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-xs">
                <div className="p-3 rounded-xl bg-white/[0.02] border border-white/5 space-y-1">
                  <div className="text-[11px] font-semibold text-red-400 uppercase tracking-wider">
                    Detected Pain Point
                  </div>
                  <p className="text-zinc-300 leading-relaxed">{prospect.painPoint}</p>
                </div>

                <div className="p-3 rounded-xl bg-white/[0.02] border border-white/5 space-y-1">
                  <div className="text-[11px] font-semibold text-emerald-400 uppercase tracking-wider">
                    Ollama Qualification Reasoning
                  </div>
                  <p className="text-zinc-300 leading-relaxed">{prospect.qualificationReason}</p>
                </div>
              </div>

              {/* Outreach Reply & Actions Bar */}
              <div className="pt-3 border-t border-white/5 flex flex-col md:flex-row md:items-center justify-between gap-3">
                <div className="flex items-center gap-2">
                  <span className="text-xs text-zinc-400">Pipeline Stage:</span>
                  <select
                    value={prospect.status}
                    onChange={(e) => onUpdateStatus(prospect.id, e.target.value as ProspectStatus)}
                    className="bg-white/5 border border-white/10 rounded-lg px-2.5 py-1 text-xs text-white focus:outline-none focus:border-amber-500"
                  >
                    <option value="new" className="bg-zinc-900">New</option>
                    <option value="contacted" className="bg-zinc-900">Contacted</option>
                    <option value="interested" className="bg-zinc-900">Interested</option>
                    <option value="converted" className="bg-zinc-900">Converted</option>
                    <option value="rejected" className="bg-zinc-900">Rejected</option>
                  </select>
                </div>

                <div className="flex items-center gap-2">
                  {prospect.replyStatus === 'approved' ? (
                    <span className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-emerald-500/10 text-emerald-400 text-xs font-semibold">
                      <CheckCircle2 className="w-3.5 h-3.5" />
                      <span>Approved via OAuth</span>
                    </span>
                  ) : prospect.generatedReply ? (
                    <button
                      id={`btn-review-${prospect.id}`}
                      onClick={() => onOpenReview(prospect)}
                      className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg bg-amber-500 hover:bg-amber-400 text-zinc-950 font-bold text-xs transition-colors"
                    >
                      <SendHorizontal className="w-3.5 h-3.5" />
                      <span>Review & Approve Reply (M2)</span>
                    </button>
                  ) : (
                    <button
                      id={`btn-draft-reply-${prospect.id}`}
                      onClick={() => onDraftReply(prospect)}
                      className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-white/10 hover:bg-white/20 text-white font-semibold text-xs transition-colors"
                    >
                      <Sparkles className="w-3.5 h-3.5 text-amber-400" />
                      <span>Draft AI Reply</span>
                    </button>
                  )}
                </div>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};
