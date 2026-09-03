import React, { useState } from 'react';
import { 
  Layers, 
  Search, 
  Plus, 
  Play, 
  Users, 
  Video, 
  MessageSquare, 
  Target, 
  ChevronRight,
  TrendingUp,
  X
} from 'lucide-react';
import { Campaign } from '../types';

interface CampaignsViewProps {
  campaigns: Campaign[];
  onCreateCampaign: (newCamp: Partial<Campaign>) => void;
  onSelectCampaign: (campaign: Campaign) => void;
  onRunDiscovery: (campaignId: string) => void;
  onGoToProspects: (campaignId?: string) => void;
}

export const CampaignsView: React.FC<CampaignsViewProps> = ({
  campaigns,
  onCreateCampaign,
  onSelectCampaign,
  onRunDiscovery,
  onGoToProspects
}) => {
  const [showModal, setShowModal] = useState(false);
  const [productName, setProductName] = useState('');
  const [description, setDescription] = useState('');
  const [targetAudience, setTargetAudience] = useState('');
  const [problemStatement, setProblemStatement] = useState('');
  const [keywords, setKeywords] = useState('');

  const handleCreate = (e: React.FormEvent) => {
    e.preventDefault();
    if (!productName.trim()) return;

    onCreateCampaign({
      name: `${productName} YouTube Outreach`,
      productName,
      description,
      targetAudience: targetAudience || 'YouTube creators & niche operators',
      problemStatement: problemStatement || 'Manual prospecting inefficiencies',
      keywords: keywords ? keywords.split(',').map(k => k.trim()).filter(Boolean) : ['youtube creator tools', 'client acquisition tips'],
      aiInstructions: 'Qualify comments expressing active frustration with current tools or seeking alternatives.',
      status: 'active'
    });

    setShowModal(false);
    setProductName('');
    setDescription('');
    setTargetAudience('');
    setProblemStatement('');
    setKeywords('');
  };

  const totalVideos = campaigns.reduce((acc, c) => acc + c.stats.videosScanned, 0);
  const totalComments = campaigns.reduce((acc, c) => acc + c.stats.commentsAnalyzed, 0);
  const totalProspects = campaigns.reduce((acc, c) => acc + c.stats.prospectsFound, 0);
  const totalQualified = campaigns.reduce((acc, c) => acc + c.stats.qualifiedCount, 0);

  return (
    <div className="flex-1 flex flex-col h-full bg-[#0d0f15] overflow-y-auto">
      {/* Header */}
      <div className="p-6 md:p-8 border-b border-white/10 bg-[#12141c]/50">
        <div className="max-w-6xl mx-auto flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 text-xs font-mono text-amber-400 mb-1">
              <Layers className="w-4 h-4" />
              <span>CAMPAIGN STRATEGY & DISCOVERY</span>
            </div>
            <h1 className="text-2xl font-bold tracking-tight text-white">
              Targeted Outreach Campaigns
            </h1>
            <p className="text-xs text-zinc-400 mt-1">
              &quot;TrafficHunt doesn&apos;t know what app we&apos;re promoting. The Campaign tells it what we&apos;re promoting, who we&apos;re looking for, and what problem we&apos;re solving.&quot;
            </p>
          </div>

          <button
            id="btn-create-campaign-modal"
            onClick={() => setShowModal(true)}
            className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-amber-500 hover:bg-amber-400 text-zinc-950 font-bold text-xs transition-all shadow-md active:scale-95"
          >
            <Plus className="w-4 h-4" />
            <span>New Campaign</span>
          </button>
        </div>

        {/* Global Stats Grid */}
        <div className="max-w-6xl mx-auto grid grid-cols-2 md:grid-cols-4 gap-3.5 mt-6">
          <div className="p-3.5 rounded-xl bg-white/[0.03] border border-white/10">
            <div className="flex items-center justify-between text-zinc-400 text-xs">
              <span>Videos Scanned</span>
              <Video className="w-4 h-4 text-zinc-500" />
            </div>
            <div className="text-xl font-bold text-white mt-1.5 font-mono">{totalVideos}</div>
            <div className="text-[10px] text-zinc-500 mt-0.5">via YoutubeExplode</div>
          </div>

          <div className="p-3.5 rounded-xl bg-white/[0.03] border border-white/10">
            <div className="flex items-center justify-between text-zinc-400 text-xs">
              <span>Comments Analyzed</span>
              <MessageSquare className="w-4 h-4 text-zinc-500" />
            </div>
            <div className="text-xl font-bold text-white mt-1.5 font-mono">{totalComments}</div>
            <div className="text-[10px] text-zinc-500 mt-0.5">YouTube Data API v3</div>
          </div>

          <div className="p-3.5 rounded-xl bg-white/[0.03] border border-white/10">
            <div className="flex items-center justify-between text-zinc-400 text-xs">
              <span>Prospects Found</span>
              <Users className="w-4 h-4 text-zinc-500" />
            </div>
            <div className="text-xl font-bold text-white mt-1.5 font-mono">{totalProspects}</div>
            <div className="text-[10px] text-zinc-500 mt-0.5">Qualified leads</div>
          </div>

          <div className="p-3.5 rounded-xl bg-white/[0.03] border border-white/10">
            <div className="flex items-center justify-between text-zinc-400 text-xs">
              <span>Intent Qualification</span>
              <Target className="w-4 h-4 text-emerald-400" />
            </div>
            <div className="text-xl font-bold text-emerald-400 mt-1.5 font-mono">{totalQualified}</div>
            <div className="text-[10px] text-zinc-500 mt-0.5">Score 80+ by Ollama</div>
          </div>
        </div>
      </div>

      {/* Campaigns List */}
      <div className="p-6 md:p-8 max-w-6xl mx-auto w-full space-y-6">
        <div className="flex items-center justify-between">
          <h2 className="text-sm font-semibold text-zinc-300 uppercase tracking-wider">
            Active Campaigns ({campaigns.length})
          </h2>
          <span className="text-xs text-zinc-500 font-mono">Ranked by Lead Density</span>
        </div>

        <div className="grid grid-cols-1 gap-4">
          {campaigns.map((camp) => (
            <div
              key={camp.id}
              id={`campaign-card-${camp.id}`}
              className="p-5 rounded-2xl bg-white/[0.03] hover:bg-white/[0.05] border border-white/10 transition-all space-y-4"
            >
              <div className="flex flex-col md:flex-row md:items-center justify-between gap-3">
                <div>
                  <div className="flex items-center gap-2">
                    <h3 className="text-base font-bold text-white">{camp.name}</h3>
                    <span className="px-2 py-0.5 rounded-full bg-emerald-500/20 text-emerald-300 text-[10px] font-mono uppercase font-semibold">
                      {camp.status}
                    </span>
                  </div>
                  <p className="text-xs text-zinc-400 mt-1 max-w-2xl">{camp.description}</p>
                </div>

                <div className="flex items-center gap-2">
                  <button
                    id={`btn-run-discovery-${camp.id}`}
                    onClick={() => onRunDiscovery(camp.id)}
                    className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-white/10 hover:bg-white/20 text-xs font-semibold text-zinc-200 transition-colors"
                  >
                    <Play className="w-3.5 h-3.5 text-amber-400" />
                    <span>Run Discovery Job</span>
                  </button>
                  <button
                    id={`btn-view-prospects-${camp.id}`}
                    onClick={() => onGoToProspects(camp.id)}
                    className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-amber-500 hover:bg-amber-400 text-xs font-bold text-zinc-950 transition-colors"
                  >
                    <span>View Prospects</span>
                    <ChevronRight className="w-3.5 h-3.5" />
                  </button>
                </div>
              </div>

              {/* Target & Problem */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3 pt-2 text-xs">
                <div className="p-3 rounded-xl bg-black/30 border border-white/5 space-y-1">
                  <div className="text-[11px] font-semibold text-amber-400 uppercase tracking-wider">
                    Target Audience
                  </div>
                  <p className="text-zinc-300 leading-relaxed">{camp.targetAudience}</p>
                </div>

                <div className="p-3 rounded-xl bg-black/30 border border-white/5 space-y-1">
                  <div className="text-[11px] font-semibold text-red-400 uppercase tracking-wider">
                    Problem Solved
                  </div>
                  <p className="text-zinc-300 leading-relaxed">{camp.problemStatement}</p>
                </div>
              </div>

              {/* Keywords */}
              <div>
                <div className="text-[11px] font-semibold text-zinc-500 uppercase tracking-wider mb-2">
                  Discovery Keywords (YoutubeExplode Clusters)
                </div>
                <div className="flex flex-wrap gap-1.5">
                  {camp.keywords.map((kw, idx) => (
                    <span
                      key={idx}
                      className="px-2.5 py-1 rounded-md bg-white/5 border border-white/10 text-zinc-300 text-xs font-mono"
                    >
                      {kw}
                    </span>
                  ))}
                </div>
              </div>

              {/* Funnel Metrics Bar */}
              <div className="pt-3 border-t border-white/5 flex items-center justify-between text-xs text-zinc-400 font-mono">
                <div className="flex items-center gap-4">
                  <span><strong className="text-white">{camp.stats.videosScanned}</strong> Videos Scanned</span>
                  <span><strong className="text-white">{camp.stats.commentsAnalyzed}</strong> Comments Analyzed</span>
                  <span><strong className="text-emerald-400">{camp.stats.qualifiedCount}</strong> High-Intent Leads</span>
                </div>
                <div className="text-amber-400 font-semibold">
                  Avg Intent: {camp.stats.averageIntentScore}%
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* New Campaign Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black/70 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="w-full max-w-lg bg-[#12141c] border border-white/15 rounded-2xl p-6 shadow-2xl space-y-4">
            <div className="flex items-center justify-between border-b border-white/10 pb-3">
              <h2 className="text-base font-bold text-white flex items-center gap-2">
                <Layers className="w-4 h-4 text-amber-400" />
                <span>Create New Campaign</span>
              </h2>
              <button
                onClick={() => setShowModal(false)}
                className="p-1 rounded-lg hover:bg-white/10 text-zinc-400 hover:text-white"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            <form onSubmit={handleCreate} className="space-y-3.5 text-xs">
              <div>
                <label className="block text-zinc-300 font-semibold mb-1">Product / Service Name *</label>
                <input
                  type="text"
                  required
                  value={productName}
                  onChange={(e) => setProductName(e.target.value)}
                  placeholder="e.g. ColdMail Gorilla, ClipFlow AI, SEO Ranker"
                  className="w-full px-3 py-2 rounded-lg bg-white/5 border border-white/10 text-white placeholder-zinc-500 focus:outline-none focus:border-amber-500"
                />
              </div>

              <div>
                <label className="block text-zinc-300 font-semibold mb-1">Description / Core Offer</label>
                <textarea
                  rows={2}
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="What does your tool do and how does it benefit users?"
                  className="w-full px-3 py-2 rounded-lg bg-white/5 border border-white/10 text-white placeholder-zinc-500 focus:outline-none focus:border-amber-500 resize-none"
                />
              </div>

              <div>
                <label className="block text-zinc-300 font-semibold mb-1">Target Audience Profile</label>
                <input
                  type="text"
                  value={targetAudience}
                  onChange={(e) => setTargetAudience(e.target.value)}
                  placeholder="e.g. Freelance video editors, YouTube creators with 10k+ subs"
                  className="w-full px-3 py-2 rounded-lg bg-white/5 border border-white/10 text-white placeholder-zinc-500 focus:outline-none focus:border-amber-500"
                />
              </div>

              <div>
                <label className="block text-zinc-300 font-semibold mb-1">Problem Statement / Pain Point</label>
                <input
                  type="text"
                  value={problemStatement}
                  onChange={(e) => setProblemStatement(e.target.value)}
                  placeholder="e.g. Sending cold pitches that get ignored, manual prospecting takes hours"
                  className="w-full px-3 py-2 rounded-lg bg-white/5 border border-white/10 text-white placeholder-zinc-500 focus:outline-none focus:border-amber-500"
                />
              </div>

              <div>
                <label className="block text-zinc-300 font-semibold mb-1">Discovery Keywords (Comma-separated)</label>
                <input
                  type="text"
                  value={keywords}
                  onChange={(e) => setKeywords(e.target.value)}
                  placeholder="e.g. how to get editing clients, cold outreach youtube, video editor portfolio"
                  className="w-full px-3 py-2 rounded-lg bg-white/5 border border-white/10 text-white placeholder-zinc-500 focus:outline-none focus:border-amber-500"
                />
              </div>

              <div className="pt-3 border-t border-white/10 flex items-center justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setShowModal(false)}
                  className="px-3 py-2 rounded-lg bg-white/5 hover:bg-white/10 text-zinc-300 font-medium"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 rounded-lg bg-amber-500 hover:bg-amber-400 text-zinc-950 font-bold"
                >
                  Save Campaign
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
