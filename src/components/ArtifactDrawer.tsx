import React from 'react';
import { X, ExternalLink, Layers, SendHorizontal, CheckCircle2 } from 'lucide-react';
import { ChatArtifact } from '../types';

interface ArtifactDrawerProps {
  artifact: ChatArtifact | null;
  onClose: () => void;
  onNavigateTab: (tab: 'campaigns' | 'prospects' | 'review' | 'hangfire') => void;
}

export const ArtifactDrawer: React.FC<ArtifactDrawerProps> = ({
  artifact,
  onClose,
  onNavigateTab
}) => {
  if (!artifact) return null;

  return (
    <div className="fixed inset-y-0 right-0 w-full max-w-xl bg-[#12141c] border-l border-white/10 shadow-2xl z-50 flex flex-col animate-in slide-in-from-right duration-200">
      {/* Drawer Header */}
      <div className="p-4 border-b border-white/10 flex items-center justify-between bg-black/20">
        <div className="flex items-center gap-2.5">
          <div className="w-8 h-8 rounded-lg bg-amber-500/20 text-amber-400 flex items-center justify-center">
            <Layers className="w-4 h-4" />
          </div>
          <div>
            <h3 className="text-xs font-bold text-white truncate max-w-sm">
              {artifact.title}
            </h3>
            <span className="text-[10px] font-mono text-zinc-500 uppercase">
              TrafficHunt Generated Artifact • Type: {artifact.type}
            </span>
          </div>
        </div>

        <button
          onClick={onClose}
          className="p-1.5 rounded-lg hover:bg-white/10 text-zinc-400 hover:text-white transition-colors"
        >
          <X className="w-4 h-4" />
        </button>
      </div>

      {/* Drawer Content */}
      <div className="flex-1 overflow-y-auto p-6 space-y-4 text-xs">
        {artifact.type === 'campaign' && artifact.data?.campaign && (
          <div className="space-y-4">
            <div className="p-4 rounded-xl bg-white/[0.03] border border-white/10 space-y-2">
              <span className="text-[10px] font-mono text-amber-400 uppercase">Generated Campaign</span>
              <h2 className="text-base font-bold text-white">{artifact.data.campaign.name}</h2>
              <p className="text-zinc-300 leading-relaxed">{artifact.data.campaign.description}</p>
            </div>

            <div className="p-3.5 rounded-xl bg-black/40 border border-white/5 space-y-1">
              <span className="text-[10px] font-mono text-zinc-400 uppercase">Target Audience</span>
              <p className="text-zinc-200">{artifact.data.campaign.targetAudience}</p>
            </div>

            <div className="p-3.5 rounded-xl bg-black/40 border border-white/5 space-y-1">
              <span className="text-[10px] font-mono text-zinc-400 uppercase">Problem Solved</span>
              <p className="text-zinc-200">{artifact.data.campaign.problemStatement}</p>
            </div>

            <div className="space-y-1.5">
              <span className="text-[10px] font-mono text-zinc-400 uppercase">Search Keywords:</span>
              <div className="flex flex-wrap gap-1.5">
                {artifact.data.campaign.keywords?.map((kw: string, i: number) => (
                  <span key={i} className="px-2 py-1 rounded bg-white/5 text-zinc-300 font-mono text-[11px]">
                    {kw}
                  </span>
                ))}
              </div>
            </div>

            <button
              onClick={() => {
                onClose();
                onNavigateTab('campaigns');
              }}
              className="w-full mt-4 flex items-center justify-center gap-2 py-2.5 px-4 rounded-xl bg-amber-500 hover:bg-amber-400 text-zinc-950 font-bold transition-colors"
            >
              <span>View in Campaigns Hub</span>
              <ExternalLink className="w-3.5 h-3.5" />
            </button>
          </div>
        )}

        {artifact.type === 'reply_studio' && artifact.data && (
          <div className="space-y-4">
            <div className="p-4 rounded-xl bg-white/[0.03] border border-white/10 space-y-2">
              <span className="text-[10px] font-mono text-amber-400 uppercase">Outreach Reply Draft</span>
              <h2 className="text-sm font-bold text-white">{artifact.data.authorName} ({artifact.data.authorHandle})</h2>
              <p className="text-zinc-400 italic text-[11px]">&quot;{artifact.data.commentText}&quot;</p>
            </div>

            <div className="p-3.5 rounded-xl bg-black/50 border border-white/10 space-y-2">
              <span className="text-[10px] font-mono text-emerald-400 uppercase">AI Drafted Comment</span>
              <p className="text-zinc-200 font-mono text-xs leading-relaxed">
                {artifact.data.generatedReply || artifact.data.approvedReply}
              </p>
            </div>

            <button
              onClick={() => {
                onClose();
                onNavigateTab('review');
              }}
              className="w-full mt-4 flex items-center justify-center gap-2 py-2.5 px-4 rounded-xl bg-amber-500 hover:bg-amber-400 text-zinc-950 font-bold transition-colors"
            >
              <SendHorizontal className="w-3.5 h-3.5" />
              <span>Open in Review Studio (Human Approval)</span>
            </button>
          </div>
        )}
      </div>
    </div>
  );
};
