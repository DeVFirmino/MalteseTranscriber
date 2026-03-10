import { useCallback, useEffect, useMemo, useReducer, useRef } from "react";

import type {
  TranscriptLanguage,
  TranscriptSegment,
  TranscriptionMode,
} from "@/domain/transcription";
import { BrowserAudioInputService, type AudioInputService } from "@/infrastructure/audio/browserAudioInputService";
import { appConfig, type AppConfig } from "@/infrastructure/config/appConfig";
import { BrowserSignalRTranscriptionClient } from "@/infrastructure/transcription/browserSignalRTranscriptionClient";
import {
  type ClientConnectionState,
  type SignalRTranscriptionClient,
  type TranscriptEvent,
} from "@/infrastructure/transcription/SignalRTranscriptionClient";
import { SampleTranscriptionClient } from "@/infrastructure/transcription/sampleTranscriptionClient";
import {
  createInitialTranscriptionState,
  transcriptionReducer,
} from "@/features/transcription/model/transcriptionReducer";

interface UseTranscriptionSessionOptions {
  config?: AppConfig;
  createAudioInput?: () => AudioInputService;
  createLiveClient?: (config: AppConfig) => SignalRTranscriptionClient;
  createSampleClient?: () => SignalRTranscriptionClient;
  now?: () => Date;
}

const getErrorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "An unexpected transcription error occurred.";

const formatTimestampLabel = (order: number) => {
  const totalSeconds = order * 4;
  const minutes = Math.floor(totalSeconds / 60)
    .toString()
    .padStart(2, "0");
  const seconds = (totalSeconds % 60).toString().padStart(2, "0");

  return `${minutes}:${seconds}`;
};

export const useTranscriptionSession = (options: UseTranscriptionSessionOptions = {}) => {
  const {
    config: providedConfig,
    createAudioInput: createAudioInputOverride,
    createLiveClient: createLiveClientOverride,
    createSampleClient: createSampleClientOverride,
    now: nowOverride,
  } = options;
  const resolvedConfig = providedConfig ?? appConfig;
  const [session, dispatch] = useReducer(
    transcriptionReducer,
    createInitialTranscriptionState(resolvedConfig.defaultMode),
  );
  const sessionRef = useRef(session);
  const clientRef = useRef<SignalRTranscriptionClient | null>(null);
  const audioInputRef = useRef<AudioInputService | null>(null);
  const sessionIdRef = useRef<string | null>(null);
  const stopRequestedRef = useRef(false);
  const segmentCounterRef = useRef<Record<TranscriptLanguage, number>>({ mt: 0, en: 0 });

  useEffect(() => {
    sessionRef.current = session;
  }, [session]);

  const now = useMemo(() => nowOverride ?? (() => new Date()), [nowOverride]);

  const createAudioInput = useCallback(
    () => createAudioInputOverride?.() ?? new BrowserAudioInputService(),
    [createAudioInputOverride],
  );

  const createClient = useCallback(
    (mode: TranscriptionMode) =>
      mode === "live"
        ? (createLiveClientOverride?.(resolvedConfig) ??
          new BrowserSignalRTranscriptionClient(resolvedConfig))
        : (createSampleClientOverride?.() ?? new SampleTranscriptionClient()),
    [createLiveClientOverride, createSampleClientOverride, resolvedConfig],
  );

  const disposeResources = useCallback(async () => {
    try {
      await audioInputRef.current?.stopCapture();
      await audioInputRef.current?.dispose();
    } catch {
      // Disposal should be best-effort to avoid masking the primary failure.
    }

    try {
      if (sessionIdRef.current) {
        await clientRef.current?.endSession(sessionIdRef.current);
      }
      await clientRef.current?.disconnect();
      await clientRef.current?.dispose();
    } catch {
      // Ignore disposal failures during teardown.
    }

    audioInputRef.current = null;
    clientRef.current = null;
    sessionIdRef.current = null;
    segmentCounterRef.current = { mt: 0, en: 0 };
  }, []);

  const toTranscriptSegment = useCallback(
    (event: TranscriptEvent): TranscriptSegment => {
      const order = segmentCounterRef.current[event.language];

      if (event.isFinal) {
        segmentCounterRef.current[event.language] += 1;
      }

      return {
        id: `${event.language}-${order}-${event.isFinal ? "final" : "partial"}`,
        language: event.language,
        text: event.text,
        transcriptType: event.isFinal ? "final" : "partial",
        order,
        timestampLabel: event.timestampLabel ?? formatTimestampLabel(order + 1),
        source: event.source,
        receivedAt: now().toISOString(),
      };
    },
    [now],
  );

  const handleConnectionState = useCallback((state: ClientConnectionState) => {
    if (stopRequestedRef.current && state === "disconnected") {
      return;
    }

    if (state === "connecting") {
      dispatch({ type: "setStatus", status: "connecting" });
      return;
    }

    if (state === "reconnecting") {
      dispatch({ type: "setStatus", status: "reconnecting" });
      return;
    }

    if (state === "connected") {
      dispatch({ type: "clearError" });
      dispatch({ type: "setStatus", status: "recording" });
      return;
    }

    dispatch({ type: "setError", message: "The transcription session disconnected unexpectedly." });
  }, []);

  const stopSession = useCallback(async () => {
    if (sessionRef.current.status === "idle") {
      return;
    }

    stopRequestedRef.current = true;
    dispatch({ type: "setStatus", status: "stopping" });

    await disposeResources();

    dispatch({ type: "sessionStopped" });
    stopRequestedRef.current = false;
  }, [disposeResources]);

  const clearSession = useCallback(
    async (nextMode?: TranscriptionMode) => {
      stopRequestedRef.current = true;
      await disposeResources();
      dispatch({ type: "reset", mode: nextMode ?? sessionRef.current.mode });
      stopRequestedRef.current = false;
    },
    [disposeResources],
  );

  const setMode = useCallback(
    async (mode: TranscriptionMode) => {
      if (sessionRef.current.mode === mode && sessionRef.current.status === "idle") {
        return;
      }

      await clearSession(mode);
    },
    [clearSession],
  );

  const startSession = useCallback(async () => {
    const currentMode = sessionRef.current.mode;

    if (currentMode === "live" && !resolvedConfig.signalRHubUrl) {
      dispatch({
        type: "setError",
        message:
          "Live hub mode requires VITE_SIGNALR_HUB_URL to point at your ASP.NET Core SignalR endpoint.",
      });
      return;
    }

    const sessionId = crypto.randomUUID();
    dispatch({
      type: "sessionStarted",
      sessionId,
      startedAt: now().toISOString(),
      mode: currentMode,
    });

    try {
      if (currentMode === "live") {
        dispatch({ type: "setStatus", status: "requestingPermission" });
        audioInputRef.current = createAudioInput();
        const permission = await audioInputRef.current.requestPermission();
        dispatch({ type: "setPermission", permission });

        if (permission !== "granted") {
          throw new Error("Microphone permission is required for live transcription mode.");
        }
      } else {
        dispatch({ type: "setPermission", permission: "not-required" });
      }

      const client = createClient(currentMode);
      client.setHandlers({
        onConnectionStateChange: handleConnectionState,
        onTranscript: (event) => {
          dispatch({ type: "receiveSegment", segment: toTranscriptSegment(event) });
        },
        onError: (message) => {
          dispatch({ type: "setError", message });
        },
      });

      clientRef.current = client;
      sessionIdRef.current = sessionId;

      dispatch({ type: "setStatus", status: "connecting" });
      await client.connect();
      await client.startSession(sessionId);

      if (currentMode === "live") {
        await audioInputRef.current?.startCapture(async (chunk) => {
          if (!clientRef.current || !sessionIdRef.current) {
            return;
          }

          await clientRef.current.sendAudioChunk(sessionIdRef.current, chunk);
        });
      }

      dispatch({ type: "setStatus", status: "recording" });
    } catch (error) {
      await disposeResources();
      dispatch({ type: "setError", message: getErrorMessage(error) });
    }
  }, [
    createAudioInput,
    createClient,
    disposeResources,
    handleConnectionState,
    now,
    resolvedConfig.signalRHubUrl,
    toTranscriptSegment,
  ]);

  useEffect(() => {
    return () => {
      void disposeResources();
    };
  }, [disposeResources]);

  const isSessionActive = [
    "requestingPermission",
    "connecting",
    "recording",
    "reconnecting",
    "stopping",
  ].includes(session.status);

  return {
    session,
    startSession,
    stopSession,
    clearSession,
    setMode,
    backendAvailable: Boolean(resolvedConfig.signalRHubUrl),
    sampleModeEnabled: resolvedConfig.sampleModeEnabled,
    isSessionActive,
  };
};
