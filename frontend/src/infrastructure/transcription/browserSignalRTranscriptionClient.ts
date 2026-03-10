import * as signalR from "@microsoft/signalr";

import type { AppConfig } from "@/infrastructure/config/appConfig";
import type { AudioChunk } from "@/infrastructure/audio/browserAudioInputService";
import type {
  SignalRTranscriptionClient,
  SignalRTranscriptionClientHandlers,
} from "@/infrastructure/transcription/SignalRTranscriptionClient";

interface TranscriptPayload {
  text?: string;
  translatedText?: string;
  transcript?: string;
  timestamp?: string;
  isFinal?: boolean;
}

export class BrowserSignalRTranscriptionClient implements SignalRTranscriptionClient {
  private connection: signalR.HubConnection | null = null;
  private handlers: SignalRTranscriptionClientHandlers = {};
  private order = 0;

  constructor(private readonly config: AppConfig) {}

  setHandlers(handlers: SignalRTranscriptionClientHandlers) {
    this.handlers = handlers;
  }

  async connect() {
    if (!this.config.signalRHubUrl) {
      throw new Error("SignalR hub URL is not configured.");
    }

    this.handlers.onConnectionStateChange?.("connecting");

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.config.signalRHubUrl)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.on(this.config.signalREvents.maltese, (payload: TranscriptPayload) => {
      const text = payload.text ?? payload.transcript ?? "";

      if (!text) {
        return;
      }

      this.handlers.onTranscript?.({
        language: "mt",
        text,
        isFinal: payload.isFinal ?? true,
        timestampLabel: payload.timestamp,
        source: "live",
      });
      this.order += 1;
    });

    this.connection.on(this.config.signalREvents.english, (payload: TranscriptPayload) => {
      const text = payload.translatedText ?? payload.text ?? payload.transcript ?? "";

      if (!text) {
        return;
      }

      this.handlers.onTranscript?.({
        language: "en",
        text,
        isFinal: payload.isFinal ?? true,
        timestampLabel: payload.timestamp,
        source: "live",
      });
      this.order += 1;
    });

    this.connection.on(this.config.signalREvents.error, (payload: { message?: string }) => {
      this.handlers.onError?.(payload.message ?? "The backend reported a transcription error.");
    });

    this.connection.onreconnecting(() => {
      this.handlers.onConnectionStateChange?.("reconnecting");
    });

    this.connection.onreconnected(() => {
      this.handlers.onConnectionStateChange?.("connected");
    });

    this.connection.onclose((error) => {
      if (error) {
        this.handlers.onError?.(error.message);
      }

      this.handlers.onConnectionStateChange?.("disconnected");
    });

    await this.connection.start();
    this.handlers.onConnectionStateChange?.("connected");
  }

  async startSession(sessionId: string) {
    if (!this.connection) {
      throw new Error("SignalR connection has not been created.");
    }

    await this.connection.invoke(this.config.signalRMethods.startSession, sessionId);
  }

  async sendAudioChunk(sessionId: string, chunk: AudioChunk) {
    if (!this.connection) {
      throw new Error("SignalR connection is not active.");
    }

    await this.connection.invoke(
      this.config.signalRMethods.sendAudioChunk,
      sessionId,
      chunk.base64Audio,
      chunk.sequence,
    );
  }

  async endSession(sessionId: string) {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    await this.connection.invoke(this.config.signalRMethods.endSession, sessionId);
  }

  async disconnect() {
    if (!this.connection) {
      return;
    }

    await this.connection.stop();
    this.handlers.onConnectionStateChange?.("disconnected");
  }

  async dispose() {
    await this.disconnect();
    this.connection = null;
  }
}
