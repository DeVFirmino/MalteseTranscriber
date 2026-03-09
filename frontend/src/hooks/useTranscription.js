import { useState, useRef, useCallback } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

const BACKEND_URL = import.meta.env.VITE_BACKEND_URL || 'http://localhost:5001';
const SAMPLE_RATE = 16000;
const SAMPLES_PER_CHUNK = 48000; // 3 seconds

export function useTranscription() {
  const [status, setStatus] = useState('idle');
  const [processing, setProcessing] = useState(false);
  const [malteseLines, setMalteseLines] = useState([]);
  const [englishLines, setEnglishLines] = useState([]);
  const [errorMsg, setErrorMsg] = useState('');

  const connectionRef = useRef(null);
  const cleanupRef = useRef(null);

  const start = useCallback(async () => {
    try {
      setStatus('connecting');
      setErrorMsg('');

      const connection = new HubConnectionBuilder()
        .withUrl(`${BACKEND_URL}/hubs/transcription`)
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build();

      connection.on('OnMalteseTranscription', (data) => {
        setProcessing(false);
        setMalteseLines((prev) => [...prev, data.text]);
      });

      connection.on('OnEnglishTranslation', (data) => {
        setEnglishLines((prev) => [...prev, data.translatedText]);
      });

      connection.on('OnError', (data) => {
        setProcessing(false);
        setErrorMsg(data.message);
      });

      connection.onreconnecting(() => setStatus('reconnecting'));
      connection.onreconnected(() => setStatus('recording'));
      connection.onclose(() => {
        setStatus('idle');
        setProcessing(false);
      });

      await connection.start();
      connectionRef.current = connection;

      const sessionId = crypto.randomUUID();
      await connection.invoke('StartSession', sessionId);
      setStatus('recording');

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
        const float32 = e.inputBuffer.getChannelData(0);
        const int16 = new Int16Array(float32.length);
        for (let i = 0; i < float32.length; i++) {
          int16[i] = Math.max(-32768, Math.min(32767, float32[i] * 32768));
        }

        buffer.push(...int16);

        if (buffer.length >= SAMPLES_PER_CHUNK) {
          const chunk = new Int16Array(buffer.splice(0, SAMPLES_PER_CHUNK));
          const bytes = new Uint8Array(chunk.buffer);
          const base64 = btoa(String.fromCharCode(...bytes));
          connection.invoke('SendAudioChunk', sessionId, base64, chunkIndex);
          setProcessing(true);
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
        await connection.invoke('EndSession', sessionId);
        await connection.stop();
      };
    } catch (err) {
      setStatus('error');
      setErrorMsg(err.message);
    }
  }, []);

  const stop = useCallback(async () => {
    try {
      await cleanupRef.current?.();
      cleanupRef.current = null;
      connectionRef.current = null;
      setStatus('idle');
      setProcessing(false);
    } catch (err) {
      setErrorMsg(err.message);
      setStatus('idle');
      setProcessing(false);
    }
  }, []);

  const clear = useCallback(() => {
    setMalteseLines([]);
    setEnglishLines([]);
    setErrorMsg('');
  }, []);

  return { status, processing, malteseLines, englishLines, errorMsg, start, stop, clear };
}
