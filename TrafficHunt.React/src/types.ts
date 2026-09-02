export interface CampaignProblem {
  id: number;
  text: string;
}

export interface CampaignKeyword {
  id: number;
  keyword: string;
  active: boolean;
}

export interface Campaign {
  id: number;
  name: string;
  productName: string;
  productDescription: string;
  productUrl: string;
  valueProposition: string;
  targetAudience: string;
  primaryProblem: string;
  problems: CampaignProblem[];
  keywords: CampaignKeyword[];
  qualificationRules: string;
  outreachInstructions: string;
  replyInstructions: string;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export interface CampaignStats {
  totalProspects: number;
  highIntent: number;
  contacted: number;
  interested: number;
  converted: number;
}

export interface Prospect {
  id: number;
  campaignId: number;
  commentId: string;
  videoId: string;
  videoTitle: string;
  authorName: string;
  youTubeChannelId: string;
  youTubeProfileUrl: string;
  commentText: string;
  isTargetAudience: boolean;
  hasRelevantProblem: boolean;
  intentScore: number;
  painPoint: string;
  aiReason: string;
  status: string;
  createdAt: string;
  lastContactedAt: string | null;
}

export interface GlobalStats {
  totalProspects: number;
  highIntent: number;
  contacted: number;
  interested: number;
  converted: number;
}

export interface DiscoveryProgress {
  campaignId: number;
  currentKeyword: string;
  keywordsProcessed: number;
  totalKeywords: number;
  videosScanned: number;
  commentsAnalyzed: number;
  prospectsFound: number;
  isComplete: boolean;
}
