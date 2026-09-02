import request from './api';
import type { Prospect, GlobalStats } from '../types';

export const prospectService = {
  getByCampaign: (campaignId: number, status?: string, minIntentScore?: number) => {
    const params = new URLSearchParams({ campaignId: String(campaignId) });
    if (status) params.set('status', status);
    if (minIntentScore !== undefined) params.set('minIntentScore', String(minIntentScore));
    return request<Prospect[]>(`/prospects?${params}`);
  },

  getById: (id: number) => request<Prospect>(`/prospects/${id}`),

  updateStatus: (id: number, status: string) =>
    request<Prospect>(`/prospects/${id}/status`, { method: 'PATCH', body: JSON.stringify({ status }) }),

  remove: (id: number) => request<void>(`/prospects/${id}`, { method: 'DELETE' }),

  getGlobalStats: () => request<GlobalStats>('/prospects/stats/global')
};
