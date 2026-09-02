import request from './api';
import type { Campaign, CampaignKeyword, CampaignStats } from '../types';

export const campaignService = {
  getAll: () => request<Campaign[]>('/campaigns'),

  getById: (id: number) => request<Campaign>(`/campaigns/${id}`),

  create: (campaign: Partial<Campaign>) =>
    request<Campaign>('/campaigns', { method: 'POST', body: JSON.stringify(campaign) }),

  update: (campaign: Campaign) =>
    request<Campaign>(`/campaigns/${campaign.id}`, { method: 'PUT', body: JSON.stringify(campaign) }),

  remove: (id: number) => request<void>(`/campaigns/${id}`, { method: 'DELETE' }),

  addKeyword: (campaignId: number, keyword: string) =>
    request<CampaignKeyword>(`/campaigns/${campaignId}/keywords`, {
      method: 'POST',
      body: JSON.stringify({ keyword })
    }),

  removeKeyword: (keywordId: number) =>
    request<void>(`/campaigns/keywords/${keywordId}`, { method: 'DELETE' }),

  getStats: (id: number) => request<CampaignStats>(`/campaigns/${id}/stats`),

  runDiscovery: (
    campaignId: number,
    onProgress: (keyword: string, videos: number, comments: number, found: number, done: boolean) => void,
    videosPerKeyword = 3,
    commentsPerVideo = 50
  ) => {
    fetch(`/api/discovery/${campaignId}/run`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ videosPerKeyword, commentsPerVideo })
    }).then(async (response) => {
      const reader = response.body?.getReader();
      if (!reader) return;
      const decoder = new TextDecoder();
      let buffer = '';
      for (;;) {
        const { done, value } = await reader.read();
        if (done) break;
        buffer += decoder.decode(value, { stream: true });
        const events = buffer.split('\n\n');
        buffer = events.pop() ?? '';
        for (const event of events) {
          const data = event.replace(/^data: /, '').trim();
          if (!data) continue;
          try {
            const p = JSON.parse(data);
            onProgress(p.currentKeyword, p.videosScanned, p.commentsAnalyzed, p.prospectsFound, p.isComplete);
          } catch {
            // ignore malformed chunk
          }
        }
      }
    });
  }
};
