import { useEffect, useRef } from 'react';

export function TranscriptPanel({ label, flag, lines }) {
  const bottomRef = useRef(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [lines]);

  return (
    <section className="panel" aria-label={label}>
      <h2 className="panel-header">
        <span aria-hidden="true">{flag}</span> {label}
      </h2>
      <div className="panel-text" role="log" aria-live="polite">
        {lines.length === 0 && (
          <p className="panel-placeholder">Waiting for audio...</p>
        )}
        {lines.map((line, i) => (
          <span key={i}>{line} </span>
        ))}
        <div ref={bottomRef} />
      </div>
    </section>
  );
}
