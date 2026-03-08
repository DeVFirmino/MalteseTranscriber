# Session Handoff — March 8, 2026

## What Was Done This Session

### Bugs Fixed
1. **SignalR negotiate failing (429)** — Rate limiter was too strict (5 req/min). Increased to 30/min for SignalR policy.
2. **Whisper API 400 — language 'mt' not supported** — Removed `language` param from API call entirely. Maltese is not in Whisper's ~57 supported languages.
3. **StartSession failing silently** — Hub methods had no try-catch. SignalR swallowed exceptions with no server logs. Added try-catch + diagnostic logging to `StartSession` and `SendAudioChunk`.
4. **Frontend pointing to wrong port** — `frontend/.env` had `VITE_BACKEND_URL=http://localhost:5000` while backend moved to 5001 (macOS AirPlay uses 5000).
5. **Whisper hallucinating prompt text** — The prompt "Transcribe accurately including: ħ, għ, ċ, ż" was echoed back as transcription when audio was unclear. Removed character-listing prompt.
6. **whisper-1 misidentifying Maltese** — Without `language` param, each 3s chunk auto-detected as Arabic/Italian/Hebrew/French. Switched to `gpt-4o-transcribe` which has better language understanding.

### Commits (unpushed on develop)
```
441f49c9 feat: switch transcription model from whisper-1 to gpt-4o-transcribe
977cb47a chore: change backend port from 5000 to 5001
327ccfed test: update pipeline tests for new Whisper interface
d5b0c63c fix: add error handling to TranscriptionHub methods
d19ecca5 fix: remove unsupported language param from Whisper API call
```

### Key Decisions
- **gpt-4o-transcribe over whisper-1** — Produces coherent Maltese transcription. Tested side-by-side with Speechmatics (which is more accurate at 96%), but gpt-4o-transcribe is "good enough" and uses the same OpenAI API key. Speechmatics remains a future upgrade option.
- **No Whisper prompt** — Only a simple `"Transcribe this Maltese speech."` hint. Any detailed prompt gets hallucinated when audio quality is low.
- **Port 5001** — macOS AirPlay Receiver occupies port 5000 on modern Macs.
- **EnableDetailedErrors = true** — Temporarily enabled for all environments (not just Development) to aid debugging. Should be reverted to `builder.Environment.IsDevelopment()` before production deploy.

---

## Current State

### What Works
- SignalR WebSocket connection (negotiate + upgrade)
- Session lifecycle (StartSession → SendAudioChunk → EndSession)
- Real-time Maltese transcription via gpt-4o-transcribe
- Real-time Maltese→English translation via GPT-4o
- Frontend displays both panels (Maltese original + English translation)
- Error handling with logging in Hub methods
- 49 unit tests (validators, pipeline, notifier)

### What Doesn't Work Well
- **Audio quality via mic** — Capturing YouTube through laptop speakers → mic produces mediocre results. Direct mic input or virtual audio cable (BlackHole) would be much better.
- **No partial results** — Transcription only shows after full 3-second chunk is processed + API roundtrip. No streaming/partial display.

### Known Technical Debt
- `EnableDetailedErrors = true` should be `builder.Environment.IsDevelopment()` before production
- `frontend/.env` is not in `.gitignore` — contains no secrets but should be managed
- `ScriptProcessor` (Web Audio API) is deprecated — should migrate to `AudioWorklet`

---

## How to Run

```bash
# Backend (port 5001)
cd src/MalteseTranscriber.API && dotnet run

# Frontend (port 5173)
cd frontend && npm run dev

# Tests
dotnet test

# Docker
docker build -t maltese-transcriber .
```

**Required**: `.env` at project root with:
```
OPENAI_API_KEY=sk-proj-...
ASPNETCORE_ENVIRONMENT=Production
```

---

## Architecture Quick Reference

```
Core (zero deps)          Infrastructure (Core only)       API (wires DI)
├── IWhisperService       ├── WhisperService               ├── TranscriptionHub
├── ITranslationService   ├── TranslationService           ├── Program.cs (DI)
├── ITranscriptionPipeline├── TranscriptionPipeline         ├── GlobalExceptionMiddleware
├── ITranscriptionNotifier├── FakeWhisperService            └── RateLimitingExtensions
├── Models/               ├── FakeTranslationService
├── Validators/           └── AudioConverter
└── Requests/
```

**Data flow**: Mic → PCM chunks → SignalR → Pipeline buffers 96KB → WAV → gpt-4o-transcribe → Maltese text → GPT-4o → English text → SignalR → Frontend panels

---

## Next Steps / Roadmap

### Priority 1 — Polish & Robustness
- [ ] Revert `EnableDetailedErrors` to dev-only before deploy
- [ ] Add `.gitignore` entries for `frontend/.env`, `.claude/`, `.vscode/`
- [ ] Handle reconnection gracefully (SignalR `withAutomaticReconnect` is set but UI doesn't reflect reconnecting state)
- [ ] Add loading indicator while waiting for first transcription result

### Priority 2 — Audio Quality
- [ ] Migrate from deprecated `ScriptProcessor` to `AudioWorklet` for better audio processing
- [ ] Add noise gate / silence detection to skip sending silent chunks (saves API costs)
- [ ] Consider supporting system audio capture (virtual audio cable) for demo purposes

### Priority 3 — Features
- [ ] Add "copy to clipboard" button for transcriptions
- [ ] Add timestamp display per transcription line
- [ ] Session history / export (save transcription to file)
- [ ] Language selector (support other languages beyond Maltese)

### Priority 4 — Production
- [ ] Speechmatics integration for better Maltese accuracy (96% vs gpt-4o-transcribe)
- [ ] Deploy with Docker to cloud (Railway, Fly.io, Azure)
- [ ] Add health check dashboard
- [ ] Rate limiting per API key (multi-tenant)

### Priority 5 — Testing
- [ ] Run `dotnet test` and fix any failures from interface changes
- [ ] Add integration tests for the full pipeline (Hub → Pipeline → mock Whisper → Notifier)
- [ ] Add frontend tests (React Testing Library)
