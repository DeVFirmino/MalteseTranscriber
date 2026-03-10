import { Copy, Download, Mic, RotateCcw, Square } from "lucide-react";

import type { MicrophonePermissionState, TranscriptionStatus } from "@/domain/transcription";
import { StatusBadge } from "@/features/transcription/components/StatusBadge";
import { cn } from "@/lib/utils";

interface RecordingControlsProps {
  status: TranscriptionStatus;
  microphonePermission: MicrophonePermissionState;
  onStart: () => void;
  onStop: () => void;
  onReset: () => void;
  onCopy: () => void;
  onDownload: () => void;
}

const permissionLabels: Record<MicrophonePermissionState, string> = {
  unknown: "Not requested",
  granted: "Granted",
  denied: "Denied",
  unavailable: "Unavailable",
  "not-required": "Not required",
};

const sessionLabels: Record<TranscriptionStatus, string> = {
  idle: "Ready",
  requestingPermission: "Requesting mic permission",
  connecting: "Starting session",
  recording: "Listening",
  reconnecting: "Resuming session",
  stopping: "Stopping",
  error: "Needs attention",
};

export const RecordingControls = ({
  status,
  microphonePermission,
  onStart,
  onStop,
  onReset,
  onCopy,
  onDownload,
}: RecordingControlsProps) => {
  const canStart = status === "idle" || status === "error";
  const canStop = ["requestingPermission", "connecting", "recording", "reconnecting"].includes(status);

  return (
    <section className="border-b border-foreground/15">
      <div className="grid gap-6 px-4 py-6 sm:px-6 xl:grid-cols-[auto_minmax(0,1fr)] xl:items-start">
        <div className="flex flex-wrap items-center gap-3">
          <PrimaryActionButton
            tone="primary"
            icon={Mic}
            label={status === "connecting" ? "Starting" : "Transcribe"}
            disabled={!canStart}
            onClick={onStart}
          />
          <PrimaryActionButton
            tone="secondary"
            icon={Square}
            label="Stop"
            disabled={!canStop}
            onClick={onStop}
          />
          <PrimaryActionButton
            tone="outline"
            icon={RotateCcw}
            label="Clear"
            disabled={false}
            onClick={onReset}
          />
        </div>

        <div className="grid gap-4 xl:justify-items-end">
          <div className="flex flex-wrap items-center gap-2">
            <SecondaryActionButton icon={Copy} label="Copy" onClick={onCopy} />
            <SecondaryActionButton icon={Download} label="Export" onClick={onDownload} />
          </div>

          <div className="flex flex-wrap items-center gap-3 text-sm font-semibold uppercase tracking-[0.24em] text-muted-foreground">
            <StatusBadge status={status} />
            <span>{sessionLabels[status]}</span>
          </div>
        </div>
      </div>

      <div className="grid gap-0 border-t border-foreground/15 md:grid-cols-3">
        <InfoTile label="Session" value={sessionLabels[status]} />
        <InfoTile label="Mic permission" value={permissionLabels[microphonePermission]} />
        <InfoTile label="Actions" value="Transcribe / Stop / Clear / Export" />
      </div>
    </section>
  );
};

interface InfoTileProps {
  label: string;
  value: string;
}

const InfoTile = ({ label, value }: InfoTileProps) => (
  <div className="border-r border-foreground/15 px-4 py-4 last:border-r-0">
    <p className="text-[11px] font-semibold uppercase tracking-[0.32em] text-muted-foreground">{label}</p>
    <p className="mt-2 text-sm font-medium uppercase tracking-[0.16em] text-foreground">{value}</p>
  </div>
);

interface PrimaryActionButtonProps {
  tone: "primary" | "secondary" | "outline";
  icon: typeof Mic;
  label: string;
  disabled: boolean;
  onClick: () => void;
}

const PrimaryActionButton = ({ tone, icon: Icon, label, disabled, onClick }: PrimaryActionButtonProps) => (
  <button
    type="button"
    onClick={onClick}
    disabled={disabled}
    className={cn(
      "inline-flex min-h-[78px] min-w-[188px] items-center justify-center gap-3 border px-6 py-4 text-left text-2xl font-semibold uppercase tracking-[0.04em] transition-colors disabled:cursor-not-allowed disabled:opacity-45",
      tone === "primary" && "border-primary bg-primary text-primary-foreground hover:bg-primary/90",
      tone === "secondary" && "border-foreground bg-foreground text-background hover:bg-foreground/90",
      tone === "outline" && "border-foreground bg-background text-foreground hover:bg-foreground hover:text-background",
    )}
  >
    <Icon className="h-7 w-7" />
    {label}
  </button>
);

interface SecondaryActionButtonProps {
  icon: typeof Copy;
  label: string;
  onClick: () => void;
}

const SecondaryActionButton = ({ icon: Icon, label, onClick }: SecondaryActionButtonProps) => (
  <button
    type="button"
    onClick={onClick}
    className="inline-flex items-center gap-2 border border-foreground/15 px-4 py-3 text-xs font-semibold uppercase tracking-[0.28em] text-foreground transition-colors hover:bg-foreground hover:text-background"
  >
    <Icon className="h-4 w-4" />
    {label}
  </button>
);
