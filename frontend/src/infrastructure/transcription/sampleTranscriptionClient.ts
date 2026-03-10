import type { AudioChunk } from "@/infrastructure/audio/browserAudioInputService";
import type {
  SignalRTranscriptionClient,
  SignalRTranscriptionClientHandlers,
  TranscriptEvent,
} from "@/infrastructure/transcription/SignalRTranscriptionClient";

const SAMPLE_SCRIPT: Array<Omit<TranscriptEvent, "source"> & { delayMs: number }> = [
  {
    language: "mt",
    text: "Bonġu, jien qed nitkellem",
    isFinal: false,
    timestampLabel: "00:02",
    delayMs: 400,
  },
  {
    language: "mt",
    text: "Bonġu, jien qed nitkellem bil-Malti għal dan id-demo.",
    isFinal: true,
    timestampLabel: "00:04",
    delayMs: 1100,
  },
  {
    language: "en",
    text: "Hello, I am speaking",
    isFinal: false,
    timestampLabel: "00:05",
    delayMs: 1600,
  },
  {
    language: "en",
    text: "Hello, I am speaking Maltese for this live product demo.",
    isFinal: true,
    timestampLabel: "00:06",
    delayMs: 2200,
  },
  {
    language: "mt",
    text: "Is-sistema turi transkrizzjoni live, stat tal-konnessjoni, u esportazzjoni tat-test.",
    isFinal: true,
    timestampLabel: "00:09",
    delayMs: 3200,
  },
  {
    language: "en",
    text: "The experience highlights live transcription, connection status, and transcript export.",
    isFinal: true,
    timestampLabel: "00:10",
    delayMs: 3900,
  },
];

export class SampleTranscriptionClient implements SignalRTranscriptionClient {
  private handlers: SignalRTranscriptionClientHandlers = {};
  private timers: number[] = [];

  setHandlers(handlers: SignalRTranscriptionClientHandlers) {
    this.handlers = handlers;
  }

  async connect() {
    this.handlers.onConnectionStateChange?.("connecting");

    await new Promise((resolve) => {
      const timer = window.setTimeout(resolve, 250);
      this.timers.push(timer);
    });

    this.handlers.onConnectionStateChange?.("connected");
  }

  async startSession() {
    SAMPLE_SCRIPT.forEach((step) => {
      const timer = window.setTimeout(() => {
        this.handlers.onTranscript?.({
          language: step.language,
          text: step.text,
          isFinal: step.isFinal,
          timestampLabel: step.timestampLabel,
          source: "sample",
        });
      }, step.delayMs);

      this.timers.push(timer);
    });
  }

  async sendAudioChunk(_sessionId: string, _chunk: AudioChunk) {
    return Promise.resolve();
  }

  async endSession() {
    this.clearTimers();
  }

  async disconnect() {
    this.clearTimers();
    this.handlers.onConnectionStateChange?.("disconnected");
  }

  async dispose() {
    this.clearTimers();
  }

  private clearTimers() {
    this.timers.forEach((timer) => window.clearTimeout(timer));
    this.timers = [];
  }
}
