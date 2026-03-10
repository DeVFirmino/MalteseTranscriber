/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_SIGNALR_HUB_URL?: string;
  readonly VITE_TRANSCRIPTION_MODE?: "sample" | "live";
  readonly VITE_ENABLE_SAMPLE_MODE?: "true" | "false";
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
