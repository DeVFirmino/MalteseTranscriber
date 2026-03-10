import type {
  MicrophonePermissionState,
  TranscriptCollection,
  TranscriptSegment,
  TranscriptionMode,
  TranscriptionSession,
  TranscriptionStatus,
} from "@/domain/transcription";
import {
  createEmptyTranscriptCollection,
  createInitialTranscriptionSession,
} from "@/domain/transcription";

type TranscriptionAction =
  | { type: "sessionStarted"; sessionId: string; startedAt: string; mode: TranscriptionMode }
  | { type: "setMode"; mode: TranscriptionMode }
  | { type: "setStatus"; status: TranscriptionStatus }
  | { type: "setPermission"; permission: MicrophonePermissionState }
  | { type: "receiveSegment"; segment: TranscriptSegment }
  | { type: "setError"; message: string }
  | { type: "clearError" }
  | { type: "sessionStopped" }
  | { type: "reset"; mode: TranscriptionMode };

const appendTranscriptSegment = (
  collection: TranscriptCollection,
  segment: TranscriptSegment,
): TranscriptCollection => {
  if (segment.transcriptType === "partial") {
    return {
      ...collection,
      partialSegment: segment,
    };
  }

  return {
    finalSegments: [...collection.finalSegments, segment],
    partialSegment: null,
  };
};

export const transcriptionReducer = (
  state: TranscriptionSession,
  action: TranscriptionAction,
): TranscriptionSession => {
  switch (action.type) {
    case "sessionStarted":
      return {
        ...createInitialTranscriptionSession(action.mode),
        sessionId: action.sessionId,
        startedAt: action.startedAt,
        status: "connecting",
      };
    case "setMode":
      return {
        ...state,
        mode: action.mode,
        microphonePermission: action.mode === "sample" ? "not-required" : "unknown",
      };
    case "setStatus":
      return {
        ...state,
        status: action.status,
      };
    case "setPermission":
      return {
        ...state,
        microphonePermission: action.permission,
      };
    case "receiveSegment":
      return action.segment.language === "mt"
        ? {
            ...state,
            maltese: appendTranscriptSegment(state.maltese, action.segment),
          }
        : {
            ...state,
            english: appendTranscriptSegment(state.english, action.segment),
          };
    case "setError":
      return {
        ...state,
        status: "error",
        lastError: action.message,
      };
    case "clearError":
      return {
        ...state,
        lastError: null,
      };
    case "sessionStopped":
      return {
        ...state,
        status: "idle",
        microphonePermission: state.mode === "sample" ? "not-required" : state.microphonePermission,
        maltese: {
          ...state.maltese,
          partialSegment: null,
        },
        english: {
          ...state.english,
          partialSegment: null,
        },
      };
    case "reset":
      return createInitialTranscriptionSession(action.mode);
    default:
      return state;
  }
};

export const createInitialTranscriptionState = (mode: TranscriptionMode) =>
  createInitialTranscriptionSession(mode);

export const clearTranscriptCollections = () => ({
  maltese: createEmptyTranscriptCollection(),
  english: createEmptyTranscriptCollection(),
});
