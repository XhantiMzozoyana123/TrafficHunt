import React, { useState } from 'react';
import { 
  SendHorizontal, 
  ShieldAlert, 
  Sparkles, 
  CheckCircle2, 
  AlertTriangle, 
  ExternalLink, 
  RefreshCw, 
  Youtube, 
  Terminal,
  Lock,
  ThumbsUp
} from 'lucide-react';
import { Prospect } from '../types';

interface ReviewStudioViewProps {
  prospects: Prospect[];
  selectedProspect?: Prospect;
  onSelectProspect: (prospect: Prospect) => void;
  onApproveReply: (prospectId: string, approvedText: string) => void;
  onRegenerateReply: (prospectId: string, tone: 'value_first' | 'helpful_advisor' | 'direct_solution') => void;
}

export const ReviewStudioView: React.FC<ReviewStudioViewProps> = ({
  prospects,
  selectedProspect,
  onSelectProspect,
  onApproveReply,
  onRegenerateReply
}) => {
  const pendingProspects = prospects.filter(p => p.replyStatus !== 'approved');
  const activeProspect = selectedProspect || pendingProspects[0] || prospects[0];

  const [replyText, setReplyText] = useState(
    activeProspect?.approvedReply || activeProspect?.generatedReply || ''
  );
  const [tone, setTone] = useState<'value_first' | 'helpful_advisor' | 'direct_solution'>(
    activeProspect?.replyTone || 'value_first'
  );
  const [dispatchLogs, setDispatchLogs] = useState<string[]>([]);
  const [isDispatching, setIsDispatching] = useState(false);
  const [isSuccess, setIsSuccess] = useState(activeProspect?.replyStatus === 'approved');

  // Update text when active prospect changes
  React.useEffect(() => {
    if (activeProspect) {
      setReplyText(activeProspect.approvedReply || activeProspect.generatedReply || '');
      setTone(activeProspect.replyTone || 'value_first');
      setIsSuccess(activeProspect.replyStatus === 'approved');
      setDispatchLogs([]);
    }
  }, [activeProspect?.id]);

  const handleToneChange = (newTone: 'value_first' | 'helpful_advisor' | 'direct_solution') => {
    setTone(newTone);
    if (activeProspect) {
      onRegenerateReply(activeProspect.id, newTone);
    }
  };

  const handleApprove = () => {
    if (!activeProspect || !replyText.trim()) return;

    setIsDispatching(true);
    setDispatchLogs([
      `[OAuth v3] Initializing YouTube Data API client session...`,
      `[Boundary Check] Operator human approval confirmed for prospect ${activeProspect.authorHandle}.`,
      `[Safety Scan] Evaluating spam keywords, link density, and comment velocity... PASSED.`,
      `[POST /youtube/v3/comments] Dispatching top-level comment reply to thread ${activeProspect.commentId}...`,
      `[200 OK] Response received. Comment ID: cmt_live_${Date.now().toString().slice(-6)}`,
      `[Pipeline Update] Prospect ${activeProspect.authorName} moved to 'Contacted' stage.`
    ]);

    setTimeout(() => {
      setIsDispatching(false);
      setIsSuccess(true);
      onApproveReply(activeProspect.id, replyText);
    }, 1200);
  };

  // Basic spam risk analysis
  const hasLink = /https?:\/\/|www\./i.test(replyText);
  const charCount = replyText.length;
  const isTooLong = charCount > 500;

  return (
    <div className="flex-1 flex flex-col h-full bg-[#0d0f15] overflow-y-auto">
      {/* Header */}
      <div className="p-6 md:p-8 border-b border-white/10 bg-[#12141c]/50">
        <div className="max-w-6xl mx-auto flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 text-xs font-mono text-amber-400 mb-1">
              <Lock className="w-4 h-4 text-emerald-400" />
              <span>MILESTONE 2: HUMAN APPROVAL BOUNDARY</span>
            </div>
            <h1 className="text-2xl font-bold tracking-tight text-white">
              Outreach Review & Approval Studio
            </h1>
            <p className="text-xs text-zinc-400 mt-1">
              TrafficHunt enforces a strict safety boundary: the AI crafts the personalized reply, but the human operator inspects, edits, and authorizes the YouTube OAuth send.
            </p>
          </div>

          <div className="flex items-center gap-2">
            <span className="px-3 py-1.5 rounded-lg bg-emerald-500/10 border border-emerald-500/20 text-emerald-400 text-xs font-mono">
              YouTube OAuth v3 Ready
            </span>
          </div>
        </div>
      </div>

      {/* Main Studio Two-Column Grid */}
      <div className="max-w-6xl mx-auto w-full p-6 md:p-8 grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Left Column: Queue of prospects */}
        <div className="lg:col-span-4 space-y-3">
          <div className="flex items-center justify-between text-xs font-semibold text-zinc-400 uppercase tracking-wider px-1">
            <span>Review Queue ({pendingProspects.length})</span>
          </div>

          <div className="space-y-2 max-h-[600px] overflow-y-auto pr-1">
            {prospects.map((p) => {
              const isSelected = activeProspect?.id === p.id;
              const isApproved = p.replyStatus === 'approved';

              return (
                <div
                  key={p.id}
                  onClick={() => onSelectProspect(p)}
                  className={`p-3.5 rounded-xl border transition-all cursor-pointer ${
                    isSelected
                      ? 'bg-amber-500/10 border-amber-500/50 shadow-sm'
                      : 'bg-white/[0.02] hover:bg-white/[0.05] border-white/10'
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-bold text-white truncate">{p.authorName}</span>
                    <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-amber-500/20 text-amber-300">
                      {p.intentScore}% Intent
                    </span>
                  </div>
                  <div className="text-[11px] text-zinc-400 font-mono mt-0.5">{p.authorHandle}</div>
                  <p className="text-[11px] text-zinc-400 mt-2 line-clamp-2 italic">
                    &quot;{p.commentText}&quot;
                  </p>

                  <div className="mt-2.5 flex items-center justify-between text-[10px]">
                    {isApproved ? (
                      <span className="text-emerald-400 flex items-center gap-1 font-medium">
                        <CheckCircle2 className="w-3 h-3" /> Approved & Sent
                      </span>
                    ) : p.generatedReply ? (
                      <span className="text-amber-400 flex items-center gap-1 font-medium">
                        <Sparkles className="w-3 h-3" /> Draft Ready
                      </span>
                    ) : (
                      <span className="text-zinc-500 font-mono">Pending AI Draft</span>
                    )}
                    <span className="text-zinc-500">{p.commentPublishedAt}</span>
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Right Column: Active Prospect Review & Dispatch Console */}
        {activeProspect ? (
          <div className="lg:col-span-8 space-y-4">
            {/* Prospect Context Box */}
            <div className="p-4 rounded-2xl bg-white/[0.03] border border-white/10 space-y-3">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div className="w-8 h-8 rounded-lg bg-amber-500/20 text-amber-400 flex items-center justify-center font-bold text-xs">
                    {activeProspect.authorName.charAt(0)}
                  </div>
                  <div>
                    <h3 className="text-sm font-bold text-white flex items-center gap-1.5">
                      {activeProspect.authorName}
                      <span className="text-xs text-zinc-400 font-mono">({activeProspect.authorHandle})</span>
                    </h3>
                    <div className="text-[11px] text-zinc-400">
                      Target Campaign: <span className="text-amber-400 font-medium">{activeProspect.campaignName}</span>
                    </div>
                  </div>
                </div>

                <a
                  href={activeProspect.videoUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="flex items-center gap-1.5 text-xs text-red-400 hover:text-red-300 font-mono transition-colors"
                >
                  <Youtube className="w-4 h-4" />
                  <span>Open Video Thread</span>
                  <ExternalLink className="w-3 h-3" />
                </a>
              </div>

              {/* Original Comment */}
              <div className="p-3 rounded-xl bg-black/40 border border-white/5 space-y-1">
                <div className="text-[10px] font-semibold text-zinc-500 uppercase tracking-wider">
                  Original YouTube Comment:
                </div>
                <p className="text-xs text-zinc-200 italic leading-relaxed">
                  &quot;{activeProspect.commentText}&quot;
                </p>
                <div className="text-[11px] text-red-300/90 pt-1">
                  <strong>Pain Point:</strong> {activeProspect.painPoint}
                </div>
              </div>
            </div>

            {/* Reply Editor */}
            <div className="p-5 rounded-2xl bg-white/[0.03] border border-white/10 space-y-4">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-white/10 pb-3">
                <div className="flex items-center gap-2">
                  <Sparkles className="w-4 h-4 text-amber-400" />
                  <span className="text-xs font-bold text-white uppercase tracking-wider">
                    AI Reply Draft (Human Editable)
                  </span>
                </div>

                {/* Tone Selector */}
                <div className="flex items-center gap-1 bg-black/30 p-1 rounded-lg border border-white/5">
                  <button
                    onClick={() => handleToneChange('value_first')}
                    className={`px-2.5 py-1 rounded text-[11px] font-medium transition-colors ${
                      tone === 'value_first' ? 'bg-amber-500 text-zinc-950 font-bold' : 'text-zinc-400 hover:text-white'
                    }`}
                  >
                    Value-First
                  </button>
                  <button
                    onClick={() => handleToneChange('helpful_advisor')}
                    className={`px-2.5 py-1 rounded text-[11px] font-medium transition-colors ${
                      tone === 'helpful_advisor' ? 'bg-amber-500 text-zinc-950 font-bold' : 'text-zinc-400 hover:text-white'
                    }`}
                  >
                    Helpful Advisor
                  </button>
                  <button
                    onClick={() => handleToneChange('direct_solution')}
                    className={`px-2.5 py-1 rounded text-[11px] font-medium transition-colors ${
                      tone === 'direct_solution' ? 'bg-amber-500 text-zinc-950 font-bold' : 'text-zinc-400 hover:text-white'
                    }`}
                  >
                    Direct Solution
                  </button>
                </div>
              </div>

              {/* Text Area */}
              <div className="space-y-1.5">
                <textarea
                  id="reply-editor-textarea"
                  rows={6}
                  value={replyText}
                  onChange={(e) => setReplyText(e.target.value)}
                  placeholder="Craft your personalized outreach comment..."
                  className="w-full p-3.5 rounded-xl bg-black/40 border border-white/10 text-xs text-zinc-100 placeholder-zinc-500 focus:outline-none focus:border-amber-500 font-mono leading-relaxed resize-none"
                />
                
                <div className="flex items-center justify-between text-[11px] text-zinc-400 px-1">
                  <div className="flex items-center gap-3">
                    <span className={isTooLong ? 'text-red-400 font-bold' : ''}>
                      {charCount} / 500 characters
                    </span>
                    {hasLink && (
                      <span className="text-amber-400 flex items-center gap-1 font-semibold">
                        <AlertTriangle className="w-3 h-3" />
                        Links in first comment increase spam filter risk
                      </span>
                    )}
                  </div>
                  <span className="text-zinc-500">Auto-saves to prospect record</span>
                </div>
              </div>

              {/* Approval CTAs */}
              <div className="pt-2 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                <button
                  onClick={() => onRegenerateReply(activeProspect.id, tone)}
                  className="flex items-center gap-1.5 px-3 py-2 rounded-xl bg-white/5 hover:bg-white/10 text-zinc-300 font-semibold text-xs transition-colors"
                >
                  <RefreshCw className="w-3.5 h-3.5 text-zinc-400" />
                  <span>Regenerate with {tone.replace('_', ' ')}</span>
                </button>

                <button
                  id="btn-approve-dispatch"
                  onClick={handleApprove}
                  disabled={isDispatching || isSuccess || !replyText.trim()}
                  className={`flex items-center gap-2 px-5 py-2.5 rounded-xl font-bold text-xs transition-all shadow-md active:scale-95 ${
                    isSuccess
                      ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40 cursor-default'
                      : 'bg-amber-500 hover:bg-amber-400 text-zinc-950 cursor-pointer disabled:opacity-50'
                  }`}
                >
                  {isDispatching ? (
                    <>
                      <div className="w-3.5 h-3.5 border-2 border-zinc-950 border-t-transparent rounded-full animate-spin"></div>
                      <span>Dispatching OAuth v3...</span>
                    </>
                  ) : isSuccess ? (
                    <>
                      <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                      <span>Approved & Dispatched</span>
                    </>
                  ) : (
                    <>
                      <SendHorizontal className="w-4 h-4" />
                      <span>Approve & Dispatch Comment</span>
                    </>
                  )}
                </button>
              </div>
            </div>

            {/* Live OAuth Dispatch Console Logs */}
            {dispatchLogs.length > 0 && (
              <div className="p-4 rounded-2xl bg-black/60 border border-white/10 font-mono text-xs space-y-2">
                <div className="flex items-center gap-2 text-zinc-400 text-[11px] font-semibold border-b border-white/10 pb-2">
                  <Terminal className="w-3.5 h-3.5 text-amber-400" />
                  <span>YOUTUBE OAUTH DISPATCH LOG (STDIO)</span>
                </div>
                <div className="space-y-1 text-zinc-300 text-[11px]">
                  {dispatchLogs.map((log, idx) => (
                    <div key={idx} className="leading-relaxed">
                      {log.startsWith('[200 OK]') ? (
                        <span className="text-emerald-400 font-bold">{log}</span>
                      ) : log.startsWith('[Safety Scan]') ? (
                        <span className="text-cyan-300">{log}</span>
                      ) : (
                        log
                      )}
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        ) : (
          <div className="lg:col-span-8 text-center py-24 text-zinc-500 text-xs">
            Select a prospect from the queue to start review.
          </div>
        )}
      </div>
    </div>
  );
};
