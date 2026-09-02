import { useEffect, useState } from 'react';
import { campaignService } from '../services/campaignService';
import { prospectService } from '../services/prospectService';
import CampaignCard from '../components/CampaignCard';
import type { Campaign, CampaignStats, GlobalStats } from '../types';

export default function Dashboard() {
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [stats, setStats] = useState<Record<number, CampaignStats>>({});
  const [global, setGlobal] = useState<GlobalStats | null>(null);

  useEffect(() => {
    campaignService.getAll().then(async (list) => {
      setCampaigns(list);
      const entries = await Promise.all(
        list.map(async (c) => [c.id, await campaignService.getStats(c.id)] as const)
      );
      setStats(Object.fromEntries(entries));
    });
    prospectService.getGlobalStats().then(setGlobal);
  }, []);

  return (
    <div>
      <h1>Dashboard</h1>
      {global && (
        <div className="global-stats">
          <div><span>TOTAL PROSPECTS</span><strong>{global.totalProspects.toLocaleString()}</strong></div>
          <div><span>HIGH INTENT</span><strong>{global.highIntent}</strong></div>
          <div><span>CONTACTED</span><strong>{global.contacted}</strong></div>
          <div><span>INTERESTED</span><strong>{global.interested}</strong></div>
          <div><span>CONVERTED</span><strong>{global.converted}</strong></div>
        </div>
      )}
      <h2>Campaigns</h2>
      <div className="card-grid">
        {campaigns.map((c) => (
          <CampaignCard key={c.id} campaign={c} stats={stats[c.id]} />
        ))}
      </div>
    </div>
  );
}
