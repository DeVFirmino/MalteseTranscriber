import { useState, useCallback } from 'react';
import { useSignalR } from './useSignalR';
import { useAudioCapture } from './useAudioCapture';

/**
 * Composes useSignalR and useAudioCapture into a single public API.
 * This hook owns transcript line state and processing indicator only.
 * App.jsx depends on the return shape — do not change it.
 */
export function useTranscription() {
  const [processing, setProcessing] = useState(false);
  const [malteseLines, setMalteseLines] = useState([]);
  const [englishLines, setEnglishLines] = useState([]);

  const { connectionRef, sessionIdRef, status, errorMsg, connect, disconnect } = useSignalR({
    onMalteseTranscription: (data) => {
      setProcessing(false);
      setMalteseLines((prev) => [...prev, data.text]);
    },
    onEnglishTranslation: (data) => {
      setEnglishLines((prev) => [...prev, data.translatedText]);
    },
    onError: () => {
      setProcessing(false);
    },
  });

  const { startCapture, stopCapture } = useAudioCapture({
    connectionRef,
    sessionIdRef,
    onChunkSent: () => setProcessing(true),
  });

  const start = useCallback(async () => {
    try {
      await connect();
      await startCapture();
    } catch (err) {
      // error state already set by useSignalR
    }
  }, [connect, startCapture]);

  const stop = useCallback(async () => {
    try {
      await stopCapture();
      await disconnect();
    } finally {
      setProcessing(false);
    }
  }, [stopCapture, disconnect]);

  const clear = useCallback(() => {
    setMalteseLines([]);
    setEnglishLines([]);
  }, []);

  return { status, processing, malteseLines, englishLines, errorMsg, start, stop, clear };
}
