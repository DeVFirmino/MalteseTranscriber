import { useRef, useCallback } from 'react';
import { SAMPLE_RATE, SAMPLES_PER_CHUNK } from '../constants/audio';
import { float32ToInt16, uint8ToBase64 } from '../utils/audioUtils';

/**
 * Manages microphone capture and PCM chunking via AudioWorklet.
 * AudioWorkletNode runs on a dedicated audio thread — replacing the
 * deprecated ScriptProcessorNode which ran on the main thread.
 *
 * Sends base64-encoded 3-second PCM chunks to the SignalR connection.
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

    // Register the worklet processor (served from /public)
    await ctx.audioWorklet.addModule('/audioProcessor.js');

    const source = ctx.createMediaStreamSource(stream);
    const workletNode = new AudioWorkletNode(ctx, 'audio-processor');

    // Int16Array accumulator — avoids repeated array spread allocations
    let buffer = new Int16Array(0);
    let chunkIndex = 0;

    workletNode.port.onmessage = (e) => {
      // e.data is an ArrayBuffer (transferred, zero-copy) of 128 float32 samples
      const float32 = new Float32Array(e.data);
      const int16 = float32ToInt16(float32);

      // Append int16 samples to the accumulator
      const merged = new Int16Array(buffer.length + int16.length);
      merged.set(buffer);
      merged.set(int16, buffer.length);
      buffer = merged;

      if (buffer.length >= SAMPLES_PER_CHUNK) {
        const chunk = buffer.slice(0, SAMPLES_PER_CHUNK);
        buffer = buffer.slice(SAMPLES_PER_CHUNK);

        const base64 = uint8ToBase64(new Uint8Array(chunk.buffer));
        connectionRef.current?.invoke('SendAudioChunk', sessionIdRef.current, base64, chunkIndex);
        onChunkSent();
        chunkIndex++;
      }
    };

    source.connect(workletNode);
    workletNode.connect(ctx.destination);

    cleanupRef.current = async () => {
      workletNode.port.onmessage = null;
      workletNode.disconnect();
      source.disconnect();
      stream.getTracks().forEach((t) => t.stop());
      await ctx.close();
    };
  }, [connectionRef, sessionIdRef, onChunkSent]);

  const stopCapture = useCallback(async () => {
    await cleanupRef.current?.();
    cleanupRef.current = null;
  }, []);

  return { startCapture, stopCapture };
}
