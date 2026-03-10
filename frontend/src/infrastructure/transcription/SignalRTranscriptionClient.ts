import type { TranscriptLanguage, TranscriptionMode } from "@/domain/transcription";
import type { AudioChunk } from "@/infrastructure/audio/browserAudioInputService";

export type ClientConnectionState =
  | "connecting"
  | "connected"
  | "reconnecting"
  | "disconnected";

export interface TranscriptEvent {
  language: TranscriptLanguage;
  text: string;
  isFinal: boolean;
  timestampLabel?: string;
  source: TranscriptionMode;
}

export interface SignalRTranscriptionClientHandlers {
  onConnectionStateChange?: (state: ClientConnectionState) => void;
  onTranscript?: (event: TranscriptEvent) => void;
  onError?: (message: string) => void;
}

export interface SignalRTranscriptionClient {
  setHandlers(handlers: SignalRTranscriptionClientHandlers): void;
  connect(): Promise<void>;
  startSession(sessionId: string): Promise<void>;
  sendAudioChunk(sessionId: string, chunk: AudioChunk): Promise<void>;
  endSession(sessionId: string): Promise<void>;
  disconnect(): Promise<void>;
  dispose(): Promise<void>;
}
