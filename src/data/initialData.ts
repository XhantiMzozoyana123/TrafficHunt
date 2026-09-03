import { Campaign, Prospect, HangfireJob, McpToolDefinition } from '../types';

export const INITIAL_CAMPAIGNS: Campaign[] = [
  {
    id: 'camp-1',
    name: 'TubeMail Gorilla - Video Editors Outreach',
    productName: 'TubeMail Gorilla',
    description: 'Video outreach & client prospecting tool tailored for freelance video editors seeking high-ticket YouTube clients.',
    targetAudience: 'Freelance video editors, Premiere Pro/DaVinci Resolve creators, thumbnail designers, YouTube production assistants',
    problemStatement: 'Struggling to land consistent editing clients, sending cold DMs that get ignored, finding channels that actually have editing budgets.',
    keywords: [
      'how to get video editing clients 2026',
      'cold email youtube creators freelance',
      'freelance video editor portfolio tips',
      'video editing client outreach strategy',
      'getting first video editing gig on youtube'
    ],
    aiInstructions: 'Identify editors expressing frustration with client acquisition. Disqualify established agencies or viewers merely asking for editing software tutorials. Look for high intent: "I need clients", "cold outreach isn\'t working", "how do you reach out to big YouTubers".',
    status: 'active',
    createdAt: '2026-08-28T14:20:00Z',
    stats: {
      videosScanned: 24,
      commentsAnalyzed: 412,
      prospectsFound: 14,
      qualifiedCount: 9,
      averageIntentScore: 84
    }
  },
  {
    id: 'camp-2',
    name: 'ClipFlow - AI Repurposing SaaS',
    productName: 'ClipFlow AI',
    description: 'Automated long-form podcast & webinar to viral YouTube Shorts and TikTok repurposing workflow.',
    targetAudience: 'Podcast hosts, solopreneurs, course creators, business interviewers producing 30min+ horizontal videos',
    problemStatement: 'Spending 8+ hours chopping long videos into shorts, lack of retention hooks, subtitles formatting taking too long.',
    keywords: [
      'how to repurpose podcast for shorts',
      'automate youtube shorts from podcast',
      'best ai video editor for clips 2026',
      'long form to short form workflow'
    ],
    aiInstructions: 'Look for creators overwhelmed by short-form production volume or complaining about the manual labor of subtitle alignment and cutdowns.',
    status: 'active',
    createdAt: '2026-09-01T09:15:00Z',
    stats: {
      videosScanned: 18,
      commentsAnalyzed: 285,
      prospectsFound: 8,
      qualifiedCount: 6,
      averageIntentScore: 78
    }
  }
];

export const INITIAL_PROSPECTS: Prospect[] = [
  {
    id: 'prospect-1',
    campaignId: 'camp-1',
    campaignName: 'TubeMail Gorilla - Video Editors Outreach',
    authorName: 'Alex Thorne (Cuts & Color)',
    authorHandle: '@alex_edits_nyc',
    authorChannelUrl: 'https://youtube.com/@alex_edits_nyc',
    videoId: 'vid_yt_101',
    videoTitle: 'How I Got My First 5 Video Editing Clients on YouTube (Cold Outreach Guide)',
    videoUrl: 'https://youtube.com/watch?v=vid_yt_101',
    channelTitle: 'Creator Freelancing Hub',
    commentId: 'cmt_001',
    commentText: "Man I've sent over 80 personalized email pitches to gaming and tech YouTubers this month with a free sample cut and got zero responses. Does anyone have a better way to find creators who are actively hiring and actually check their business email?",
    commentPublishedAt: '2 days ago',
    intentScore: 95,
    isTargetAudience: true,
    hasRelevantProblem: true,
    painPoint: 'Sent 80+ cold pitches with zero replies; cannot identify channels with active hiring budgets or unmonitored inboxes.',
    qualificationReason: 'High intent: actively sending pitches right now, experiencing high rejection/silence, explicitly seeking a better prospect discovery system.',
    status: 'new',
    replyStatus: 'drafted',
    replyTone: 'value_first',
    generatedReply: "@alex_edits_nyc Hey Alex, 80 emails with zero replies usually points to two common pitfalls: targeting creator inboxes that are inundated with spam filters, or pitching channels whose upload velocity doesn't demand external editing help yet. When we analyze creator channels, we filter for creators publishing 3+ long-form videos weekly whose editing bottleneck is evident in pacing drops. If you want a quick diagnostic on your outreach angle or a list of 10 channels with verified high-demand editing bottlenecks, let me know — happy to share our operator checklist!",
    approvedReply: "@alex_edits_nyc Hey Alex, 80 emails with zero replies usually points to two common pitfalls: targeting creator inboxes that are inundated with spam filters, or pitching channels whose upload velocity doesn't demand external editing help yet. When we analyze creator channels, we filter for creators publishing 3+ long-form videos weekly whose editing bottleneck is evident in pacing drops. If you want a quick diagnostic on your outreach angle or a list of 10 channels with verified high-demand editing bottlenecks, let me know — happy to share our operator checklist!",
    notes: 'Very high intent. Follow up if no reply within 48h after YouTube comment approval.'
  },
  {
    id: 'prospect-2',
    campaignId: 'camp-1',
    campaignName: 'TubeMail Gorilla - Video Editors Outreach',
    authorName: 'Liam Carter VFX',
    authorHandle: '@liamcarterfilms',
    authorChannelUrl: 'https://youtube.com/@liamcarterfilms',
    videoId: 'vid_yt_102',
    videoTitle: 'Why 90% of Freelance Video Editors Are Broke in 2026',
    videoUrl: 'https://youtube.com/watch?v=vid_yt_102',
    channelTitle: 'Post Production Mastery',
    commentId: 'cmt_002',
    commentText: "The hardest part isn't the editing skills, it's finding which YouTubers aren't already swarmed by 50 editors in their DMs every morning. I feel like by the time a creator hits 100k subs they never even look at DMs anymore.",
    commentPublishedAt: '3 days ago',
    intentScore: 89,
    isTargetAudience: true,
    hasRelevantProblem: true,
    painPoint: 'Market saturation in direct messages; unable to bypass creator gatekeepers or identify mid-tier creators.',
    qualificationReason: 'Recognizes that outreach timing and discovery is his critical bottleneck rather than technical editing ability.',
    status: 'contacted',
    replyStatus: 'published',
    replyTone: 'helpful_advisor',
    generatedReply: "@liamcarterfilms Spot on Liam. Creators above 100k subs usually route outreach through agents or auto-archive unsolicited portfolio links. The sweet spot we've observed is 20k-60k channels ramping up sponsorship obligations where the creator is still editing until 3am. Reaching them right after an apology about delayed uploads yields a 4x response rate.",
    approvedReply: "@liamcarterfilms Spot on Liam. Creators above 100k subs usually route outreach through agents or auto-archive unsolicited portfolio links. The sweet spot we've observed is 20k-60k channels ramping up sponsorship obligations where the creator is still editing until 3am. Reaching them right after an apology about delayed uploads yields a 4x response rate.",
    notes: 'Sent via approved YouTube OAuth path. Prospect liked the comment.'
  },
  {
    id: 'prospect-3',
    campaignId: 'camp-1',
    campaignName: 'TubeMail Gorilla - Video Editors Outreach',
    authorName: 'Elena Rostova (Montage Studio)',
    authorHandle: '@elenacuts',
    authorChannelUrl: 'https://youtube.com/@elenacuts',
    videoId: 'vid_yt_101',
    videoTitle: 'How I Got My First 5 Video Editing Clients on YouTube (Cold Outreach Guide)',
    videoUrl: 'https://youtube.com/watch?v=vid_yt_101',
    channelTitle: 'Creator Freelancing Hub',
    commentId: 'cmt_003',
    commentText: "Is there a tool or database that tracks when channels start ramping up posting frequency? I waste hours manually clicking through channel 'About' tabs trying to find business emails.",
    commentPublishedAt: '1 day ago',
    intentScore: 92,
    isTargetAudience: true,
    hasRelevantProblem: true,
    painPoint: 'Wastes hours on manual channel prospecting and metadata collection; specifically asked for automated tool/database.',
    qualificationReason: 'Direct buying signal: explicitly asking "Is there a tool or database". Perfect fit for TubeMail Gorilla discovery engine.',
    status: 'new',
    replyStatus: 'drafted',
    replyTone: 'direct_solution',
    generatedReply: "@elenacuts Elena, that manual About-tab scraping is exactly what drains 80% of an editor's prospecting hours. We built an automated scanner called TubeMail Gorilla specifically for this — it passively indexes YouTube channels searching for creator upload spikes, extracts verified contact paths, and filters out inactive channels. Let me know if you want early operator access to test it on your niche!",
    approvedReply: "@elenacuts Elena, that manual About-tab scraping is exactly what drains 80% of an editor's prospecting hours. We built an automated scanner called TubeMail Gorilla specifically for this — it passively indexes YouTube channels searching for creator upload spikes, extracts verified contact paths, and filters out inactive channels. Let me know if you want early operator access to test it on your niche!"
  },
  {
    id: 'prospect-4',
    campaignId: 'camp-2',
    campaignName: 'ClipFlow - AI Repurposing SaaS',
    authorName: 'Marcus Wright (Founders Lounge Podcast)',
    authorHandle: '@marcuspodcasting',
    authorChannelUrl: 'https://youtube.com/@marcuspodcasting',
    videoId: 'vid_yt_201',
    videoTitle: 'Podcast Growth Strategies: Turning 1 Episode into 15 Viral Shorts',
    videoUrl: 'https://youtube.com/watch?v=vid_yt_201',
    channelTitle: 'SaaS Founder Blueprint',
    commentId: 'cmt_004',
    commentText: "We record a 60-minute interview weekly with B2B founders, but extracting 10 digestible hooks and getting dynamic captions styled takes my VA two full workdays. Opus and CapCut keep chopping speakers mid-sentence.",
    commentPublishedAt: '4 days ago',
    intentScore: 88,
    isTargetAudience: true,
    hasRelevantProblem: true,
    painPoint: 'VA spends 16+ hours editing shorts; existing AI tools (Opus, CapCut) cut speakers off mid-sentence.',
    qualificationReason: 'High willingness to pay, active production bottleneck, specific complaints with competing tools.',
    status: 'new',
    replyStatus: 'none',
    generatedReply: ''
  }
];

export const INITIAL_HANGFIRE_JOBS: HangfireJob[] = [
  {
    id: 'job-8492',
    jobName: 'YouTubeDiscoveryJob',
    queue: 'youtube',
    state: 'Processing',
    createdAt: '2026-09-03T01:48:10Z',
    duration: '42s',
    details: 'Searching YoutubeExplode for "freelance video editor portfolio tips" (Page 2/4)',
    progress: 65
  },
  {
    id: 'job-8491',
    jobName: 'CommentAnalysisJob',
    queue: 'ai',
    state: 'Processing',
    createdAt: '2026-09-03T01:48:22Z',
    duration: '18s',
    details: 'Ollama Llama 3.1 analyzing 15 comment threads on video vid_yt_104 for pain point extraction',
    progress: 40
  },
  {
    id: 'job-8490',
    jobName: 'CommentImportJob',
    queue: 'youtube',
    state: 'Succeeded',
    createdAt: '2026-09-03T01:46:00Z',
    duration: '12s',
    details: 'Fetched 50 comment threads from YouTube Data API v3 (Quota used: 1 unit)',
    progress: 100
  },
  {
    id: 'job-8489',
    jobName: 'OpportunityDetectionJob',
    queue: 'ai',
    state: 'Succeeded',
    createdAt: '2026-09-03T01:44:30Z',
    duration: '8s',
    details: 'Detected 3 high-intent prospects with intent score >= 85 in TubeMail Gorilla campaign',
    progress: 100
  },
  {
    id: 'job-8488',
    jobName: 'ChannelMonitoringJob',
    queue: 'youtube',
    state: 'Succeeded',
    createdAt: '2026-09-03T01:40:00Z',
    duration: '5s',
    details: 'Recurring monitor channel-UC_creatorHub: 1 new video detected',
    progress: 100
  },
  {
    id: 'job-8487',
    jobName: 'NotificationJob',
    queue: 'notifications',
    state: 'Succeeded',
    createdAt: '2026-09-03T01:38:00Z',
    duration: '1s',
    details: 'Desktop notification sent to operator for prospect Alex Thorne (Score: 95)',
    progress: 100
  },
  {
    id: 'job-8486',
    jobName: 'MaintenanceJob',
    queue: 'maintenance',
    state: 'Succeeded',
    createdAt: '2026-09-03T01:00:00Z',
    duration: '3s',
    details: 'Purged raw comment cache older than 14 days and recalculated campaign aggregates',
    progress: 100
  }
];

export const MCP_TOOLS_REGISTRY: McpToolDefinition[] = [
  {
    name: 'promote',
    purpose: 'Primary entry point for the TrafficHunt operator.',
    description: 'Provide a plain-English description of what you are promoting. The AI generates the campaign (audience, problems, search keywords), runs passive discovery, imports comments, analyzes intent, and returns ranked prospects.',
    milestone: 'Milestone 1 & 3',
    parameters: [
      { name: 'description', type: 'string', required: true, description: 'Plain English pitch of the product or service being promoted' },
      { name: 'max_prospects', type: 'number', required: false, description: 'Target number of high-intent prospects to return (default: 10)' },
      { name: 'min_intent_score', type: 'number', required: false, description: 'Minimum qualification threshold between 0 and 100 (default: 75)' }
    ],
    sampleExecution: {
      request: {
        description: 'A video outreach tool for freelance video editors struggling to find clients on YouTube',
        min_intent_score: 80
      },
      response: {
        campaign_id: 'camp-1',
        campaign_name: 'TubeMail Gorilla - Video Editors Outreach',
        keywords_generated: [
          'how to get video editing clients 2026',
          'cold email youtube creators freelance'
        ],
        prospects_found: 3,
        top_prospects: [
          { author: '@alex_edits_nyc', intent_score: 95, pain_point: 'Sent 80+ cold pitches with zero replies' }
        ]
      }
    }
  },
  {
    name: 'find_prospects',
    purpose: 'List qualified prospects for an existing campaign.',
    description: 'Retrieve prospects matching minimum intent score and workflow status filter.',
    milestone: 'Milestone 1',
    parameters: [
      { name: 'campaign_id', type: 'string', required: true, description: 'ID of the campaign to query' },
      { name: 'min_intent_score', type: 'number', required: false, description: 'Lower bound intent score (0-100)' },
      { name: 'status', type: 'string', required: false, description: 'Filter by pipeline status (new, contacted, interested, converted, rejected)' }
    ],
    sampleExecution: {
      request: { campaign_id: 'camp-1', min_intent_score: 85, status: 'new' },
      response: { count: 2, prospects: ['prospect-1', 'prospect-3'] }
    }
  },
  {
    name: 'get_prospect',
    purpose: 'Get comprehensive details of a single prospect.',
    description: 'Returns author information, video context, original comment, AI reasoning, pain point, and reply history.',
    milestone: 'Milestone 1',
    parameters: [
      { name: 'prospect_id', type: 'string', required: true, description: 'ID of the prospect record' }
    ],
    sampleExecution: {
      request: { prospect_id: 'prospect-1' },
      response: {
        id: 'prospect-1',
        author: 'Alex Thorne',
        comment: "Man I've sent over 80 personalized email pitches...",
        intent_score: 95,
        status: 'new'
      }
    }
  },
  {
    name: 'generate_reply',
    purpose: 'Generate personalized outreach reply for human approval.',
    description: 'Uses campaign messaging rules and comment context to draft a value-first, non-spammy YouTube comment reply.',
    milestone: 'Milestone 2',
    parameters: [
      { name: 'prospect_id', type: 'string', required: true, description: 'ID of the target prospect' },
      { name: 'tone', type: 'string', required: false, description: 'Tone variation: value_first, helpful_advisor, direct_solution' }
    ],
    sampleExecution: {
      request: { prospect_id: 'prospect-1', tone: 'value_first' },
      response: {
        reply_draft: "@alex_edits_nyc Hey Alex, 80 emails with zero replies usually points to two common pitfalls...",
        requires_human_approval: true
      }
    }
  },
  {
    name: 'update_prospect_status',
    purpose: 'Advance a prospect through the sales outreach pipeline.',
    description: 'Move prospect state between new, contacted, interested, converted, or rejected.',
    milestone: 'Milestone 1',
    parameters: [
      { name: 'prospect_id', type: 'string', required: true, description: 'ID of the prospect' },
      { name: 'status', type: 'string', required: true, description: 'Target status' }
    ],
    sampleExecution: {
      request: { prospect_id: 'prospect-1', status: 'contacted' },
      response: { success: true, updated_status: 'contacted' }
    }
  },
  {
    name: 'get_campaign',
    purpose: 'Retrieve campaign configuration and current metrics.',
    description: 'Returns audience definition, keywords, scanned video stats, and qualification funnel numbers.',
    milestone: 'Milestone 1',
    parameters: [
      { name: 'campaign_id', type: 'string', required: true, description: 'Campaign ID' }
    ],
    sampleExecution: {
      request: { campaign_id: 'camp-1' },
      response: {
        id: 'camp-1',
        name: 'TubeMail Gorilla - Video Editors Outreach',
        stats: { videosScanned: 24, qualifiedCount: 9 }
      }
    }
  }
];
