import type { Prospect } from '../types';
import IntentScore from './IntentScore';

interface Props {
  prospect: Prospect;
  onStatusChange?: (id: number, status: string) => void;
}

export default function ProspectCard({ prospect, onStatusChange }: Props) {
  return (
    <div className="prospect-card">
      <div className="prospect-head">
        <IntentScore score={prospect.intentScore} />
        <div>
          <strong>{prospect.authorName}</strong>
          <span className="muted"> · {prospect.videoTitle}</span>
        </div>
        <span className={`status status-${prospect.status.toLowerCase()}`}>{prospect.status}</span>
      </div>
      <blockquote>{prospect.commentText}</blockquote>
      <p className="pain"><strong>Pain point:</strong> {prospect.painPoint}</p>
      <p className="muted reason">{prospect.aiReason}</p>
      {onStatusChange && (
        <div className="prospect-actions">
          {['Qualified', 'Contacted', 'Interested', 'Rejected'].map((s) => (
            <button key={s} onClick={() => onStatusChange(prospect.id, s)}>{s}</button>
          ))}
        </div>
      )}
    </div>
  );
}
