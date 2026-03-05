export function StatusBadge({ status }) {
  const labels = {
    idle: 'Ready',
    connecting: 'Connecting...',
    recording: 'Recording',
    error: 'Error',
  };

  return (
    <span className={`status-badge status-${status}`} role="status" aria-live="polite">
      {labels[status] || status}
    </span>
  );
}
