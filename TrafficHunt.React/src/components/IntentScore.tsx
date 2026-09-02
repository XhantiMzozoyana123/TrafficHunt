interface Props {
  score: number;
}

export default function IntentScore({ score }: Props) {
  const level = score >= 90 ? 'high' : score >= 70 ? 'medium' : 'low';
  return <span className={`intent intent-${level}`}>{score}</span>;
}
