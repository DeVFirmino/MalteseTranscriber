import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import type { TranscriptSegment } from "@/domain/transcription";
import { TranscriptPanel } from "@/features/transcription/components/TranscriptPanel";

const finalSegment: TranscriptSegment = {
  id: "mt-0-final",
  language: "mt",
  text: "Bonġu, jien qed nitkellem bil-Malti.",
  transcriptType: "final",
  order: 0,
  timestampLabel: "00:04",
  source: "sample",
  receivedAt: "2026-03-10T10:00:00.000Z",
};

describe("TranscriptPanel", () => {
  it("renders a placeholder when no transcript exists", () => {
    render(
      <TranscriptPanel
        title="Maltese Transcript"
        subtitle="Original speech capture"
        finalSegments={[]}
        partialSegment={null}
        placeholder="Waiting for Maltese audio..."
        isActive={false}
      />,
    );

    expect(screen.getByText("Waiting for Maltese audio...")).toBeInTheDocument();
  });

  it("renders final and partial transcript segments", () => {
    render(
      <TranscriptPanel
        title="Maltese Transcript"
        subtitle="Original speech capture"
        finalSegments={[finalSegment]}
        partialSegment={{
          ...finalSegment,
          id: "mt-1-partial",
          transcriptType: "partial",
          text: "Qed inkompli",
        }}
        placeholder="Waiting for Maltese audio..."
        isActive
      />,
    );

    expect(screen.getByText("Bonġu, jien qed nitkellem bil-Malti.")).toBeInTheDocument();
    expect(screen.getByText("Qed inkompli")).toBeInTheDocument();
    expect(screen.getByText("partial")).toBeInTheDocument();
    expect(screen.getByText("1 final")).toBeInTheDocument();
  });
});
