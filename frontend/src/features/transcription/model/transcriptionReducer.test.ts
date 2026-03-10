import { describe, expect, it } from "vitest";

import type { TranscriptSegment } from "@/domain/transcription";
import { createInitialTranscriptionState, transcriptionReducer } from "@/features/transcription/model/transcriptionReducer";

const makeSegment = (overrides: Partial<TranscriptSegment> = {}): TranscriptSegment => ({
  id: "mt-0-final",
  language: "mt",
  text: "Bonġu Malta",
  transcriptType: "final",
  order: 0,
  timestampLabel: "00:04",
  source: "sample",
  receivedAt: "2026-03-10T10:00:00.000Z",
  ...overrides,
});

describe("transcriptionReducer", () => {
  it("starts a new session and clears old transcript state", () => {
    const initial = createInitialTranscriptionState("sample");
    const withTranscript = transcriptionReducer(initial, {
      type: "receiveSegment",
      segment: makeSegment(),
    });

    const next = transcriptionReducer(withTranscript, {
      type: "sessionStarted",
      sessionId: "session-1",
      startedAt: "2026-03-10T10:01:00.000Z",
      mode: "sample",
    });

    expect(next.sessionId).toBe("session-1");
    expect(next.status).toBe("connecting");
    expect(next.maltese.finalSegments).toHaveLength(0);
    expect(next.english.finalSegments).toHaveLength(0);
  });

  it("stores partial segments separately from final segments", () => {
    const initial = createInitialTranscriptionState("sample");
    const withPartial = transcriptionReducer(initial, {
      type: "receiveSegment",
      segment: makeSegment({
        id: "mt-0-partial",
        transcriptType: "partial",
        text: "Bonġu",
      }),
    });
    const withFinal = transcriptionReducer(withPartial, {
      type: "receiveSegment",
      segment: makeSegment(),
    });

    expect(withPartial.maltese.partialSegment?.text).toBe("Bonġu");
    expect(withFinal.maltese.partialSegment).toBeNull();
    expect(withFinal.maltese.finalSegments).toHaveLength(1);
  });

  it("moves to error state with the supplied message", () => {
    const initial = createInitialTranscriptionState("sample");
    const next = transcriptionReducer(initial, {
      type: "setError",
      message: "SignalR failed",
    });

    expect(next.status).toBe("error");
    expect(next.lastError).toBe("SignalR failed");
  });

  it("resets back to the initial state for a given mode", () => {
    const initial = createInitialTranscriptionState("sample");
    const errored = transcriptionReducer(initial, {
      type: "setError",
      message: "SignalR failed",
    });

    const reset = transcriptionReducer(errored, {
      type: "reset",
      mode: "live",
    });

    expect(reset.mode).toBe("live");
    expect(reset.status).toBe("idle");
    expect(reset.microphonePermission).toBe("unknown");
    expect(reset.maltese.finalSegments).toHaveLength(0);
  });
});
