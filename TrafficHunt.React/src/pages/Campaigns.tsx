import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { campaignService } from '../services/campaignService';
import type { Campaign } from '../types';

const blank: Partial<Campaign> = {
  name: '', productName: '', productUrl: '', productDescription: '',
  valueProposition: '', targetAudience: '', primaryProblem: '', qualificationRules: ''
};

export default function Campaigns() {
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [form, setForm] = useState<Partial<Campaign>>(blank);
  const navigate = useNavigate();

  useEffect(() => {
    campaignService.getAll().then(setCampaigns);
  }, []);

  async function create() {
    const campaign = await campaignService.create(form as Campaign);
    navigate(`/campaigns/${campaign.id}`);
  }

  return (
    <div>
      <h1>Campaigns</h1>
      <div className="form">
        <h2>New campaign</h2>
        <input placeholder="Campaign name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
        <input placeholder="Product name" value={form.productName} onChange={(e) => setForm({ ...form, productName: e.target.value })} />
        <input placeholder="Product URL" value={form.productUrl} onChange={(e) => setForm({ ...form, productUrl: e.target.value })} />
        <textarea placeholder="Product description" value={form.productDescription} onChange={(e) => setForm({ ...form, productDescription: e.target.value })} />
        <input placeholder="Target audience" value={form.targetAudience} onChange={(e) => setForm({ ...form, targetAudience: e.target.value })} />
        <input placeholder="Primary problem solved" value={form.primaryProblem} onChange={(e) => setForm({ ...form, primaryProblem: e.target.value })} />
        <textarea placeholder="Qualification rules for the AI" value={form.qualificationRules} onChange={(e) => setForm({ ...form, qualificationRules: e.target.value })} />
        <button onClick={create}>Create campaign</button>
      </div>
      <ul className="simple-list">
        {campaigns.map((c) => (
          <li key={c.id}>
            <button className="link" onClick={() => navigate(`/campaigns/${c.id}`)}>{c.name}</button>
          </li>
        ))}
      </ul>
    </div>
  );
}
