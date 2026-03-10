import { useState, useRef, useCallback } from 'react';
import { buildHubConnection } from '../services/signalrService';

/**
 * Manages the SignalR connection lifecycle for a transcription session.
 * Owns: connection state, session ID, status transitions, event registration.
 */
export function useSignalR({ onMalteseTranscription, onEnglishTranslation, onError }) {
  const [status, setStatus] = useState('idle');
  const [errorMsg, setErrorMsg] = useState('');
  const connectionRef = useRef(null);
  const sessionIdRef = useRef(null);

  const connect = useCallback(async () => {
    try {
      setStatus('connecting');
      setErrorMsg('');

      const connection = buildHubConnection();

      connection.on('OnMalteseTranscription', onMalteseTranscription);
      connection.on('OnEnglishTranslation', onEnglishTranslation);
      connection.on('OnError', (data) => {
        onError(data);
        setErrorMsg(data.message);
      });

      connection.onreconnecting(() => setStatus('reconnecting'));
      connection.onreconnected(() => setStatus('recording'));
      connection.onclose(() => setStatus('idle'));

      await connection.start();
      connectionRef.current = connection;

      const sessionId = crypto.randomUUID();
      sessionIdRef.current = sessionId;
      await connection.invoke('StartSession', sessionId);

      setStatus('recording');
    } catch (err) {
      setStatus('error');
      setErrorMsg(err.message);
      throw err;
    }
  }, [onMalteseTranscription, onEnglishTranslation, onError]);

  const disconnect = useCallback(async () => {
    const connection = connectionRef.current;
    const sessionId = sessionIdRef.current;
    if (!connection || !sessionId) return;

    try {
      await connection.invoke('EndSession', sessionId);
      await connection.stop();
    } catch (err) {
      // best-effort disconnect
    } finally {
      connectionRef.current = null;
      sessionIdRef.current = null;
      setStatus('idle');
    }
  }, []);

  return {
    connection: connectionRef.current,
    connectionRef,
    sessionIdRef,
    status,
    errorMsg,
    connect,
    disconnect,
  };
}
