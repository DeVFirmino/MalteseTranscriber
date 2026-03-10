/**
 * AudioWorkletProcessor — runs on a dedicated audio thread (not the main thread).
 * Receives 128-sample Float32 frames from the audio graph and forwards them
 * to the main thread via MessagePort for PCM chunking and transmission.
 */
class AudioProcessor extends AudioWorkletProcessor {
  process(inputs) {
    const channel = inputs[0]?.[0];
    if (channel) {
      // Transfer the buffer to avoid copying — main thread receives ArrayBuffer
      const copy = channel.slice();
      this.port.postMessage(copy.buffer, [copy.buffer]);
    }
    return true; // keep processor alive until explicitly disconnected
  }
}

registerProcessor('audio-processor', AudioProcessor);
