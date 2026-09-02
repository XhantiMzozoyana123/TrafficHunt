import { Link } from 'react-router-dom';
import type { Campaign, CampaignStats } from '../types';

interface Props {
  campaign: Campaign;
  stats?: CampaignStats;
}

export default function CampaignCard({ campaign, stats }: Props) {
  return (
    <Link to={`/campaigns/${campaign.id}`} className="campaign-card">
      <h3>{campaign.name}</h3>
      <p className="muted">{campaign.productName} → {campaign.targetAudience}</p>
      {stats && (
        <div className="card-stats">
          <span><strong>{stats.highIntent}</strong> high intent</span>
          <span><strong>{stats.totalProspects}</strong> prospects</span>
          <span><strong>{stats.contacted}</strong> contacted</span>
        </div>
      )}
    </Link>
  );
}
