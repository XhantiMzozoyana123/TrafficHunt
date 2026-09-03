import React, { useState } from 'react';
import { 
  Terminal, 
  Play, 
  Code, 
  CheckCircle2, 
  Copy, 
  ChevronRight, 
  Cpu,
  Layers,
  ArrowRight
} from 'lucide-react';
import { McpToolDefinition } from '../types';
import { MCP_TOOLS_REGISTRY } from '../data/initialData';

interface McpServerViewProps {
  onExecuteTool: (toolName: string, params: Record<string, any>) => void;
}

export const McpServerView: React.FC<McpServerViewProps> = ({ onExecuteTool }) => {
  const [selectedTool, setSelectedTool] = useState<McpToolDefinition>(MCP_TOOLS_REGISTRY[0]);
  const [activeTab, setActiveTab] = useState<'docs' | 'runner'>('docs');
  const [inputJson, setInputJson] = useState<string>(
    JSON.stringify(MCP_TOOLS_REGISTRY[0].sampleExecution.request, null, 2)
  );
  const [outputJson, setOutputJson] = useState<string>(
    JSON.stringify(MCP_TOOLS_REGISTRY[0].sampleExecution.response, null, 2)
  );
  const [isExecuting, setIsExecuting] = useState(false);
  const [copied, setCopied] = useState(false);

  const handleSelectTool = (tool: McpToolDefinition) => {
    setSelectedTool(tool);
    setInputJson(JSON.stringify(tool.sampleExecution.request, null, 2));
    setOutputJson(JSON.stringify(tool.sampleExecution.response, null, 2));
  };

  const handleRunExecution = () => {
    setIsExecuting(true);
    try {
      const parsed = JSON.parse(inputJson);
      setTimeout(() => {
        setIsExecuting(false);
        setOutputJson(JSON.stringify({
          jsonrpc: "2.0",
          id: `req_${Date.now().toString().slice(-4)}`,
          result: selectedTool.sampleExecution.response
        }, null, 2));
        onExecuteTool(selectedTool.name, parsed);
      }, 500);
    } catch (e: any) {
      setIsExecuting(false);
      setOutputJson(JSON.stringify({ error: "Invalid JSON parameters" }, null, 2));
    }
  };

  const copyConfig = () => {
    const config = JSON.stringify({
      mcpServers: {
        TrafficHunt: {
          command: "dotnet",
          args: ["run", "--project", "TrafficHunt.Mcp"]
        }
      }
    }, null, 2);
    navigator.clipboard.writeText(config);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="flex-1 flex flex-col h-full bg-[#0d0f15] overflow-y-auto">
      {/* Header */}
      <div className="p-6 md:p-8 border-b border-white/10 bg-[#12141c]/50">
        <div className="max-w-6xl mx-auto flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 text-xs font-mono text-amber-400 mb-1">
              <Terminal className="w-4 h-4" />
              <span>MILESTONE 3: MODEL CONTEXT PROTOCOL (MCP)</span>
            </div>
            <h1 className="text-2xl font-bold tracking-tight text-white">
              TrafficHunt MCP Server Inspector
            </h1>
            <p className="text-xs text-zinc-400 mt-1">
              Exposes Clean Architecture Application services to AI operators (Claude Desktop, Cursor, AI Chat) via standard JSON-RPC stdio.
            </p>
          </div>

          <button
            onClick={copyConfig}
            className="flex items-center gap-2 px-3.5 py-2 rounded-xl bg-white/10 hover:bg-white/20 text-xs font-mono text-zinc-200 transition-colors"
          >
            <Copy className="w-3.5 h-3.5 text-amber-400" />
            <span>{copied ? 'Copied claude_desktop_config.json' : 'Copy Claude Config'}</span>
          </button>
        </div>
      </div>

      {/* Main MCP Console Two-Column Layout */}
      <div className="max-w-6xl mx-auto w-full p-6 md:p-8 grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Tool Selector List */}
        <div className="lg:col-span-4 space-y-3">
          <div className="text-xs font-semibold text-zinc-400 uppercase tracking-wider px-1">
            Registered MCP Tools ({MCP_TOOLS_REGISTRY.length})
          </div>

          <div className="space-y-2">
            {MCP_TOOLS_REGISTRY.map((tool) => {
              const isSelected = selectedTool.name === tool.name;
              return (
                <div
                  key={tool.name}
                  id={`mcp-tool-${tool.name}`}
                  onClick={() => handleSelectTool(tool)}
                  className={`p-3.5 rounded-xl border transition-all cursor-pointer ${
                    isSelected
                      ? 'bg-amber-500/10 border-amber-500/50 shadow-sm'
                      : 'bg-white/[0.02] hover:bg-white/[0.05] border-white/10'
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-mono font-bold text-amber-400">
                      {tool.name}
                    </span>
                    <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-white/5 text-zinc-400">
                      {tool.milestone}
                    </span>
                  </div>
                  <p className="text-[11px] text-zinc-400 mt-1.5 leading-relaxed">
                    {tool.purpose}
                  </p>
                </div>
              );
            })}
          </div>
        </div>

        {/* Selected Tool Details & Runner */}
        <div className="lg:col-span-8 space-y-4">
          <div className="p-5 rounded-2xl bg-white/[0.03] border border-white/10 space-y-4">
            <div className="flex items-center justify-between border-b border-white/10 pb-3">
              <div>
                <h2 className="text-base font-bold text-white font-mono flex items-center gap-2">
                  <Terminal className="w-4 h-4 text-amber-400" />
                  <span>tool: {selectedTool.name}</span>
                </h2>
                <p className="text-xs text-zinc-400 mt-1">{selectedTool.description}</p>
              </div>

              <div className="flex items-center gap-1 bg-black/30 p-1 rounded-lg border border-white/5 text-xs">
                <button
                  onClick={() => setActiveTab('docs')}
                  className={`px-3 py-1 rounded font-medium transition-colors ${
                    activeTab === 'docs' ? 'bg-amber-500 text-zinc-950 font-bold' : 'text-zinc-400 hover:text-white'
                  }`}
                >
                  Parameters
                </button>
                <button
                  onClick={() => setActiveTab('runner')}
                  className={`px-3 py-1 rounded font-medium transition-colors ${
                    activeTab === 'runner' ? 'bg-amber-500 text-zinc-950 font-bold' : 'text-zinc-400 hover:text-white'
                  }`}
                >
                  Test Runner
                </button>
              </div>
            </div>

            {activeTab === 'docs' ? (
              <div className="space-y-4">
                <div className="text-xs font-semibold text-zinc-300 uppercase tracking-wider">
                  Input Schema
                </div>
                <div className="space-y-2">
                  {selectedTool.parameters.map((param) => (
                    <div
                      key={param.name}
                      className="p-3 rounded-xl bg-black/30 border border-white/5 flex flex-col sm:flex-row sm:items-center justify-between gap-2 text-xs"
                    >
                      <div className="font-mono">
                        <span className="text-amber-400 font-bold">{param.name}</span>
                        <span className="text-zinc-500 ml-2">({param.type})</span>
                        {param.required && (
                          <span className="ml-2 text-[10px] text-red-400 bg-red-500/10 px-1.5 py-0.2 rounded">required</span>
                        )}
                      </div>
                      <div className="text-zinc-400 text-[11px] sm:text-right max-w-md">
                        {param.description}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            ) : (
              <div className="space-y-4">
                <div>
                  <div className="flex items-center justify-between text-xs text-zinc-400 mb-1.5 font-mono">
                    <span>JSON-RPC Parameters:</span>
                    <button
                      onClick={handleRunExecution}
                      disabled={isExecuting}
                      className="flex items-center gap-1.5 px-3 py-1 rounded-lg bg-amber-500 hover:bg-amber-400 text-zinc-950 font-bold text-xs cursor-pointer"
                    >
                      <Play className="w-3 h-3" />
                      <span>{isExecuting ? 'Calling...' : 'Invoke Tool'}</span>
                    </button>
                  </div>
                  <textarea
                    rows={6}
                    value={inputJson}
                    onChange={(e) => setInputJson(e.target.value)}
                    className="w-full p-3 bg-black/40 border border-white/10 rounded-xl font-mono text-xs text-amber-300 focus:outline-none focus:border-amber-500 leading-relaxed"
                  />
                </div>

                <div>
                  <div className="text-xs text-zinc-400 mb-1.5 font-mono">
                    Result (JSON-RPC stdio response):
                  </div>
                  <pre className="p-3 bg-black/60 border border-white/10 rounded-xl font-mono text-xs text-emerald-300 overflow-x-auto max-h-60 leading-relaxed">
                    {outputJson}
                  </pre>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
