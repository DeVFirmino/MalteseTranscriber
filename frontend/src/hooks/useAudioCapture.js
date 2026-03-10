import { useRef, useCallback } from 'react';
import { SAMPLE_RATE, SAMPLES_PER_CHUNK } from '../constants/audio';
import { float32ToInt16, uint8ToBase64 } from '../utils/audioUtils';

/**
 * Manages microphone capture and PCM chunking.
 * Owns: getUserMedia, AudioContext, ScriptProcessorNode, buffer accumulation.
 * Sends base64-encoded PCM chunks to the SignalR connection.
 */
export function useAudioCapture({ connectionRef, sessionIdRef, onChunkSent }) {
  const cleanupRef = useRef(null);

  const startCapture = useCallback(async () => {
    const stream = await navigator.mediaDevices.getUserMedia({
      audio: {
        sampleRate: SAMPLE_RATE,
        channelCount: 1,
        echoCancellation: true,
        noiseSuppression: true,
      },
    });

    const ctx = new AudioContext({ sampleRate: SAMPLE_RATE });
    const source = ctx.createMediaStreamSource(stream);
    const processor = ctx.createScriptProcessor(4096, 1, 1);

    let buffer = [];
    let chunkIndex = 0;

    processor.onaudioprocess = (e) => {
      const int16 = float32ToInt16(e.inputBuffer.getChannelData(0));
      buffer.push(...int16);

      if (buffer.length >= SAMPLES_PER_CHUNK) {
        const chunk = new Int16Array(buffer.splice(0, SAMPLES_PER_CHUNK));
        const base64 = uint8ToBase64(new Uint8Array(chunk.buffer));
        connectionRef.current?.invoke('SendAudioChunk', sessionIdRef.current, base64, chunkIndex);
        onChunkSent();
        chunkIndex++;
      }
    };

    source.connect(processor);
    processor.connect(ctx.destination);

    cleanupRef.current = async () => {
      stream.getTracks().forEach((t) => t.stop());
      processor.disconnect();
      source.disconnect();
      await ctx.close();
    };
  }, [connectionRef, sessionIdRef, onChunkSent]);

  const stopCapture = useCallback(async () => {
    await cleanupRef.current?.();
    cleanupRef.current = null;
  }, []);

  return { startCapture, stopCapture };
}
