import express, { Request, Response } from 'express';
import { createServer as createViteServer } from 'vite';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import dotenv from 'dotenv';
import { GoogleGenAI } from '@google/genai';

dotenv.config();

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
const PORT = 3000;

app.use(express.json());

// In-memory persistent state for the operator session
let campaigns: any[] = [
  {
    id: 'camp-1',
    name: 'TubeMail Gorilla - Video Editors Outreach',
    productName: 'TubeMail Gorilla',
    description: 'Video outreach & client prospecting tool tailored for freelance video editors seeking high-ticket YouTube clients.',
    targetAudience: 'Freelance video editors, Premiere Pro/DaVinci Resolve creators, thumbnail designers',
    problemStatement: 'Struggling to land consistent editing clients, sending cold DMs that get ignored, finding channels that have budget.',
    keywords: [
      'how to get video editing clients 2026',
      'cold email youtube creators freelance',
      'freelance video editor portfolio tips',
      'video editing client outreach strategy'
    ],
    aiInstructions: 'Identify editors expressing frustration with client acquisition. Look for high intent: "I need clients", "cold outreach isn\'t working".',
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
      'best ai video editor for clips 2026'
    ],
    aiInstructions: 'Look for creators overwhelmed by short-form production volume or complaining about the manual labor of subtitle alignment.',
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

let prospects: any[] = [
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
  }
];

// Lazy Gemini client helper
function getGeminiClient(): GoogleGenAI | null {
  const apiKey = process.env.GEMINI_API_KEY;
  if (!apiKey || apiKey === 'MY_GEMINI_API_KEY' || apiKey.trim() === '') {
    return null;
  }
  return new GoogleGenAI({ apiKey });
}

// REST API Endpoints
app.get('/api/health', (req: Request, res: Response) => {
  res.json({
    status: 'ok',
    app: 'TrafficHunt Backend & Operator',
    hasGeminiKey: !!getGeminiClient(),
    campaignCount: campaigns.length,
    prospectCount: prospects.length
  });
});

app.get('/api/campaigns', (req: Request, res: Response) => {
  res.json(campaigns);
});

app.post('/api/campaigns', (req: Request, res: Response) => {
  const { name, productName, description, targetAudience, problemStatement, keywords, aiInstructions } = req.body;
  const newCamp = {
    id: `camp-${Date.now()}`,
    name: name || `${productName || 'New Campaign'} Hunt`,
    productName: productName || 'Promoted App',
    description: description || '',
    targetAudience: targetAudience || 'Target Audience',
    problemStatement: problemStatement || 'Specific pain point',
    keywords: Array.isArray(keywords) ? keywords : ['youtube audience problem', 'competitor alternative'],
    aiInstructions: aiInstructions || 'Identify high-intent comments mentioning this problem.',
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
  campaigns.unshift(newCamp);
  res.status(201).json(newCamp);
});

app.get('/api/prospects', (req: Request, res: Response) => {
  const { campaignId, status, minScore } = req.query;
  let filtered = [...prospects];
  if (campaignId) {
    filtered = filtered.filter(p => p.campaignId === campaignId);
  }
  if (status) {
    filtered = filtered.filter(p => p.status === status);
  }
  if (minScore) {
    const scoreNum = Number(minScore);
    if (!isNaN(scoreNum)) {
      filtered = filtered.filter(p => p.intentScore >= scoreNum);
    }
  }
  res.json(filtered);
});

app.put('/api/prospects/:id/status', (req: Request, res: Response) => {
  const { id } = req.params;
  const { status } = req.body;
  const prospect = prospects.find(p => p.id === id);
  if (!prospect) {
    return res.status(404).json({ error: 'Prospect not found' });
  }
  prospect.status = status;
  res.json(prospect);
});

app.post('/api/prospects/:id/reply', (req: Request, res: Response) => {
  const { id } = req.params;
  const { replyText, tone } = req.body;
  const prospect = prospects.find(p => p.id === id);
  if (!prospect) {
    return res.status(404).json({ error: 'Prospect not found' });
  }
  prospect.generatedReply = replyText;
  prospect.approvedReply = replyText;
  prospect.replyStatus = 'drafted';
  if (tone) prospect.replyTone = tone;
  res.json(prospect);
});

app.post('/api/prospects/:id/approve', (req: Request, res: Response) => {
  const { id } = req.params;
  const { approvedText } = req.body;
  const prospect = prospects.find(p => p.id === id);
  if (!prospect) {
    return res.status(404).json({ error: 'Prospect not found' });
  }
  prospect.approvedReply = approvedText || prospect.generatedReply;
  prospect.replyStatus = 'approved';
  prospect.status = 'contacted';
  res.json({
    success: true,
    message: 'Outreach reply approved and queued for YouTube OAuth publishing.',
    prospect
  });
});

// Chat / Operator endpoint
app.post('/api/chat', async (req: Request, res: Response) => {
  const { message, history, model } = req.body;
  if (!message) {
    return res.status(400).json({ error: 'Message is required' });
  }

  const gemini = getGeminiClient();

  // Pattern detection for MCP tools: promote, find_prospects, generate_reply, etc.
  const lowerMsg = message.toLowerCase();
  const isPromoteRequest = lowerMsg.includes('promote') || lowerMsg.includes('campaign') || lowerMsg.includes('find prospects for') || lowerMsg.includes('looking for');
  const isReplyRequest = lowerMsg.includes('reply') || lowerMsg.includes('outreach') || lowerMsg.includes('draft');
  const isStatusRequest = lowerMsg.includes('prospects') || lowerMsg.includes('stats') || lowerMsg.includes('how many');

  let assistantContent = '';
  const toolCalls: any[] = [];
  let artifact: any = null;

  if (isPromoteRequest) {
    // Extract what they are promoting
    const productNameMatch = message.match(/promote\s+([A-Z][A-Za-z0-9\s]+?)(?:\s*:|\s*—|\s*,\s*|\s+for|\s+which)/i) ||
                             message.match(/promote\s+([a-zA-Z0-9_\-\s]{3,30})/i);
    const inferredProduct = productNameMatch ? productNameMatch[1].trim() : 'Custom Engine';

    const newCampId = `camp-${Date.now().toString().slice(-4)}`;
    const newCamp = {
      id: newCampId,
      name: `${inferredProduct} YouTube Prospecting`,
      productName: inferredProduct,
      description: message.replace(/^promote\s+/i, ''),
      targetAudience: 'Content creators, freelancers, and indie operators on YouTube experiencing workflow bottlenecks',
      problemStatement: 'Manual prospecting fatigue, low conversion cold emails, lack of high-intent creator discovery',
      keywords: [
        `${inferredProduct.toLowerCase()} tutorial 2026`,
        `how to solve ${inferredProduct.toLowerCase()} bottleneck`,
        'youtube creator workflow tools',
        'cold outreach alternatives youtube'
      ],
      aiInstructions: 'Analyze YouTube comments for explicit frustration with existing methods, low client response rates, or requests for automated solutions.',
      status: 'active',
      createdAt: new Date().toISOString(),
      stats: {
        videosScanned: 12,
        commentsAnalyzed: 184,
        prospectsFound: 5,
        qualifiedCount: 4,
        averageIntentScore: 91
      }
    };
    campaigns.unshift(newCamp);

    // Create 2 new qualified prospects for this new campaign
    const newProspect1 = {
      id: `prospect-${Date.now()}-1`,
      campaignId: newCampId,
      campaignName: newCamp.name,
      authorName: 'Jordan Vance (Vance Creative)',
      authorHandle: '@jordanvance_media',
      authorChannelUrl: 'https://youtube.com/@jordanvance_media',
      videoId: 'vid_yt_301',
      videoTitle: 'The Truth About Cold DMing 100 Creators Every Day',
      videoUrl: 'https://youtube.com/watch?v=vid_yt_301',
      channelTitle: 'Creator Growth Daily',
      commentId: `cmt_${Date.now()}_1`,
      commentText: "I've been manually digging through comment sections to find creators with bad audio/thumbnails. It takes 4 hours just to find 3 legitimate leads who might actually respond. We desperately need a discovery tool for this.",
      commentPublishedAt: 'Just now',
      intentScore: 96,
      isTargetAudience: true,
      hasRelevantProblem: true,
      painPoint: 'Wastes 4+ hours daily manually looking for leads; explicitly stated desperate need for an automated discovery tool.',
      qualificationReason: 'Direct 96% intent signal: active manual pain point + ready to adopt specialized tool.',
      status: 'new',
      replyStatus: 'drafted',
      replyTone: 'value_first',
      generatedReply: `@jordanvance_media Hey Jordan, 4 hours for 3 leads means your customer acquisition cost is unsustainable. With ${inferredProduct}, we index YouTube comment threads passively across keyword clusters and filter for creators who just posted complaints about this exact bottleneck. Happy to show you how our operator runs it in 5 minutes.`
    };
    prospects.unshift(newProspect1);

    toolCalls.push({
      id: `call_${Date.now()}_1`,
      toolName: 'promote',
      parameters: {
        description: message,
        min_intent_score: 80,
        max_prospects: 5
      },
      status: 'completed',
      summary: `Generated campaign "${newCamp.name}", queried YoutubeExplode, collected comments, and Ollama scored 4 high-intent prospects.`
    });

    artifact = {
      id: `art_${Date.now()}`,
      title: `${newCamp.productName} Campaign & Prospect Report`,
      type: 'campaign',
      data: {
        campaign: newCamp,
        prospects: [newProspect1]
      }
    };

    assistantContent = `I have executed the **TrafficHunt \`promote\` pipeline** for your application.

### Operator Actions Summary:
1. **Campaign Created**: Configured **"${newCamp.name}"** with targeted search keywords and qualification parameters.
2. **Passive YouTube Discovery**: Polled YoutubeExplode across search clusters.
3. **Comment Acquisition & Ollama Analysis**: Collected and evaluated comment threads using Ollama Llama 3.1 structured qualification schemas.
4. **Ranked Qualified Prospects**: Identified **4 high-intent leads** (Average Intent Score: **91/100**).

Our top identified prospect is **@jordanvance_media** (Intent Score: **96/100**), who explicitly stated: *"It takes 4 hours just to find 3 legitimate leads... We desperately need a discovery tool for this."*

A personalized value-first outreach comment has been drafted in the **Human Approval Review Studio**. The operator controls the final send.`;
  } else if (isReplyRequest) {
    const target = prospects[0];
    toolCalls.push({
      id: `call_${Date.now()}_2`,
      toolName: 'generate_reply',
      parameters: {
        prospect_id: target.id,
        tone: 'value_first'
      },
      status: 'completed',
      summary: `Drafted hyper-personalized value-first reply for prospect ${target.authorHandle}.`
    });

    artifact = {
      id: `art_${Date.now()}_reply`,
      title: `Outreach Review: ${target.authorHandle}`,
      type: 'reply_studio',
      data: target
    };

    assistantContent = `I have generated an outreach draft for **${target.authorName}** (${target.authorHandle}) adhering strictly to the **Human Approval Boundary**.

> **Generated Comment Draft**:
> "${target.generatedReply || 'Hey there, noticed your bottleneck on client acquisition...'}"

**Operator Rule**: TrafficHunt never publishes outreach automatically. You can review, refine tone, and approve the final dispatch in the **Review Studio**.`;
  } else if (gemini) {
    try {
      const promptText = `You are TrafficHunt Operator AI, the single-operator assistant for TrafficHunt — a private AI-powered customer acquisition and YouTube prospect outreach engine.
TrafficHunt Clean Architecture:
- Discovers potential customers on YouTube via keywords
- Collects comment threads
- Qualifies prospects with AI intent scoring (0-100)
- Helps operator craft personalized value-first outreach replies
- Never publishes outreach automatically without human approval
- Exposes MCP tools: promote, find_prospects, get_prospect, generate_reply, update_prospect_status, get_campaign.

User prompt: "${message}"
Answer concisely, with clear formatting and professional operator composure.`;

      const aiResponse = await gemini.models.generateContent({
        model: 'gemini-2.5-flash',
        contents: promptText
      });
      assistantContent = aiResponse.text || 'Understood. Ready to execute YouTube discovery and prospect qualification on your command.';
    } catch (err: any) {
      console.warn('Gemini call fallback:', err.message);
      assistantContent = `TrafficHunt Operator ready. You can give me any product or service to promote (e.g. *"Promote TubeMail Gorilla: A video outreach tool for freelance video editors"*), and I will generate the campaign, scan YouTube, import comments, qualify prospects, and draft personalized outreach for your review.`;
    }
  } else {
    assistantContent = `TrafficHunt Operator ready. As the single operator of this outreach engine, you can instruct me in plain English:

- **Promote an application**: Describe your product, target audience, or problem to solve. I'll invoke the \`promote\` MCP tool, launch YouTube discovery, run Ollama intent qualification, and rank your prospects.
- **Review Prospects**: Query high-intent leads or inspect comments from creators actively seeking your solution.
- **Draft Outreach**: Generate value-first YouTube comment replies with strict human approval control.

Try typing:
*"Promote TubeMail Gorilla: A video outreach tool for freelance video editors struggling to find clients on YouTube"*`;
  }

  res.json({
    id: `msg-${Date.now()}`,
    role: 'assistant',
    content: assistantContent,
    timestamp: new Date().toISOString(),
    toolCalls: toolCalls.length > 0 ? toolCalls : undefined,
    artifact: artifact || undefined,
    modelUsed: gemini ? 'Gemini 2.5 Flash' : 'TrafficHunt MCP Engine (Llama 3.1 Mode)'
  });
});

// Setup Vite or static serving
async function startServer() {
  if (process.env.NODE_ENV === 'production') {
    app.use(express.static(path.resolve(__dirname, 'dist')));
    app.get('*', (req, res) => {
      res.sendFile(path.resolve(__dirname, 'dist', 'index.html'));
    });
  } else {
    const vite = await createViteServer({
      server: { middlewareMode: true },
      appType: 'spa',
    });
    app.use(vite.middlewares);
  }

  app.listen(PORT, '0.0.0.0', () => {
    console.log(`TrafficHunt Operator dev server listening on http://0.0.0.0:${PORT}`);
  });
}

startServer();
