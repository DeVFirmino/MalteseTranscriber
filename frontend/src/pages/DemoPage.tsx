import { useEffect, useMemo, useState } from "react";
import { AlertTriangle, AudioLines, Clock3, Languages } from "lucide-react";
import { toast } from "sonner";

import { RecordingControls } from "@/features/transcription/components/RecordingControls";
import { TranscriptPanel } from "@/features/transcription/components/TranscriptPanel";
import { WaveformVisualizer } from "@/features/transcription/components/WaveformVisualizer";
import { useTranscriptionSession } from "@/features/transcription/hooks/useTranscriptionSession";
import {
  buildTranscriptExport,
  getWordCount,
} from "@/features/transcription/utils/transcriptExport";

const formatElapsed = (startedAt: string | null) => {
  if (!startedAt) {
    return "00:00";
  }

  const started = new Date(startedAt).getTime();
  const elapsedSeconds = Math.max(0, Math.floor((Date.now() - started) / 1000));
  const minutes = Math.floor(elapsedSeconds / 60)
    .toString()
    .padStart(2, "0");
  const seconds = (elapsedSeconds % 60).toString().padStart(2, "0");

  return `${minutes}:${seconds}`;
};

export default function DemoPage() {
  const {
    session,
    startSession,
    stopSession,
    clearSession,
  } = useTranscriptionSession();

  const exportText = useMemo(() => buildTranscriptExport(session), [session]);
  const malteseWordCount = useMemo(() => getWordCount(session.maltese), [session.maltese]);
  const englishWordCount = useMemo(() => getWordCount(session.english), [session.english]);
  const sessionActive = ["recording", "reconnecting"].includes(session.status);
  const [, setElapsedTick] = useState(0);

  useEffect(() => {
    if (!session.startedAt || !sessionActive) {
      return;
    }

    const timer = window.setInterval(() => {
      setElapsedTick((value) => value + 1);
    }, 1000);

    return () => {
      window.clearInterval(timer);
    };
  }, [session.startedAt, sessionActive]);

  const elapsed = formatElapsed(session.startedAt);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(exportText);
      toast.success("Transcript copied to clipboard.");
    } catch {
      toast.error("Clipboard access is not available in this browser.");
    }
  };

  const handleDownload = () => {
    const blob = new Blob([exportText], { type: "text/plain;charset=utf-8" });
    const href = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = href;
    anchor.download = "maltese-to-english-session.txt";
    anchor.click();
    URL.revokeObjectURL(href);
    toast.success("Transcript export downloaded.");
  };

  return (
    <div className="mx-auto flex w-full max-w-[1800px] flex-col px-3 py-4 sm:px-6 lg:px-8 lg:py-8">
      <section className="border border-foreground/15 bg-background">
        <div className="border-b border-foreground/15 px-4 py-6 sm:px-6">
          <div className="grid gap-4 xl:grid-cols-[auto_minmax(0,1fr)] xl:items-center">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.36em] text-muted-foreground">
                Maltese session
              </p>
              <h1 className="mt-2 text-3xl font-semibold uppercase tracking-[0.02em] sm:text-4xl">
                Transcribe Maltese to English
              </h1>
            </div>
            <p className="max-w-3xl text-sm font-semibold uppercase tracking-[0.2em] text-muted-foreground xl:text-right">
              Click transcribe, watch the live text appear, and export your session when done.
            </p>
          </div>
        </div>

        {session.lastError ? (
          <div className="border-b border-destructive/35 bg-destructive/10 px-4 py-4 text-sm text-destructive sm:px-6">
            <div className="flex items-start gap-3">
              <AlertTriangle className="mt-0.5 h-4 w-4" />
              <p>{session.lastError}</p>
            </div>
          </div>
        ) : null}

        <RecordingControls
          status={session.status}
          microphonePermission={session.microphonePermission}
          onStart={() => {
            void startSession();
          }}
          onStop={() => {
            void stopSession();
          }}
          onReset={() => {
            void clearSession();
          }}
          onCopy={() => {
            void handleCopy();
          }}
          onDownload={handleDownload}
        />

        <div className="grid gap-0 xl:grid-cols-[minmax(0,1.4fr)_minmax(420px,1fr)]">
          <div className="border-b border-foreground/15 xl:border-b-0 xl:border-r xl:border-foreground/15">
            <section className="min-h-[640px]">
              <header className="border-b border-foreground/15 px-4 py-5 sm:px-6">
                <p className="text-[13px] font-semibold uppercase tracking-[0.34em] text-muted-foreground">
                  Session details
                </p>
              </header>

              <div className="flex min-h-[560px] flex-col justify-between gap-8 px-4 py-6 sm:px-6">
                <div className="grid gap-3 sm:grid-cols-2">
                  <SessionMetric icon={Clock3} label="Elapsed" value={elapsed} />
                  <SessionMetric
                    icon={AudioLines}
                    label="Maltese words"
                    value={`${malteseWordCount}`}
                  />
                  <SessionMetric
                    icon={Languages}
                    label="English words"
                    value={`${englishWordCount}`}
                  />
                  <SessionMetric
                    icon={Clock3}
                    label="Started"
                    value={session.startedAt ? new Date(session.startedAt).toLocaleTimeString() : "Not started"}
                  />
                </div>

                <WaveformVisualizer active={sessionActive} statusLabel={session.status} />
              </div>
            </section>
          </div>

          <div className="grid min-h-[780px] md:grid-rows-2">
            <TranscriptPanel
              title="Maltese - Original"
              subtitle="Original speech capture"
              finalSegments={session.maltese.finalSegments}
              partialSegment={session.maltese.partialSegment}
              placeholder="Waiting for audio..."
              isActive={sessionActive}
            />
            <TranscriptPanel
              title="English - Translation"
              subtitle="Translated output"
              finalSegments={session.english.finalSegments}
              partialSegment={session.english.partialSegment}
              placeholder="Waiting for translation..."
              isActive={sessionActive}
            />
          </div>
        </div>
      </section>
    </div>
  );
}

interface SessionMetricProps {
  icon: typeof Clock3;
  label: string;
  value: string;
}

const SessionMetric = ({ icon: Icon, label, value }: SessionMetricProps) => (
  <div className="border border-foreground/15 p-4">
    <div className="flex items-center gap-3">
      <Icon className="h-4 w-4 text-primary" />
      <p className="text-[11px] font-semibold uppercase tracking-[0.32em] text-muted-foreground">{label}</p>
    </div>
    <p className="mt-3 text-sm font-medium uppercase tracking-[0.16em] text-foreground">{value}</p>
  </div>
);
