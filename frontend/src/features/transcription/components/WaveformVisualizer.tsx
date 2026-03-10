import { cn } from "@/lib/utils";

interface WaveformVisualizerProps {
  active: boolean;
  statusLabel: string;
}

const WAVEFORM_BARS = 40;
const WAVEFORM_BAR_IDS = Array.from({ length: WAVEFORM_BARS }, (_, index) => `wave-bar-${index}`);

export const WaveformVisualizer = ({
  active,
  statusLabel,
}: WaveformVisualizerProps) => (
  <div aria-label={`Waveform visualizer: ${statusLabel}`}>
    <div className="flex h-32 items-end gap-2 sm:h-36">
      {WAVEFORM_BAR_IDS.map((barId, index) => (
        <span
          key={barId}
          className={cn(
            "flex-1 bg-foreground/18 transition-all duration-300",
            active ? "animate-waveform-rise bg-primary/65" : "h-[14%]",
          )}
          style={
            active
              ? {
                  height: `${18 + ((index * 17) % 62)}%`,
                  animationDelay: `${index * 60}ms`,
                }
              : undefined
          }
        />
      ))}
    </div>
    <div className="mt-4 flex items-center justify-between text-[11px] font-semibold uppercase tracking-[0.32em] text-muted-foreground">
      <span>16kHz PCM16</span>
      <span>{active ? "Input active" : statusLabel}</span>
    </div>
  </div>
);
