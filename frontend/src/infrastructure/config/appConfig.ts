import type { TranscriptionMode } from "@/domain/transcription";

export interface AppConfig {
  appName: string;
  signalRHubUrl: string;
  defaultMode: TranscriptionMode;
  sampleModeEnabled: boolean;
  signalRMethods: {
    startSession: string;
    sendAudioChunk: string;
    endSession: string;
  };
  signalREvents: {
    maltese: string;
    english: string;
    error: string;
  };
}

const parseRequestedMode = (value: string | undefined): TranscriptionMode =>
  value === "live" ? "live" : "sample";

const requestedMode = parseRequestedMode(import.meta.env.VITE_TRANSCRIPTION_MODE);
const signalRHubUrl = import.meta.env.VITE_SIGNALR_HUB_URL?.trim() ?? "";
const sampleModeEnabled = import.meta.env.VITE_ENABLE_SAMPLE_MODE !== "false";

export const appConfig: AppConfig = {
  appName: "Maltese Transcriber",
  signalRHubUrl,
  defaultMode: requestedMode === "live" && signalRHubUrl ? "live" : "sample",
  sampleModeEnabled,
  signalRMethods: {
    startSession: "StartSession",
    sendAudioChunk: "SendAudioChunk",
    endSession: "EndSession",
  },
  signalREvents: {
    maltese: "OnMalteseTranscription",
    english: "OnEnglishTranslation",
    error: "OnError",
  },
};
