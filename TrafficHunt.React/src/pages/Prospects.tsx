import { useEffect, useState } from 'react';
import { prospectService } from '../services/prospectService';
import ProspectCard from '../components/ProspectCard';
import type { Prospect } from '../types';

export default function Prospects() {
  const [prospects, setProspects] = useState<Prospect[]>([]);

  useEffect(() => {
    // All campaigns view is driven by selecting a campaign; placeholder shows empty state.
    prospectService.getGlobalStats();
  }, []);

  return (
    <div>
      <h1>Prospects</h1>
      <p className="muted">Prospects are organized per campaign — open a campaign to view its prospect list.</p>
      {prospects.map((p) => <ProspectCard key={p.id} prospect={p} />)}
    </div>
  );
}
