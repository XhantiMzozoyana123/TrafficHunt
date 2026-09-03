import React from 'react';
import { 
  Activity, 
  Cpu, 
  Video, 
  Bell, 
  Wrench, 
  Clock, 
  CheckCircle2, 
  AlertCircle,
  Play,
  RotateCw
} from 'lucide-react';
import { HangfireJob } from '../types';

interface HangfireViewProps {
  jobs: HangfireJob[];
  onTriggerJob: (jobName: string) => void;
}

export const HangfireView: React.FC<HangfireViewProps> = ({ jobs, onTriggerJob }) => {
  const queues = [
    {
      name: 'youtube',
      label: 'YouTube Fetching Queue',
      workers: 10,
      activeJobs: jobs.filter(j => j.queue === 'youtube' && j.state === 'Processing').length,
      color: 'text-red-400',
      bgColor: 'bg-red-500/10',
      borderColor: 'border-red-500/20',
      icon: Video,
      description: 'YoutubeExplode passive search & YouTube Data API v3 comment collection. High concurrency, network I/O bound.'
    },
    {
      name: 'ai',
      label: 'Ollama AI Qualification Queue',
      workers: 2,
      activeJobs: jobs.filter(j => j.queue === 'ai' && j.state === 'Processing').length,
      color: 'text-amber-400',
      bgColor: 'bg-amber-500/10',
      borderColor: 'border-amber-500/20',
      icon: Cpu,
      description: 'Ollama Llama 3.1 structured JSON analysis. Restricted to 2 workers to prevent GPU/CPU saturation.'
    },
    {
      name: 'notifications',
      label: 'Notifications Queue',
      workers: 5,
      activeJobs: jobs.filter(j => j.queue === 'notifications' && j.state === 'Processing').length,
      color: 'text-blue-400',
      bgColor: 'bg-blue-500/10',
      borderColor: 'border-blue-500/20',
      icon: Bell,
      description: 'Operator alerts, webhooks, and desktop lead ping dispatches.'
    },
    {
      name: 'maintenance',
      label: 'Maintenance Queue',
      workers: 1,
      activeJobs: jobs.filter(j => j.queue === 'maintenance' && j.state === 'Processing').length,
      color: 'text-zinc-400',
      bgColor: 'bg-zinc-500/10',
      borderColor: 'border-zinc-500/20',
      icon: Wrench,
      description: 'Cache purges, aggregates refresh, and recurring channel health audits.'
    }
  ];

  return (
    <div className="flex-1 flex flex-col h-full bg-[#0d0f15] overflow-y-auto">
      {/* Header */}
      <div className="p-6 md:p-8 border-b border-white/10 bg-[#12141c]/50">
        <div className="max-w-6xl mx-auto flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 text-xs font-mono text-amber-400 mb-1">
              <Activity className="w-4 h-4" />
              <span>BACKGROUND PROCESSING PIPELINE</span>
            </div>
            <h1 className="text-2xl font-bold tracking-tight text-white">
              Hangfire Queue Monitor & Jobs
            </h1>
            <p className="text-xs text-zinc-400 mt-1">
              Persistent background processing pipeline with queue separation to prevent AI inference bottlenecks.
            </p>
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={() => onTriggerJob('YouTubeDiscoveryJob')}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-white/10 hover:bg-white/20 text-xs font-semibold text-white transition-colors"
            >
              <RotateCw className="w-3.5 h-3.5" />
              <span>Poll Queues</span>
            </button>
          </div>
        </div>

        {/* Queue Separation Grid */}
        <div className="max-w-6xl mx-auto grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mt-6">
          {queues.map((q) => {
            const Icon = q.icon;
            return (
              <div
                key={q.name}
                className={`p-4 rounded-2xl bg-white/[0.02] border ${q.borderColor} space-y-3`}
              >
                <div className="flex items-center justify-between">
                  <div className={`p-2 rounded-lg ${q.bgColor} ${q.color}`}>
                    <Icon className="w-4 h-4" />
                  </div>
                  <div className="text-right">
                    <span className="text-xs font-mono font-bold text-white">
                      {q.workers} Workers
                    </span>
                    <div className="text-[10px] text-zinc-500">
                      {q.activeJobs} active
                    </div>
                  </div>
                </div>

                <div>
                  <h3 className="text-xs font-bold text-white">{q.label}</h3>
                  <p className="text-[11px] text-zinc-400 mt-1 leading-relaxed line-clamp-3">
                    {q.description}
                  </p>
                </div>

                <div className="pt-2 border-t border-white/5 flex items-center justify-between text-[10px] font-mono">
                  <span className="text-zinc-500">Queue: {q.name}</span>
                  <span className="text-emerald-400">HEALTHY</span>
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Jobs Execution History */}
      <div className="p-6 md:p-8 max-w-6xl mx-auto w-full space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-xs font-semibold text-zinc-400 uppercase tracking-wider">
            Active & Recent Background Jobs
          </h2>
          <span className="text-[11px] text-zinc-500 font-mono">MySQL Pomelo Storage</span>
        </div>

        <div className="rounded-2xl bg-white/[0.02] border border-white/10 overflow-hidden">
          <div className="divide-y divide-white/5 text-xs font-mono">
            {jobs.map((job) => (
              <div
                key={job.id}
                className="p-4 flex flex-col md:flex-row md:items-center justify-between gap-3 hover:bg-white/[0.02] transition-colors"
              >
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <span className="text-amber-400 font-bold">{job.jobName}</span>
                    <span className="text-[10px] px-1.5 py-0.5 rounded bg-white/5 text-zinc-400">
                      {job.id}
                    </span>
                    <span className="text-[10px] px-1.5 py-0.5 rounded bg-white/5 text-zinc-400 font-sans">
                      queue: {job.queue}
                    </span>
                  </div>
                  <p className="text-zinc-300 font-sans text-xs">{job.details}</p>
                </div>

                <div className="flex items-center gap-4">
                  {job.progress !== undefined && job.state === 'Processing' && (
                    <div className="w-24 bg-white/10 rounded-full h-1.5 overflow-hidden">
                      <div
                        className="bg-amber-500 h-full rounded-full transition-all"
                        style={{ width: `${job.progress}%` }}
                      ></div>
                    </div>
                  )}

                  <div className="text-right">
                    <div className="flex items-center gap-1.5">
                      {job.state === 'Processing' ? (
                        <span className="flex items-center gap-1 text-amber-400 font-semibold text-[11px]">
                          <span className="w-1.5 h-1.5 rounded-full bg-amber-400 animate-ping"></span>
                          Processing ({job.duration})
                        </span>
                      ) : (
                        <span className="flex items-center gap-1 text-emerald-400 font-semibold text-[11px]">
                          <CheckCircle2 className="w-3.5 h-3.5" />
                          Succeeded
                        </span>
                      )}
                    </div>
                    <div className="text-[10px] text-zinc-500 font-sans">
                      {new Date(job.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};
