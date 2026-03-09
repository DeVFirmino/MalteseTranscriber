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
│  Speechmatics/SpeechmaticsService,               │
│  TranslationService, TranscriptionPipeline       │
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
       ▼ raw PCM bytes (no buffering, no WAV conversion)
IStreamingTranscriptionService.SendAudioAsync()
       │
       ▼ binary WebSocket frame
Speechmatics WSS (wss://eu.rt.speechmatics.com/v2)
       │
       ▼ AddTranscript event (async callback)
Pipeline.OnTranscriptReceivedAsync()
       │
       ├──► ITranscriptionNotifier.SendMalteseTranscriptionAsync() ──► client
       │
       ▼ Maltese text
ITranslationService.TranslateAsync() ──► OpenAI GPT-4o
       │
       ▼ English text
ITranscriptionNotifier.SendEnglishTranslationAsync() ──► client
```

## Key Design Decisions

### Speechmatics Real-Time WebSocket (replacing OpenAI Whisper)
Speechmatics provides 96% accuracy for Maltese (3.1% WER). Uses a persistent WebSocket per session instead of HTTP POST per chunk. Audio is streamed directly as raw PCM — no buffering or WAV conversion needed. Transcripts arrive asynchronously via `AddTranscript` messages.

Code is organized under `Infrastructure/Speechmatics/`:
- **SpeechmaticsService** — manages WebSocket connections per session
- **SpeechmaticsConnection** — encapsulates per-session socket state
- **SpeechmaticsMessageHandler** — parses incoming messages and fires callbacks

### IStreamingTranscriptionService (Core interface)
Streaming contract with `ConnectAsync`, `SendAudioAsync`, `DisconnectAsync`. The `onTranscript` callback is registered at connect time and fired when finalized transcripts arrive. This decouples the pipeline from any specific transcription provider.

### ITranscriptionNotifier (breaking circular dependency)
`TranscriptionPipeline` (Infrastructure) needs to push results to SignalR clients (API layer). Directly referencing `IHubContext<TranscriptionHub>` would create Infrastructure → API dependency. Instead:
- `ITranscriptionNotifier` is defined in **Core**
- `SignalRTranscriptionNotifier` implements it in **API**
- `TranscriptionPipeline` depends only on the Core interface

### Fake Services for Development
In Development mode, `FakeStreamingTranscriptionService` and `FakeTranslationService` replace real API calls. This allows full end-to-end testing without API costs. Swap is controlled by `ASPNETCORE_ENVIRONMENT` in `.env`.

### FluentValidation on SignalR Hub
Hub methods validate input via `IValidator<T>` before processing. Invalid requests get an `OnError` event sent back to the caller instead of throwing exceptions.

### Legacy Files (retained for fallback)
`IWhisperService`, `WhisperService`, `FakeWhisperService`, and `AudioConverter` are kept in the codebase but no longer in the active code path. They will be used for a future fallback architecture (Speechmatics fails → fall back to gpt-4o-transcribe).

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
| `signalr` | Fixed Window | 30 conn | 1 min | IP address | `/hubs/transcription` |

## Session Lifecycle

```
1. Client connects WebSocket → Hub.OnConnectedAsync()
2. Client calls StartSession(sessionId) → validates → adds to group → caches session → opens Speechmatics WSS
3. Client streams SendAudioChunk(sessionId, base64, index) → validates → forwards PCM to Speechmatics
4. Speechmatics sends AddTranscript → callback → notify Maltese → translate → notify English
5. Client calls EndSession(sessionId) → sends EndOfStream to Speechmatics → disconnects WSS → removes cache
6. On disconnect → Hub.OnDisconnectedAsync() → cleanup
```

Sessions are cached with a 2-hour sliding expiration via `IMemoryCache`.

## Environment Variables

| Variable | Required | Used By |
|----------|----------|---------|
| `SPEECHMATICS_API_KEY` | Production | SpeechmaticsService (Maltese transcription) |
| `OPENAI_API_KEY` | Production | TranslationService (GPT-4o Maltese→English) |
| `ASPNETCORE_ENVIRONMENT` | Always | Determines fake vs real services |
| `FRONTEND_URL` | Optional | CORS origin override |

## Future Enhancements

### Rate Limiting for Speechmatics API
Add ASP.NET Core rate limiting middleware ([docs](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-9.0)) to control Speechmatics API usage. Important for deployed portfolio project to prevent abuse.

### Fallback Architecture
If Speechmatics fails (connection error, timeout, API down), automatically fall back to `gpt-4o-transcribe` via the existing `WhisperService`. Pattern: `ResilientTranscriptionService` wrapping both providers with circuit breaker logic.
