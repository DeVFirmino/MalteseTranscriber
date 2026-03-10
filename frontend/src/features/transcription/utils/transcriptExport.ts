import type { TranscriptCollection, TranscriptionSession } from "@/domain/transcription";

const toPlainText = (collection: TranscriptCollection) =>
  collection.finalSegments.map((segment) => `[${segment.timestampLabel}] ${segment.text}`).join("\n");

export const getWordCount = (collection: TranscriptCollection) =>
  collection.finalSegments
    .flatMap((segment) => segment.text.split(/\s+/))
    .filter(Boolean).length;

export const buildTranscriptExport = (session: TranscriptionSession) => {
  const startedAt = session.startedAt
    ? new Date(session.startedAt).toLocaleString()
    : "Not started";

  return [
    "Maltese Transcriber Session",
    `Mode: ${session.mode}`,
    `Started: ${startedAt}`,
    "",
    "Maltese Transcript",
    toPlainText(session.maltese) || "No transcript captured.",
    "",
    "English Translation",
    toPlainText(session.english) || "No translation captured.",
  ].join("\n");
};
