export interface Campaign {
  id: string;
  name: string;
  productName: string;
  description: string;
  targetAudience: string;
  problemStatement: string;
  keywords: string[];
  aiInstructions: string;
  status: 'active' | 'paused' | 'completed';
  createdAt: string;
  stats: {
    videosScanned: number;
    commentsAnalyzed: number;
    prospectsFound: number;
    qualifiedCount: number;
    averageIntentScore: number;
  };
}

export type ProspectStatus = 'new' | 'contacted' | 'interested' | 'converted' | 'rejected';
export type ReplyStatus = 'none' | 'drafted' | 'approved' | 'published';

export interface Prospect {
  id: string;
  campaignId: string;
  campaignName: string;
  authorName: string;
  authorHandle: string;
  authorAvatar?: string;
  authorChannelUrl: string;
  videoId: string;
  videoTitle: string;
  videoUrl: string;
  channelTitle: string;
  commentId: string;
  commentText: string;
  commentPublishedAt: string;
  intentScore: number; // 0 - 100
  isTargetAudience: boolean;
  hasRelevantProblem: boolean;
  painPoint: string;
  qualificationReason: string;
  status: ProspectStatus;
  replyStatus: ReplyStatus;
  generatedReply?: string;
  approvedReply?: string;
  replyTone?: 'value_first' | 'helpful_advisor' | 'direct_solution';
  notes?: string;
}

export interface ToolCallItem {
  id: string;
  toolName: 'promote' | 'find_prospects' | 'get_prospect' | 'generate_reply' | 'update_prospect_status' | 'get_campaign' | 'search_youtube';
  parameters: Record<string, any>;
  result?: any;
  status: 'calling' | 'completed' | 'failed';
  summary?: string;
}

export interface ChatArtifact {
  id: string;
  title: string;
  type: 'campaign' | 'prospects' | 'reply_studio' | 'hangfire';
  data: any;
}

export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: string;
  toolCalls?: ToolCallItem[];
  artifact?: ChatArtifact;
  modelUsed?: string;
}

export interface HangfireJob {
  id: string;
  jobName: string;
  queue: 'youtube' | 'ai' | 'notifications' | 'maintenance';
  state: 'Enqueued' | 'Processing' | 'Succeeded' | 'Failed';
  createdAt: string;
  duration?: string;
  details: string;
  progress?: number;
}

export interface McpToolDefinition {
  name: string;
  purpose: string;
  description: string;
  milestone: string;
  parameters: {
    name: string;
    type: string;
    required: boolean;
    description: string;
  }[];
  sampleExecution: {
    request: Record<string, any>;
    response: Record<string, any>;
  };
}
