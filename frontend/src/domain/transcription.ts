export type TranscriptionMode = "sample" | "live";

export type TranscriptionStatus =
  | "idle"
  | "requestingPermission"
  | "connecting"
  | "recording"
  | "reconnecting"
  | "stopping"
  | "error";

export type MicrophonePermissionState =
  | "unknown"
  | "granted"
  | "denied"
  | "unavailable"
  | "not-required";

export type TranscriptLanguage = "mt" | "en";

export type TranscriptSegmentState = "partial" | "final";

export interface TranscriptSegment {
  id: string;
  language: TranscriptLanguage;
  text: string;
  transcriptType: TranscriptSegmentState;
  order: number;
  timestampLabel: string;
  source: TranscriptionMode;
  receivedAt: string;
}

export interface TranscriptCollection {
  finalSegments: TranscriptSegment[];
  partialSegment: TranscriptSegment | null;
}

export interface TranscriptionSession {
  sessionId: string | null;
  mode: TranscriptionMode;
  status: TranscriptionStatus;
  microphonePermission: MicrophonePermissionState;
  startedAt: string | null;
  lastError: string | null;
  maltese: TranscriptCollection;
  english: TranscriptCollection;
}

export const createEmptyTranscriptCollection = (): TranscriptCollection => ({
  finalSegments: [],
  partialSegment: null,
});

export const createInitialTranscriptionSession = (
  mode: TranscriptionMode = "sample",
): TranscriptionSession => ({
  sessionId: null,
  mode,
  status: "idle",
  microphonePermission: mode === "sample" ? "not-required" : "unknown",
  startedAt: null,
  lastError: null,
  maltese: createEmptyTranscriptCollection(),
  english: createEmptyTranscriptCollection(),
});
