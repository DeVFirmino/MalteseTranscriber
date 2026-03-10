import { renderHook, act, waitFor } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import type { TranscriptionMode } from "@/domain/transcription";
import type { AudioInputService } from "@/infrastructure/audio/browserAudioInputService";
import type {
  ClientConnectionState,
  SignalRTranscriptionClient,
  SignalRTranscriptionClientHandlers,
} from "@/infrastructure/transcription/SignalRTranscriptionClient";
import type { AppConfig } from "@/infrastructure/config/appConfig";
import { useTranscriptionSession } from "@/features/transcription/hooks/useTranscriptionSession";

class StubClient implements SignalRTranscriptionClient {
  handlers: SignalRTranscriptionClientHandlers = {};

  setHandlers(handlers: SignalRTranscriptionClientHandlers) {
    this.handlers = handlers;
  }

  async connect() {
    this.handlers.onConnectionStateChange?.("connecting");
    this.handlers.onConnectionStateChange?.("connected");
  }

  async startSession() {
    this.handlers.onTranscript?.({
      language: "mt",
      text: "Bonġu",
      isFinal: true,
      timestampLabel: "00:04",
      source: "sample",
    });
  }

  async sendAudioChunk() {
    return Promise.resolve();
  }

  async endSession() {
    return Promise.resolve();
  }

  async disconnect() {
    this.handlers.onConnectionStateChange?.("disconnected");
  }

  async dispose() {
    return Promise.resolve();
  }

  emitState(state: ClientConnectionState) {
    this.handlers.onConnectionStateChange?.(state);
  }
}

class StubAudioInput implements AudioInputService {
  constructor(private readonly permission: "granted" | "denied" = "granted") {}

  async requestPermission() {
    return this.permission;
  }

  async startCapture() {
    return Promise.resolve();
  }

  async stopCapture() {
    return Promise.resolve();
  }

  async dispose() {
    return Promise.resolve();
  }
}

const makeConfig = (mode: TranscriptionMode, signalRHubUrl = ""): AppConfig => ({
  appName: "Maltese Transcriber",
  signalRHubUrl,
  defaultMode: mode,
  sampleModeEnabled: true,
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
});

describe("useTranscriptionSession", () => {
  it("starts a sample session and receives transcript segments", async () => {
    const client = new StubClient();
    const { result } = renderHook(() =>
      useTranscriptionSession({
        config: makeConfig("sample"),
        createSampleClient: () => client,
      }),
    );

    await act(async () => {
      await result.current.startSession();
    });

    await waitFor(() => {
      expect(result.current.session.status).toBe("recording");
      expect(result.current.session.maltese.finalSegments).toHaveLength(1);
    });
  });

  it("fails fast when live mode has no configured hub URL", async () => {
    const { result } = renderHook(() =>
      useTranscriptionSession({
        config: makeConfig("live"),
      }),
    );

    await act(async () => {
      await result.current.startSession();
    });

    expect(result.current.session.status).toBe("error");
    expect(result.current.session.lastError).toMatch(/VITE_SIGNALR_HUB_URL/);
  });

  it("surfaces reconnect state for a live client", async () => {
    const client = new StubClient();
    const { result } = renderHook(() =>
      useTranscriptionSession({
        config: makeConfig("live", "https://example.test/hubs/transcription"),
        createLiveClient: () => client,
        createAudioInput: () => new StubAudioInput("granted"),
      }),
    );

    await act(async () => {
      await result.current.startSession();
    });

    act(() => {
      client.emitState("reconnecting");
    });

    expect(result.current.session.status).toBe("reconnecting");

    act(() => {
      client.emitState("connected");
    });

    expect(result.current.session.status).toBe("recording");
  });
});
