import React, { useState, useRef, useEffect } from 'react';
import { 
  Send, 
  Bot, 
  User, 
  Sparkles, 
  Wrench, 
  ChevronRight, 
  ChevronDown, 
  ExternalLink, 
  CheckCircle2, 
  Layers, 
  ArrowUpRight,
  Terminal,
  Zap
} from 'lucide-react';
import { ChatMessage, ToolCallItem, ChatArtifact } from '../types';

interface ChatViewProps {
  messages: ChatMessage[];
  onSendMessage: (text: string) => void;
  isLoading: boolean;
  onOpenArtifact: (artifact: ChatArtifact) => void;
  selectedModel: string;
  setSelectedModel: (model: string) => void;
}

export const ChatView: React.FC<ChatViewProps> = ({
  messages,
  onSendMessage,
  isLoading,
  onOpenArtifact,
  selectedModel,
  setSelectedModel
}) => {
  const [input, setInput] = useState('');
  const [expandedTools, setExpandedTools] = useState<Record<string, boolean>>({});
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages, isLoading]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!input.trim() || isLoading) return;
    const text = input;
    setInput('');
    onSendMessage(text);
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit(e);
    }
  };

  const toggleToolExpand = (toolId: string) => {
    setExpandedTools(prev => ({
      ...prev,
      [toolId]: !prev[toolId]
    }));
  };

  const starterPrompts = [
    {
      title: "Promote TubeMail Gorilla",
      desc: "Video outreach tool for freelance video editors struggling to find clients on YouTube",
      prompt: "Promote TubeMail Gorilla: A video outreach tool for freelance video editors struggling to find clients on YouTube"
    },
    {
      title: "Promote Podcast Repurposing SaaS",
      desc: "Chops 60min podcast interviews into viral retention shorts with dynamic captions",
      prompt: "Promote ClipFlow AI: Automated long-form podcast & webinar to viral YouTube Shorts and TikTok repurposing workflow"
    },
    {
      title: "Draft High-Value Outreach",
      desc: "Generate personalized comment reply for prospect @alex_edits_nyc",
      prompt: "Generate a value-first YouTube outreach reply for prospect Alex Thorne (@alex_edits_nyc) offering our operator client acquisition checklist"
    },
    {
      title: "Scan High-Intent Prospects",
      desc: "Find all prospects with Intent Score >= 85 who have not been contacted yet",
      prompt: "Find prospects for campaign TubeMail Gorilla with minimum intent score 85 and status new"
    }
  ];

  return (
    <div className="flex-1 flex flex-col h-full bg-[#0d0f15] relative overflow-hidden">
      {/* Top Bar / Model Selector */}
      <div className="h-14 px-6 border-b border-white/10 flex items-center justify-between bg-[#12141c]/60 backdrop-blur-md z-10">
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg bg-white/5 border border-white/10 text-xs text-zinc-300">
            <Bot className="w-3.5 h-3.5 text-amber-400" />
            <span className="font-semibold text-white">Operator Mode:</span>
            <select
              id="model-selector"
              value={selectedModel}
              onChange={(e) => setSelectedModel(e.target.value)}
              className="bg-transparent border-none text-amber-400 font-medium focus:outline-none cursor-pointer"
            >
              <option value="claude-3-7-sonnet" className="bg-zinc-900 text-zinc-100">
                Claude 3.7 Sonnet (via TrafficHunt MCP)
              </option>
              <option value="llama-3-1" className="bg-zinc-900 text-zinc-100">
                Ollama Llama 3.1 (Local AI Engine)
              </option>
              <option value="gemini-2-5-flash" className="bg-zinc-900 text-zinc-100">
                Gemini 2.5 Flash (Cloud API)
              </option>
            </select>
          </div>
          <span className="text-[11px] text-zinc-500 font-mono hidden md:inline">
            Stdio JSON-RPC MCP Server Active
          </span>
        </div>

        <div className="flex items-center gap-2">
          <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-emerald-500/10 border border-emerald-500/20 text-emerald-400 text-[11px] font-mono">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse"></span>
            Clean Architecture .NET 10 + React 19
          </div>
        </div>
      </div>

      {/* Messages Scroll Area */}
      <div className="flex-1 overflow-y-auto px-4 md:px-8 py-6 space-y-6">
        {messages.length === 0 ? (
          <div className="max-w-3xl mx-auto mt-8 space-y-8">
            <div className="text-center space-y-3">
              <div className="inline-flex items-center justify-center w-14 h-14 rounded-2xl bg-gradient-to-tr from-amber-500/20 to-amber-600/10 border border-amber-500/30 text-amber-400 mb-2 shadow-inner">
                <Sparkles className="w-7 h-7" />
              </div>
              <h1 className="text-2xl md:text-3xl font-bold tracking-tight text-white">
                What are we promoting today?
              </h1>
              <p className="text-sm text-zinc-400 max-w-xl mx-auto leading-relaxed">
                TrafficHunt doesn&apos;t know what app we&apos;re promoting until you tell it.
                Type what you&apos;re offering in plain English. The AI generates the campaign, searches YouTube videos, analyzes comment pain points, and extracts high-intent prospects.
              </p>
            </div>

            {/* Starter Prompt Cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3 pt-4">
              {starterPrompts.map((item, idx) => (
                <button
                  key={idx}
                  id={`starter-card-${idx}`}
                  onClick={() => onSendMessage(item.prompt)}
                  className="text-left p-4 rounded-xl bg-white/[0.03] hover:bg-white/[0.07] border border-white/10 hover:border-amber-500/40 transition-all group relative overflow-hidden"
                >
                  <div className="flex items-start justify-between">
                    <h2 className="text-xs font-semibold text-zinc-200 group-hover:text-amber-400 transition-colors flex items-center gap-1.5">
                      {item.title}
                      <ArrowUpRight className="w-3.5 h-3.5 opacity-0 group-hover:opacity-100 transition-opacity text-amber-400" />
                    </h2>
                  </div>
                  <p className="text-[11px] text-zinc-400 mt-1.5 line-clamp-2">
                    {item.desc}
                  </p>
                </button>
              ))}
            </div>

            <div className="p-4 rounded-xl bg-amber-500/5 border border-amber-500/20 flex items-start gap-3">
              <Zap className="w-4 h-4 text-amber-400 flex-shrink-0 mt-0.5" />
              <div className="text-xs text-zinc-400 leading-relaxed">
                <span className="font-semibold text-zinc-200">Human Approval Boundary:</span> The AI can search, analyze, score and generate replies, but <strong className="text-amber-300">never publishes outreach automatically</strong>. The operator controls the final send.
              </div>
            </div>
          </div>
        ) : (
          <div className="max-w-3xl mx-auto space-y-6">
            {messages.map((msg) => (
              <div
                key={msg.id}
                id={`message-${msg.id}`}
                className={`flex gap-3.5 ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}
              >
                {msg.role === 'assistant' && (
                  <div className="w-8 h-8 rounded-lg bg-amber-500/10 border border-amber-500/30 flex items-center justify-center text-amber-400 flex-shrink-0 mt-0.5">
                    <Bot className="w-4 h-4" />
                  </div>
                )}

                <div className={`max-w-[85%] space-y-3 ${msg.role === 'user' ? 'items-end' : 'items-start'}`}>
                  {/* Tool Calls Widget */}
                  {msg.toolCalls && msg.toolCalls.length > 0 && (
                    <div className="space-y-2">
                      {msg.toolCalls.map((tool) => (
                        <div
                          key={tool.id}
                          className="rounded-lg bg-white/[0.04] border border-white/10 overflow-hidden text-xs"
                        >
                          <div
                            onClick={() => toggleToolExpand(tool.id)}
                            className="px-3 py-2 flex items-center justify-between cursor-pointer hover:bg-white/[0.03] transition-colors"
                          >
                            <div className="flex items-center gap-2">
                              <Wrench className="w-3.5 h-3.5 text-amber-400" />
                              <span className="font-mono font-semibold text-zinc-300">
                                tool_use: <span className="text-amber-400">{tool.toolName}</span>
                              </span>
                              <span className="px-1.5 py-0.2 rounded bg-emerald-500/20 text-emerald-300 text-[10px] font-mono">
                                completed
                              </span>
                            </div>
                            <div className="text-zinc-500">
                              {expandedTools[tool.id] ? (
                                <ChevronDown className="w-3.5 h-3.5" />
                              ) : (
                                <ChevronRight className="w-3.5 h-3.5" />
                              )}
                            </div>
                          </div>

                          {tool.summary && (
                            <div className="px-3 py-1.5 border-t border-white/5 bg-black/20 text-[11px] text-zinc-400">
                              {tool.summary}
                            </div>
                          )}

                          {expandedTools[tool.id] && (
                            <div className="p-3 border-t border-white/5 bg-black/40 font-mono text-[11px] space-y-2">
                              <div className="text-zinc-500">Parameters:</div>
                              <pre className="text-amber-300 overflow-x-auto p-2 bg-black/40 rounded border border-white/5">
                                {JSON.stringify(tool.parameters, null, 2)}
                              </pre>
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  )}

                  {/* Message Bubble */}
                  <div
                    className={`rounded-2xl px-4 py-3 text-sm leading-relaxed ${
                      msg.role === 'user'
                        ? 'bg-amber-500 text-zinc-950 font-medium ml-auto'
                        : 'bg-white/[0.05] border border-white/10 text-zinc-200'
                    }`}
                  >
                    <div className="whitespace-pre-wrap select-text">{msg.content}</div>
                  </div>

                  {/* Artifact Card (Claude Style) */}
                  {msg.artifact && (
                    <div
                      id={`artifact-card-${msg.artifact.id}`}
                      onClick={() => onOpenArtifact(msg.artifact!)}
                      className="p-3.5 rounded-xl bg-gradient-to-br from-amber-500/10 to-transparent border border-amber-500/30 hover:border-amber-500/60 cursor-pointer transition-all group flex items-center justify-between"
                    >
                      <div className="flex items-center gap-3">
                        <div className="w-9 h-9 rounded-lg bg-amber-500/20 text-amber-400 flex items-center justify-center flex-shrink-0">
                          <Layers className="w-4 h-4" />
                        </div>
                        <div>
                          <div className="text-xs font-semibold text-zinc-200 group-hover:text-amber-300 transition-colors">
                            {msg.artifact.title}
                          </div>
                          <div className="text-[10px] text-zinc-400 font-mono">
                            Click to open in operator view & review pipeline
                          </div>
                        </div>
                      </div>
                      <ExternalLink className="w-4 h-4 text-zinc-400 group-hover:text-amber-400 transition-colors" />
                    </div>
                  )}

                  <div className="text-[10px] text-zinc-500 px-1 font-mono">
                    {new Date(msg.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                  </div>
                </div>

                {msg.role === 'user' && (
                  <div className="w-8 h-8 rounded-lg bg-white/10 flex items-center justify-center text-zinc-300 flex-shrink-0 mt-0.5">
                    <User className="w-4 h-4" />
                  </div>
                )}
              </div>
            ))}

            {isLoading && (
              <div className="flex gap-3.5 items-start">
                <div className="w-8 h-8 rounded-lg bg-amber-500/10 border border-amber-500/30 flex items-center justify-center text-amber-400 flex-shrink-0">
                  <Bot className="w-4 h-4" />
                </div>
                <div className="px-4 py-3 rounded-2xl bg-white/[0.05] border border-white/10 text-xs text-zinc-400 flex items-center gap-2">
                  <div className="w-1.5 h-1.5 rounded-full bg-amber-400 animate-bounce"></div>
                  <div className="w-1.5 h-1.5 rounded-full bg-amber-400 animate-bounce [animation-delay:0.2s]"></div>
                  <div className="w-1.5 h-1.5 rounded-full bg-amber-400 animate-bounce [animation-delay:0.4s]"></div>
                  <span className="font-mono text-[11px] text-zinc-400 ml-1">TrafficHunt engine executing MCP discovery...</span>
                </div>
              </div>
            )}

            <div ref={messagesEndRef} />
          </div>
        )}
      </div>

      {/* Chat Input Floating Area */}
      <div className="p-4 md:p-6 border-t border-white/10 bg-[#12141c]/80 backdrop-blur-md">
        <form onSubmit={handleSubmit} className="max-w-3xl mx-auto relative">
          <div className="relative rounded-2xl bg-white/[0.05] border border-white/15 focus-within:border-amber-500/60 focus-within:ring-2 focus-within:ring-amber-500/20 transition-all overflow-hidden">
            <textarea
              id="chat-input"
              rows={2}
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Tell TrafficHunt what you are promoting or describe your target audience problem..."
              className="w-full bg-transparent px-4 py-3.5 pr-24 text-sm text-zinc-100 placeholder-zinc-500 focus:outline-none resize-none leading-relaxed"
            />
            <div className="absolute right-2.5 bottom-2.5 flex items-center gap-1.5">
              <button
                id="btn-send-message"
                type="submit"
                disabled={!input.trim() || isLoading}
                className="p-2 rounded-xl bg-amber-500 hover:bg-amber-400 disabled:opacity-30 disabled:hover:bg-amber-500 text-zinc-950 font-bold transition-all shadow active:scale-95 cursor-pointer disabled:cursor-not-allowed"
                title="Execute hunt (Enter)"
              >
                <Send className="w-4 h-4" />
              </button>
            </div>
          </div>
          <div className="flex items-center justify-between mt-2 px-1 text-[11px] text-zinc-500">
            <span>Shift + Enter for new line. Type <code className="text-zinc-400 bg-white/5 px-1 py-0.5 rounded">Promote [product]</code> to launch discovery.</span>
            <span className="font-mono text-[10px]">MCP Tool: promote</span>
          </div>
        </form>
      </div>
    </div>
  );
};
