import './App.css';
import { useTranscription } from './hooks/useTranscription';
import { TranscriptPanel } from './components/TranscriptPanel';
import { StatusBadge } from './components/StatusBadge';

function App() {
  const { status, malteseLines, englishLines, errorMsg, start, stop, clear } =
    useTranscription();

  const isRecording = status === 'recording';
  const isConnecting = status === 'connecting';

  return (
    <div className="app">
      <header className="header">
        <h1>Maltese Transcriber</h1>
        <p className="subtitle">Real-time speech transcription — Maltese to English</p>
      </header>

      <div className="controls">
        <button
          className="btn btn-start"
          onClick={start}
          disabled={isRecording || isConnecting}
          aria-label="Start recording"
        >
          Start Recording
        </button>
        <button
          className="btn btn-stop"
          onClick={stop}
          disabled={!isRecording}
          aria-label="Stop recording"
        >
          Stop
        </button>
        <button
          className="btn btn-clear"
          onClick={clear}
          aria-label="Clear transcription"
        >
          Clear
        </button>
        <StatusBadge status={status} />
      </div>

      {errorMsg && (
        <div className="error-banner" role="alert">
          {errorMsg}
        </div>
      )}

      <div className="panels">
        <TranscriptPanel label="Maltese (Original)" flag="🇲🇹" lines={malteseLines} />
        <TranscriptPanel label="English (Translation)" flag="🇬🇧" lines={englishLines} />
      </div>
    </div>
  );
}

export default App;
