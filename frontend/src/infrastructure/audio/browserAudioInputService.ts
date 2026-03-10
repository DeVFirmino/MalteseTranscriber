import type { MicrophonePermissionState } from "@/domain/transcription";

export interface AudioChunk {
  base64Audio: string;
  sequence: number;
  capturedAt: string;
  sampleRate: number;
  channelCount: number;
}

export interface AudioInputService {
  requestPermission(): Promise<MicrophonePermissionState>;
  startCapture(onChunk: (chunk: AudioChunk) => Promise<void> | void): Promise<void>;
  stopCapture(): Promise<void>;
  dispose(): Promise<void>;
}

const TARGET_SAMPLE_RATE = 16000;
const CHANNEL_COUNT = 1;
const CHUNK_SAMPLE_SIZE = 16000;

const toBase64 = (bytes: Uint8Array) => {
  let binary = "";
  const blockSize = 0x8000;

  for (let index = 0; index < bytes.length; index += blockSize) {
    binary += String.fromCharCode(...bytes.subarray(index, index + blockSize));
  }

  return window.btoa(binary);
};

export class BrowserAudioInputService implements AudioInputService {
  private stream: MediaStream | null = null;
  private audioContext: AudioContext | null = null;
  private sourceNode: MediaStreamAudioSourceNode | null = null;
  private processorNode: ScriptProcessorNode | null = null;
  private sequence = 0;
  private sampleBuffer: number[] = [];

  async requestPermission(): Promise<MicrophonePermissionState> {
    if (!navigator.mediaDevices?.getUserMedia) {
      return "unavailable";
    }

    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        audio: {
          sampleRate: TARGET_SAMPLE_RATE,
          channelCount: CHANNEL_COUNT,
          echoCancellation: true,
          noiseSuppression: true,
        },
      });

      stream.getTracks().forEach((track) => track.stop());
      return "granted";
    } catch (error) {
      const message =
        error instanceof Error ? error.message.toLowerCase() : String(error).toLowerCase();

      if (message.includes("denied") || message.includes("notallowed")) {
        return "denied";
      }

      return "unavailable";
    }
  }

  async startCapture(onChunk: (chunk: AudioChunk) => Promise<void> | void) {
    if (!navigator.mediaDevices?.getUserMedia || typeof window.AudioContext === "undefined") {
      throw new Error("This browser does not support microphone streaming.");
    }

    this.stream = await navigator.mediaDevices.getUserMedia({
      audio: {
        sampleRate: TARGET_SAMPLE_RATE,
        channelCount: CHANNEL_COUNT,
        echoCancellation: true,
        noiseSuppression: true,
      },
    });

    this.audioContext = new window.AudioContext({ sampleRate: TARGET_SAMPLE_RATE });
    this.sourceNode = this.audioContext.createMediaStreamSource(this.stream);
    this.processorNode = this.audioContext.createScriptProcessor(4096, CHANNEL_COUNT, CHANNEL_COUNT);
    this.sequence = 0;
    this.sampleBuffer = [];

    this.processorNode.onaudioprocess = (event) => {
      const input = event.inputBuffer.getChannelData(0);

      for (let index = 0; index < input.length; index += 1) {
        const sample = Math.max(-1, Math.min(1, input[index]));
        this.sampleBuffer.push(sample);
      }

      if (this.sampleBuffer.length < CHUNK_SAMPLE_SIZE) {
        return;
      }

      const frame = this.sampleBuffer.splice(0, CHUNK_SAMPLE_SIZE);
      const pcm16 = new Int16Array(frame.length);

      frame.forEach((sample, index) => {
        pcm16[index] = Math.max(-32768, Math.min(32767, sample * 32768));
      });

      const bytes = new Uint8Array(pcm16.buffer);

      void onChunk({
        base64Audio: toBase64(bytes),
        sequence: this.sequence++,
        capturedAt: new Date().toISOString(),
        sampleRate: TARGET_SAMPLE_RATE,
        channelCount: CHANNEL_COUNT,
      });
    };

    this.sourceNode.connect(this.processorNode);
    this.processorNode.connect(this.audioContext.destination);
  }

  async stopCapture() {
    this.processorNode?.disconnect();
    this.sourceNode?.disconnect();
    this.processorNode = null;
    this.sourceNode = null;

    this.stream?.getTracks().forEach((track) => track.stop());
    this.stream = null;

    if (this.audioContext) {
      await this.audioContext.close();
      this.audioContext = null;
    }

    this.sampleBuffer = [];
  }

  async dispose() {
    await this.stopCapture();
  }
}
