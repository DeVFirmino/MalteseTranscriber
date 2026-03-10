import type { TranscriptionStatus } from "@/domain/transcription";
import { cn } from "@/lib/utils";

interface StatusBadgeProps {
  status: TranscriptionStatus;
}

const statusConfig: Record<
  TranscriptionStatus,
  { label: string; tone: string; dot: string }
> = {
  idle: {
    label: "Ready",
    tone: "border-foreground/15 bg-background text-foreground",
    dot: "bg-foreground/70",
  },
  requestingPermission: {
    label: "Awaiting Mic",
    tone: "border-amber-500/50 bg-amber-500/10 text-amber-700 dark:text-amber-300",
    dot: "bg-amber-500",
  },
  connecting: {
    label: "Connecting",
    tone: "border-amber-500/50 bg-amber-500/10 text-amber-700 dark:text-amber-300",
    dot: "bg-amber-500 animate-pulse",
  },
  recording: {
    label: "Recording",
    tone: "border-primary bg-primary text-primary-foreground",
    dot: "bg-primary animate-pulse",
  },
  reconnecting: {
    label: "Reconnecting",
    tone: "border-blue-500/50 bg-blue-500/10 text-blue-700 dark:text-blue-300",
    dot: "bg-blue-500 animate-pulse",
  },
  stopping: {
    label: "Stopping",
    tone: "border-foreground/15 bg-foreground text-background",
    dot: "bg-foreground/40",
  },
  error: {
    label: "Needs Attention",
    tone: "border-destructive bg-destructive text-destructive-foreground",
    dot: "bg-destructive",
  },
};

export const StatusBadge = ({ status }: StatusBadgeProps) => {
  const config = statusConfig[status];

  return (
    <div
      className={cn(
        "inline-flex items-center gap-2 border px-3 py-2 text-xs font-semibold uppercase tracking-[0.28em]",
        config.tone,
      )}
    >
      <span
        className={cn(
          "h-2.5 w-2.5 rounded-full",
          status === "recording" ? "bg-primary-foreground" : config.dot,
        )}
        aria-hidden="true"
      />
      {config.label}
    </div>
  );
};
