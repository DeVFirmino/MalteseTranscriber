# Architecture

## Clean Architecture Layers

```
┌─────────────────────────────────────────────────┐
│                   API Layer                      │
│  Program.cs, TranscriptionHub, Middleware,       │
│  SignalRTranscriptionNotifier                    │
│  (depends on Core + Infrastructure)              │
├─────────────────────────────────────────────────┤
│              Infrastructure Layer                │
│  WhisperService, TranslationService,             │
│  TranscriptionPipeline, AudioConverter           │
│  (depends on Core only)                          │
├─────────────────────────────────────────────────┤
│                 Core Layer                        │
│  Interfaces, Models, Validators, Requests        │
│  (no dependencies — innermost layer)             │
└─────────────────────────────────────────────────┘
```

**Dependency rule**: layers only depend inward. Core has zero external references. Infrastructure implements Core interfaces. API wires everything together via DI.

## Data Flow

```
Browser Microphone
       │
       ▼
Web Audio API (16kHz/16-bit/mono PCM)
       │
       ▼ base64-encoded chunks (3s = 48,000 samples)
SignalR WebSocket ──► TranscriptionHub.SendAudioChunk()
       │
       ▼ validates via FluentValidation
TranscriptionPipeline.ProcessChunkAsync()
       │
       ├─ buffers audio until >= 96,000 bytes (3 seconds)
       ├─ prepends 16,000 bytes overlap from previous chunk
       │
       ▼ PCM → WAV (44-byte header)
AudioConverter.PcmToWav()
       │
       ▼ WAV bytes
IWhisperService.TranscribeAsync() ──► OpenAI Whisper (language: "mt")
       │
       ▼ Maltese text
ITranscriptionNotifier.SendMalteseTranscriptionAsync() ──► client
       │
       ▼ Maltese text
ITranslationService.TranslateAsync() ──► OpenAI GPT-4o
       │
       ▼ English text
ITranscriptionNotifier.SendEnglishTranslationAsync() ──► client
```

## Key Design Decisions

### ITranscriptionNotifier (breaking circular dependency)
`TranscriptionPipeline` (Infrastructure) needs to push results to SignalR clients (API layer). Directly referencing `IHubContext<TranscriptionHub>` would create Infrastructure → API dependency. Instead:
- `ITranscriptionNotifier` is defined in **Core**
- `SignalRTranscriptionNotifier` implements it in **API**
- `TranscriptionPipeline` depends only on the Core interface

### Fake Services for Development
In Development mode, `FakeWhisperService` and `FakeTranslationService` replace real OpenAI calls. This allows full end-to-end testing without API costs. Swap is controlled by `ASPNETCORE_ENVIRONMENT` in `.env`.

### Audio Overlap Buffer
Speech chunks are split every 3 seconds at arbitrary byte boundaries, which can cut words in half. A 500ms overlap (16,000 bytes) is prepended from the previous chunk to give Whisper context across boundaries.

### FluentValidation on SignalR Hub
Hub methods validate input via `IValidator<T>` before processing. Invalid requests get an `OnError` event sent back to the caller instead of throwing exceptions.

### Fire-and-Forget Transcription
`ProcessChunkAsync` triggers transcription via `Task.Run()` so the Hub method returns immediately. The client gets results asynchronously via SignalR events. Errors are caught and sent via `OnError`.

## Middleware Pipeline

Order in `Program.cs` (executed top-to-bottom for requests):

```
1. GlobalExceptionMiddleware  → catches unhandled exceptions, returns JSON { error, traceId }
2. SerilogRequestLogging      → structured HTTP request logs
3. RateLimiter                → enforces rate limit policies
4. Swagger                    → API documentation UI
5. StaticFiles                → serves wwwroot/ (test.html)
6. CORS                       → allows frontend origins
7. Routing                    → maps /hubs/transcription and /health
```

## Rate Limiting Policies

| Policy | Type | Limit | Window | Partition | Applied To |
|--------|------|-------|--------|-----------|-----------|
| `global` | Fixed Window | 100 req | 1 min | IP address | All endpoints |
| `transcription` | Sliding Window | 20 req | 1 min (4 segments) | IP address | Transcription endpoints |
| `signalr` | Fixed Window | 5 conn | 1 min | IP address | `/hubs/transcription` |

## Session Lifecycle

```
1. Client connects WebSocket → Hub.OnConnectedAsync()
2. Client calls StartSession(sessionId) → validates → adds to group → caches TranscriptionSession
3. Client streams SendAudioChunk(sessionId, base64, index) → validates → buffers → processes
4. Client calls EndSession(sessionId) → removes cache → leaves group
5. On disconnect → Hub.OnDisconnectedAsync() → cleanup
```

Sessions are cached with a 2-hour sliding expiration via `IMemoryCache`.
