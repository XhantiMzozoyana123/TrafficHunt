import { useEffect, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { campaignService } from '../services/campaignService';
import { prospectService } from '../services/prospectService';
import ProspectCard from '../components/ProspectCard';
import type { Campaign, CampaignStats, Prospect } from '../types';

export default function CampaignDetails() {
  const { id } = useParams();
  const campaignId = Number(id);
  const [campaign, setCampaign] = useState<Campaign | null>(null);
  const [stats, setStats] = useState<CampaignStats | null>(null);
  const [prospects, setProspects] = useState<Prospect[]>([]);
  const [keyword, setKeyword] = useState('');
  const [running, setRunning] = useState(false);
  const [log, setLog] = useState<string>('');
  const minIntent = useRef<HTMLInputElement>(null);

  function load() {
    campaignService.getById(campaignId).then(setCampaign);
    campaignService.getStats(campaignId).then(setStats);
    prospectService.getByCampaign(campaignId).then(setProspects);
  }

  useEffect(load, [campaignId]);

  function runDiscovery() {
    setRunning(true);
    setLog('Starting discovery...');
    campaignService.runDiscovery(
      campaignId,
      (kw, videos, comments, found, done) => {
        setLog(`Keyword "${kw}" · videos: ${videos} · comments analyzed: ${comments} · prospects: ${found}${done ? ' · DONE' : ''}`);
        if (done) {
          setRunning(false);
          load();
        }
      }
    );
  }

  async function changeStatus(prospectId: number, status: string) {
    await prospectService.updateStatus(prospectId, status);
    prospectService.getByCampaign(campaignId).then(setProspects);
    campaignService.getStats(campaignId).then(setStats);
  }

  function filter() {
    const min = minIntent.current?.value ? Number(minIntent.current.value) : undefined;
    prospectService.getByCampaign(campaignId, undefined, min).then(setProspects);
  }

  if (!campaign) return <div>Loading…</div>;

  return (
    <div>
      <h1>{campaign.name}</h1>
      <p className="muted">Target: {campaign.targetAudience} · Problem: {campaign.primaryProblem}</p>

      {stats && (
        <div className="global-stats">
          <div><span>PROSPECTS</span><strong>{stats.totalProspects}</strong></div>
          <div><span>HIGH INTENT</span><strong>{stats.highIntent}</strong></div>
          <div><span>CONTACTED</span><strong>{stats.contacted}</strong></div>
          <div><span>INTERESTED</span><strong>{stats.interested}</strong></div>
          <div><span>CONVERTED</span><strong>{stats.converted}</strong></div>
        </div>
      )}

      <div className="panel">
        <h2>Keywords</h2>
        <ul className="keywords">
          {campaign.keywords.map((k) => (
            <li key={k.id}>
              {k.keyword}{' '}
              <button className="x" onClick={() => campaignService.removeKeyword(k.id).then(load)}>×</button>
            </li>
          ))}
        </ul>
        <div className="row">
          <input
            placeholder="Add discovery keyword"
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
          />
          <button onClick={async () => { await campaignService.addKeyword(campaignId, keyword); setKeyword(''); load(); }}>
            Add
          </button>
        </div>
      </div>

      <div className="panel">
        <h2>Discovery</h2>
        <button onClick={runDiscovery} disabled={running || campaign.keywords.length === 0}>
          {running ? 'Hunting…' : 'Find More Prospects'}
        </button>
        {log && <p className="muted">{log}</p>}
      </div>

      <div className="panel">
        <h2>Prospects</h2>
        <div className="row">
          <input ref={minIntent} type="number" placeholder="Min intent score" />
          <button onClick={filter}>Filter</button>
        </div>
        {prospects.map((p) => (
          <ProspectCard key={p.id} prospect={p} onStatusChange={changeStatus} />
        ))}
      </div>
    </div>
  );
}
