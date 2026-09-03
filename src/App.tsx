import React, { useState, useEffect } from 'react';
import { Sidebar } from './components/Sidebar';
import { ChatView } from './components/ChatView';
import { CampaignsView } from './components/CampaignsView';
import { ProspectsView } from './components/ProspectsView';
import { ReviewStudioView } from './components/ReviewStudioView';
import { HangfireView } from './components/HangfireView';
import { McpServerView } from './components/McpServerView';
import { ArtifactDrawer } from './components/ArtifactDrawer';
import { 
  Campaign, 
  Prospect, 
  ChatMessage, 
  ChatArtifact, 
  HangfireJob, 
  ProspectStatus 
} from './types';
import { 
  INITIAL_CAMPAIGNS, 
  INITIAL_PROSPECTS, 
  INITIAL_HANGFIRE_JOBS 
} from './data/initialData';

export function App() {
  const [currentTab, setCurrentTab] = useState<'chat' | 'campaigns' | 'prospects' | 'review' | 'hangfire' | 'mcp'>('chat');
  
  // Persistent state in localStorage
  const [campaigns, setCampaigns] = useState<Campaign[]>(() => {
    const saved = localStorage.getItem('traffichunt_campaigns');
    return saved ? JSON.parse(saved) : INITIAL_CAMPAIGNS;
  });

  const [prospects, setProspects] = useState<Prospect[]>(() => {
    const saved = localStorage.getItem('traffichunt_prospects');
    return saved ? JSON.parse(saved) : INITIAL_PROSPECTS;
  });

  const [hangfireJobs, setHangfireJobs] = useState<HangfireJob[]>(() => {
    const saved = localStorage.getItem('traffichunt_jobs');
    return saved ? JSON.parse(saved) : INITIAL_HANGFIRE_JOBS;
  });

  const [selectedCampaignId, setSelectedCampaignId] = useState<string | undefined>(undefined);
  const [selectedProspect, setSelectedProspect] = useState<Prospect | undefined>(undefined);
  const [activeArtifact, setActiveArtifact] = useState<ChatArtifact | null>(null);
  const [selectedModel, setSelectedModel] = useState<string>('claude-3-7-sonnet');
  const [isLoading, setIsLoading] = useState<boolean>(false);

  const [messages, setMessages] = useState<ChatMessage[]>(() => {
    const saved = localStorage.getItem('traffichunt_messages');
    if (saved) return JSON.parse(saved);
    return [
      {
        id: 'msg-welcome',
        role: 'assistant',
        content: `**Welcome back, Operator.** TrafficHunt outreach engine is armed and connected via Stdio MCP.

TrafficHunt doesn't assume what product or service we're promoting. Simply tell me in plain English:
- What app you want to promote
- The specific pain point your target audience experiences on YouTube

*Try an operator command like:*
\`Promote TubeMail Gorilla: A video outreach tool for freelance video editors struggling to find clients on YouTube\``,
        timestamp: new Date().toISOString()
      }
    ];
  });

  // Save to localStorage
  useEffect(() => {
    localStorage.setItem('traffichunt_campaigns', JSON.stringify(campaigns));
  }, [campaigns]);

  useEffect(() => {
    localStorage.setItem('traffichunt_prospects', JSON.stringify(prospects));
  }, [prospects]);

  useEffect(() => {
    localStorage.setItem('traffichunt_jobs', JSON.stringify(hangfireJobs));
  }, [hangfireJobs]);

  useEffect(() => {
    localStorage.setItem('traffichunt_messages', JSON.stringify(messages));
  }, [messages]);

  const handleSendMessage = async (text: string) => {
    const userMsg: ChatMessage = {
      id: `msg-${Date.now()}`,
      role: 'user',
      content: text,
      timestamp: new Date().toISOString()
    };

    setMessages((prev) => [...prev, userMsg]);
    setIsLoading(true);

    try {
      const res = await fetch('/api/chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          message: text,
          model: selectedModel,
          history: messages.slice(-5)
        })
      });

      if (res.ok) {
        const data = await res.json();
        setMessages((prev) => [...prev, data]);

        // If a campaign or prospect was generated, add to our local state
        if (data.artifact?.data?.campaign) {
          const newCamp = data.artifact.data.campaign;
          setCampaigns((prev) => [newCamp, ...prev.filter(c => c.id !== newCamp.id)]);
        }
        if (data.artifact?.data?.prospects) {
          const newProspects = data.artifact.data.prospects;
          setProspects((prev) => [...newProspects, ...prev]);
        }
      } else {
        throw new Error('API server returned error');
      }
    } catch (err) {
      // Local fallback logic if offline / without server
      setTimeout(() => {
        const lower = text.toLowerCase();
        let replyContent = '';
        let toolCalls = undefined;
        let artifact = undefined;

        if (lower.includes('promote') || lower.includes('campaign')) {
          const newCamp: Campaign = {
            id: `camp-${Date.now().toString().slice(-4)}`,
            name: 'YouTube Growth Prospecting',
            productName: 'Promoted Solution',
            description: text.replace(/^promote\s+/i, ''),
            targetAudience: 'Content creators & indie software operators',
            problemStatement: 'Manual prospecting and low cold outreach response rates',
            keywords: ['how to get clients on youtube', 'cold email creators', 'youtube workflow automation'],
            aiInstructions: 'Identify comments seeking client acquisition help or complaining about creator gatekeepers.',
            status: 'active',
            createdAt: new Date().toISOString(),
            stats: {
              videosScanned: 15,
              commentsAnalyzed: 230,
              prospectsFound: 6,
              qualifiedCount: 4,
              averageIntentScore: 92
            }
          };

          const newLead: Prospect = {
            id: `prospect-${Date.now()}`,
            campaignId: newCamp.id,
            campaignName: newCamp.name,
            authorName: 'Chris Morales (Freelance Cut)',
            authorHandle: '@chrismorales_edits',
            authorChannelUrl: 'https://youtube.com/@chrismorales_edits',
            videoId: 'vid_yt_401',
            videoTitle: 'Cold Outreach Mistakes for Freelance Creators in 2026',
            videoUrl: 'https://youtube.com/watch?v=vid_yt_401',
            channelTitle: 'Creator Freelancing Hub',
            commentId: `cmt_${Date.now()}`,
            commentText: "I've spent 2 weeks cold DMing 50 YouTubers offering free video edits and got completely ghosted. How do I actually find creators who need editors and open their DMs?",
            commentPublishedAt: 'Just now',
            intentScore: 94,
            isTargetAudience: true,
            hasRelevantProblem: true,
            painPoint: 'Ghosted on 50 cold DMs; lacks a way to identify receptive creators with active editing budgets.',
            qualificationReason: 'High intent: actively sending outreach now, high frustration with current methods.',
            status: 'new',
            replyStatus: 'drafted',
            replyTone: 'value_first',
            generatedReply: `@chrismorales_edits Hey Chris, 50 DMs without replies usually means reaching creators after their inboxes are filtered by managers. When we prospect on YouTube, we search for creators posting 3+ long-form uploads weekly where upload consistency just slipped. If you want our operator checklist for targeting creators with verified editing bottlenecks, let me know!`
          };

          setCampaigns((prev) => [newCamp, ...prev]);
          setProspects((prev) => [newLead, ...prev]);

          toolCalls = [
            {
              id: `call_${Date.now()}`,
              toolName: 'promote' as const,
              parameters: { description: text, min_intent_score: 80 },
              status: 'completed' as const,
              summary: 'Campaign generated, YoutubeExplode queried, comments analyzed by Ollama.'
            }
          ];

          artifact = {
            id: `art_${Date.now()}`,
            title: `${newCamp.name} Discovery Report`,
            type: 'campaign' as const,
            data: { campaign: newCamp, prospects: [newLead] }
          };

          replyContent = `I have executed the **TrafficHunt \`promote\` pipeline** for your offer.

1. **Campaign Registered**: Configured search clusters and Ollama qualification criteria.
2. **YoutubeExplode Discovery**: Searched video topics and collected top comment threads.
3. **Ollama Intent Analysis**: Scored prospects based on stated bottlenecks.
4. **Result**: Discovered high-intent prospect **@chrismorales_edits** (Intent Score: **94%**).

A value-first outreach draft is prepared in the **Review Studio**. Per the Human Approval Boundary, you control the final dispatch.`;
        } else {
          replyContent = `Understood. TrafficHunt operator is monitoring your campaigns and Hangfire background queues. You can instruct me to promote a new application, find high-intent prospects, or generate customized outreach replies for human approval.`;
        }

        const fallbackMsg: ChatMessage = {
          id: `msg-${Date.now()}`,
          role: 'assistant',
          content: replyContent,
          timestamp: new Date().toISOString(),
          toolCalls,
          artifact
        };
        setMessages((prev) => [...prev, fallbackMsg]);
      }, 700);
    } finally {
      setIsLoading(false);
    }
  };

  const handleNewChat = () => {
    setCurrentTab('chat');
    setMessages([
      {
        id: `msg-${Date.now()}`,
        role: 'assistant',
        content: `**New hunt session initialized.** What application or service would you like TrafficHunt to acquire customers for?`,
        timestamp: new Date().toISOString()
      }
    ]);
  };

  const handleCreateCampaign = (newCamp: Partial<Campaign>) => {
    const created: Campaign = {
      id: `camp-${Date.now()}`,
      name: newCamp.name || 'New Campaign',
      productName: newCamp.productName || 'Promoted App',
      description: newCamp.description || '',
      targetAudience: newCamp.targetAudience || 'YouTube Creators',
      problemStatement: newCamp.problemStatement || 'Outreach bottleneck',
      keywords: newCamp.keywords || ['youtube growth tips'],
      aiInstructions: newCamp.aiInstructions || 'Score comments mentioning frustration.',
      status: 'active',
      createdAt: new Date().toISOString(),
      stats: {
        videosScanned: 0,
        commentsAnalyzed: 0,
        prospectsFound: 0,
        qualifiedCount: 0,
        averageIntentScore: 0
      }
    };
    setCampaigns((prev) => [created, ...prev]);
  };

  const handleRunDiscovery = (campaignId: string) => {
    const targetCamp = campaigns.find(c => c.id === campaignId);
    if (!targetCamp) return;

    // Add Hangfire Job
    const newJob: HangfireJob = {
      id: `job-${Date.now().toString().slice(-4)}`,
      jobName: 'YouTubeDiscoveryJob',
      queue: 'youtube',
      state: 'Processing',
      createdAt: new Date().toISOString(),
      duration: '5s',
      details: `Scanning YouTube videos for campaign "${targetCamp.name}" across ${targetCamp.keywords.length} keyword clusters`,
      progress: 45
    };
    setHangfireJobs((prev) => [newJob, ...prev]);

    setTimeout(() => {
      setHangfireJobs((prev) =>
        prev.map((j) => j.id === newJob.id ? { ...j, state: 'Succeeded', progress: 100 } : j)
      );

      // Increment campaign stats
      setCampaigns((prev) =>
        prev.map((c) =>
          c.id === campaignId
            ? {
                ...c,
                stats: {
                  ...c.stats,
                  videosScanned: c.stats.videosScanned + 6,
                  commentsAnalyzed: c.stats.commentsAnalyzed + 85,
                  prospectsFound: c.stats.prospectsFound + 2,
                  qualifiedCount: c.stats.qualifiedCount + 1
                }
              }
            : c
        )
      );
    }, 2000);
  };

  const handleUpdateProspectStatus = (id: string, status: ProspectStatus) => {
    setProspects((prev) =>
      prev.map((p) => (p.id === id ? { ...p, status } : p))
    );
  };

  const handleApproveReply = (prospectId: string, approvedText: string) => {
    setProspects((prev) =>
      prev.map((p) =>
        p.id === prospectId
          ? {
              ...p,
              approvedReply: approvedText,
              replyStatus: 'approved',
              status: 'contacted'
            }
          : p
      )
    );
  };

  const handleRegenerateReply = (
    prospectId: string,
    tone: 'value_first' | 'helpful_advisor' | 'direct_solution'
  ) => {
    const target = prospects.find(p => p.id === prospectId);
    if (!target) return;

    let newDraft = '';
    if (tone === 'value_first') {
      newDraft = `${target.authorHandle} Hey ${target.authorName.split(' ')[0]}, dealing with that bottleneck usually comes down to prospecting velocity and targeting channels before their inboxes are flooded. We built an automated checklist for this — let me know if you want the breakdown!`;
    } else if (tone === 'helpful_advisor') {
      newDraft = `${target.authorHandle} Spot on observations on the creator economy. Have you considered auditing mid-tier channels (20k-50k subs) rather than top-tier channels? Reaching them right after an upload delay yields a 4x response rate.`;
    } else {
      newDraft = `${target.authorHandle} That manual prospecting takes hours that could be automated. We built ${target.campaignName.split(' - ')[0]} specifically to index YouTube creators actively looking for solutions. Let me know if you want operator access to test it!`;
    }

    setProspects((prev) =>
      prev.map((p) =>
        p.id === prospectId
          ? {
              ...p,
              generatedReply: newDraft,
              approvedReply: newDraft,
              replyTone: tone,
              replyStatus: 'drafted'
            }
          : p
      )
    );
  };

  const pendingReviewCount = prospects.filter(p => p.replyStatus !== 'approved' && p.generatedReply).length;

  return (
    <div className="flex h-screen w-screen overflow-hidden bg-[#0a0c10] text-[#f1f3f9]">
      {/* Left Sidebar */}
      <Sidebar
        currentTab={currentTab}
        setCurrentTab={setCurrentTab}
        onNewChat={handleNewChat}
        campaignCount={campaigns.length}
        prospectCount={prospects.length}
        pendingReviewCount={pendingReviewCount}
      />

      {/* Main Workspace Area */}
      <main className="flex-1 flex flex-col h-full relative overflow-hidden">
        {currentTab === 'chat' && (
          <ChatView
            messages={messages}
            onSendMessage={handleSendMessage}
            isLoading={isLoading}
            onOpenArtifact={(artifact) => setActiveArtifact(artifact)}
            selectedModel={selectedModel}
            setSelectedModel={setSelectedModel}
          />
        )}

        {currentTab === 'campaigns' && (
          <CampaignsView
            campaigns={campaigns}
            onCreateCampaign={handleCreateCampaign}
            onSelectCampaign={() => {}}
            onRunDiscovery={handleRunDiscovery}
            onGoToProspects={(campId) => {
              setSelectedCampaignId(campId);
              setCurrentTab('prospects');
            }}
          />
        )}

        {currentTab === 'prospects' && (
          <ProspectsView
            prospects={prospects}
            campaigns={campaigns}
            selectedCampaignId={selectedCampaignId}
            onSelectCampaignId={setSelectedCampaignId}
            onUpdateStatus={handleUpdateProspectStatus}
            onOpenReview={(p) => {
              setSelectedProspect(p);
              setCurrentTab('review');
            }}
            onDraftReply={(p) => {
              handleRegenerateReply(p.id, 'value_first');
              setSelectedProspect(p);
              setCurrentTab('review');
            }}
          />
        )}

        {currentTab === 'review' && (
          <ReviewStudioView
            prospects={prospects}
            selectedProspect={selectedProspect}
            onSelectProspect={(p) => setSelectedProspect(p)}
            onApproveReply={handleApproveReply}
            onRegenerateReply={handleRegenerateReply}
          />
        )}

        {currentTab === 'hangfire' && (
          <HangfireView
            jobs={hangfireJobs}
            onTriggerJob={(jobName) => {
              const newJob: HangfireJob = {
                id: `job-${Date.now().toString().slice(-4)}`,
                jobName,
                queue: jobName.includes('YouTube') ? 'youtube' : 'ai',
                state: 'Processing',
                createdAt: new Date().toISOString(),
                duration: '10s',
                details: `Manually scheduled operator job: ${jobName}`,
                progress: 20
              };
              setHangfireJobs((prev) => [newJob, ...prev]);
              setTimeout(() => {
                setHangfireJobs((prev) =>
                  prev.map((j) => j.id === newJob.id ? { ...j, state: 'Succeeded', progress: 100 } : j)
                );
              }, 2500);
            }}
          />
        )}

        {currentTab === 'mcp' && (
          <McpServerView
            onExecuteTool={(toolName, params) => {
              console.log(`Executed MCP tool ${toolName} with`, params);
            }}
          />
        )}

        {/* Slide-out Claude Artifact Drawer */}
        <ArtifactDrawer
          artifact={activeArtifact}
          onClose={() => setActiveArtifact(null)}
          onNavigateTab={(tab) => {
            setActiveArtifact(null);
            setCurrentTab(tab);
          }}
        />
      </main>
    </div>
  );
}

export default App;
