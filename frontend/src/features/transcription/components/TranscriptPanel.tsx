import type { TranscriptSegment } from "@/domain/transcription";
import { Badge } from "@/components/ui/badge";

interface TranscriptPanelProps {
  title: string;
  subtitle: string;
  finalSegments: TranscriptSegment[];
  partialSegment: TranscriptSegment | null;
  placeholder: string;
  isActive: boolean;
}

export const TranscriptPanel = ({
  title,
  subtitle,
  finalSegments,
  partialSegment,
  placeholder,
  isActive,
}: TranscriptPanelProps) => {
  const hasContent = finalSegments.length > 0 || partialSegment;

  return (
    <section className="flex h-full flex-col border-b border-foreground/15 bg-background last:border-b-0">
      <header className="flex items-start justify-between gap-4 border-b border-foreground/15 px-5 py-4 sm:px-6">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.34em] text-muted-foreground">
            {title}
          </p>
          <p className="mt-2 text-sm uppercase tracking-[0.18em] text-muted-foreground">{subtitle}</p>
        </div>
        <Badge variant="outline" className="border-foreground/15 bg-background px-3 py-1 uppercase tracking-[0.28em]">
          {finalSegments.length} final
        </Badge>
      </header>

      <div className="flex-1 space-y-4 overflow-y-auto px-5 py-6 sm:px-6">
        {!hasContent ? (
          <p className="pt-10 font-transcript text-3xl italic text-muted-foreground">{placeholder}</p>
        ) : null}

        {finalSegments.map((segment) => (
          <article key={segment.id} className="border border-foreground/15 bg-background p-4">
            <div className="mb-3 flex items-center justify-between gap-3 text-[11px] font-semibold uppercase tracking-[0.32em] text-muted-foreground">
              <span>{segment.timestampLabel}</span>
              <span>{segment.source}</span>
            </div>
            <p className="font-transcript text-2xl leading-relaxed text-foreground">{segment.text}</p>
          </article>
        ))}

        {partialSegment ? (
          <article
            aria-live="polite"
            className="border border-dashed border-primary bg-primary/10 p-4"
          >
            <div className="mb-3 flex items-center justify-between gap-3 text-[11px] font-semibold uppercase tracking-[0.32em] text-primary">
              <span>{partialSegment.timestampLabel}</span>
              <span>partial</span>
            </div>
            <p className="font-transcript text-2xl leading-relaxed text-foreground">
              {partialSegment.text}
              {isActive ? (
                <span className="ml-1 inline-block h-5 w-[2px] animate-cursor-blink bg-primary align-text-bottom" />
              ) : null}
            </p>
          </article>
        ) : null}
      </div>
    </section>
  );
};
