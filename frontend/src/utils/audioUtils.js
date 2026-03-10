/**
 * Converts a Float32Array (WebAudio API format) to Int16Array (PCM 16-bit signed).
 * Clamps values to the valid [-32768, 32767] range.
 */
export function float32ToInt16(float32) {
  const int16 = new Int16Array(float32.length);
  for (let i = 0; i < float32.length; i++) {
    int16[i] = Math.max(-32768, Math.min(32767, float32[i] * 32768));
  }
  return int16;
}

/**
 * Encodes a Uint8Array to a base64 string.
 * Uses a loop instead of spread operator to avoid stack overflow on large buffers (96KB+).
 */
export function uint8ToBase64(bytes) {
  let binary = '';
  for (let i = 0; i < bytes.length; i++) binary += String.fromCharCode(bytes[i]);
  return btoa(binary);
}
