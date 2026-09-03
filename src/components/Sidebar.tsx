import React from 'react';
import { 
  Bot, 
  Layers, 
  Users, 
  SendHorizontal, 
  Activity, 
  Terminal, 
  Plus, 
  Flame, 
  ExternalLink,
  ShieldCheck,
  Cpu
} from 'lucide-react';

interface SidebarProps {
  currentTab: 'chat' | 'campaigns' | 'prospects' | 'review' | 'hangfire' | 'mcp';
  setCurrentTab: (tab: 'chat' | 'campaigns' | 'prospects' | 'review' | 'hangfire' | 'mcp') => void;
  onNewChat: () => void;
  campaignCount: number;
  prospectCount: number;
  pendingReviewCount: number;
}

export const Sidebar: React.FC<SidebarProps> = ({
  currentTab,
  setCurrentTab,
  onNewChat,
  campaignCount,
  prospectCount,
  pendingReviewCount
}) => {
  return (
    <aside className="w-64 bg-[#12141c] border-r border-white/10 flex flex-col h-full flex-shrink-0 select-none">
      {/* App Header */}
      <div className="p-4 border-b border-white/10">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-amber-500 to-red-600 flex items-center justify-center text-white font-bold shadow-lg shadow-amber-500/20">
              <Flame className="w-4 h-4 text-white" />
            </div>
            <div>
              <div className="font-bold text-sm tracking-tight text-white flex items-center gap-1.5">
                TrafficHunt
                <span className="text-[10px] px-1.5 py-0.5 rounded bg-amber-500/10 text-amber-400 font-mono border border-amber-500/20 font-medium">v1.0</span>
              </div>
              <p className="text-[11px] text-zinc-400">AI Customer Acquisition</p>
            </div>
          </div>
        </div>

        {/* New Hunt Action */}
        <button
          id="btn-new-hunt"
          onClick={onNewChat}
          className="mt-3.5 w-full flex items-center justify-center gap-2 py-2 px-3 rounded-lg bg-amber-500 hover:bg-amber-400 text-zinc-950 font-semibold text-xs transition-all shadow-sm active:scale-[0.98]"
        >
          <Plus className="w-4 h-4" />
          <span>New Hunt Session</span>
        </button>
      </div>

      {/* Navigation Links */}
      <div className="flex-1 overflow-y-auto px-3 py-4 space-y-1">
        <div className="text-[10px] font-semibold tracking-wider text-zinc-500 uppercase px-2 mb-2">
          Workspace
        </div>

        <button
          id="nav-chat"
          onClick={() => setCurrentTab('chat')}
          className={`w-full flex items-center justify-between px-2.5 py-2 rounded-lg text-xs font-medium transition-colors ${
            currentTab === 'chat'
              ? 'bg-white/10 text-white shadow-inner font-semibold'
              : 'text-zinc-400 hover:text-zinc-200 hover:bg-white/5'
          }`}
        >
          <div className="flex items-center gap-2.5">
            <Bot className={`w-4 h-4 ${currentTab === 'chat' ? 'text-amber-400' : 'text-zinc-400'}`} />
            <span>AI Operator (Chat)</span>
          </div>
          <span className="text-[10px] px-1.5 py-0.5 rounded bg-amber-500/20 text-amber-300 font-mono">Claude/Llama</span>
        </button>

        <button
          id="nav-campaigns"
          onClick={() => setCurrentTab('campaigns')}
          className={`w-full flex items-center justify-between px-2.5 py-2 rounded-lg text-xs font-medium transition-colors ${
            currentTab === 'campaigns'
              ? 'bg-white/10 text-white shadow-inner font-semibold'
              : 'text-zinc-400 hover:text-zinc-200 hover:bg-white/5'
          }`}
        >
          <div className="flex items-center gap-2.5">
            <Layers className={`w-4 h-4 ${currentTab === 'campaigns' ? 'text-amber-400' : 'text-zinc-400'}`} />
            <span>Campaigns</span>
          </div>
          <span className="text-[11px] text-zinc-400 bg-white/5 px-2 py-0.5 rounded-full">{campaignCount}</span>
        </button>

        <button
          id="nav-prospects"
          onClick={() => setCurrentTab('prospects')}
          className={`w-full flex items-center justify-between px-2.5 py-2 rounded-lg text-xs font-medium transition-colors ${
            currentTab === 'prospects'
              ? 'bg-white/10 text-white shadow-inner font-semibold'
              : 'text-zinc-400 hover:text-zinc-200 hover:bg-white/5'
          }`}
        >
          <div className="flex items-center gap-2.5">
            <Users className={`w-4 h-4 ${currentTab === 'prospects' ? 'text-amber-400' : 'text-zinc-400'}`} />
            <span>Prospect Pipeline</span>
          </div>
          <span className="text-[11px] text-zinc-400 bg-white/5 px-2 py-0.5 rounded-full">{prospectCount}</span>
        </button>

        <button
          id="nav-review"
          onClick={() => setCurrentTab('review')}
          className={`w-full flex items-center justify-between px-2.5 py-2 rounded-lg text-xs font-medium transition-colors ${
            currentTab === 'review'
              ? 'bg-white/10 text-white shadow-inner font-semibold'
              : 'text-zinc-400 hover:text-zinc-200 hover:bg-white/5'
          }`}
        >
          <div className="flex items-center gap-2.5">
            <SendHorizontal className={`w-4 h-4 ${currentTab === 'review' ? 'text-amber-400' : 'text-zinc-400'}`} />
            <span>Review Studio (M2)</span>
          </div>
          {pendingReviewCount > 0 && (
            <span className="text-[10px] px-1.5 py-0.5 rounded-full bg-red-500/20 text-red-300 font-mono font-semibold">
              {pendingReviewCount} pending
            </span>
          )}
        </button>

        <div className="text-[10px] font-semibold tracking-wider text-zinc-500 uppercase px-2 pt-4 mb-2">
          Engine Infrastructure
        </div>

        <button
          id="nav-hangfire"
          onClick={() => setCurrentTab('hangfire')}
          className={`w-full flex items-center justify-between px-2.5 py-2 rounded-lg text-xs font-medium transition-colors ${
            currentTab === 'hangfire'
              ? 'bg-white/10 text-white shadow-inner font-semibold'
              : 'text-zinc-400 hover:text-zinc-200 hover:bg-white/5'
          }`}
        >
          <div className="flex items-center gap-2.5">
            <Activity className={`w-4 h-4 ${currentTab === 'hangfire' ? 'text-amber-400' : 'text-zinc-400'}`} />
            <span>Hangfire Queues</span>
          </div>
          <span className="flex h-2 w-2 relative">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
            <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
          </span>
        </button>

        <button
          id="nav-mcp"
          onClick={() => setCurrentTab('mcp')}
          className={`w-full flex items-center justify-between px-2.5 py-2 rounded-lg text-xs font-medium transition-colors ${
            currentTab === 'mcp'
              ? 'bg-white/10 text-white shadow-inner font-semibold'
              : 'text-zinc-400 hover:text-zinc-200 hover:bg-white/5'
          }`}
        >
          <div className="flex items-center gap-2.5">
            <Terminal className={`w-4 h-4 ${currentTab === 'mcp' ? 'text-amber-400' : 'text-zinc-400'}`} />
            <span>MCP Server (M3)</span>
          </div>
          <span className="text-[10px] font-mono text-zinc-400 bg-white/5 px-1.5 py-0.5 rounded">6 tools</span>
        </button>
      </div>

      {/* Operator Status Footer */}
      <div className="p-3 border-t border-white/10 bg-black/20 text-[11px] text-zinc-400 space-y-2">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-1.5 text-zinc-300">
            <ShieldCheck className="w-3.5 h-3.5 text-emerald-400" />
            <span className="font-medium">Single Operator</span>
          </div>
          <span className="text-[10px] text-zinc-500 font-mono">No Tenancy</span>
        </div>
        <div className="flex items-center justify-between text-[10px] text-zinc-500">
          <span className="flex items-center gap-1">
            <Cpu className="w-3 h-3 text-amber-500" /> Ollama Llama 3.1 + MCP
          </span>
          <span className="text-emerald-400 font-mono">ONLINE</span>
        </div>
      </div>
    </aside>
  );
};
