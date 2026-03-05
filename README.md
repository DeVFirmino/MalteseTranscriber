# MalteseTranscriber

Real-time Maltese speech transcription and English translation web application. Captures audio from the browser microphone, transcribes it using OpenAI Whisper (Maltese), and translates it to English using GPT-4o — all streamed live via SignalR.

## Tech Stack

- **Backend**: .NET 10, ASP.NET Core, SignalR (WebSocket)
- **AI Services**: OpenAI Whisper (speech-to-text), GPT-4o (translation)
- **Frontend**: React + Vite, Web Audio API
- **Validation**: FluentValidation
- **Logging**: Serilog (console + rolling file)
- **Testing**: xUnit, NSubstitute, FluentAssertions
- **Containerization**: Docker (multi-stage build)

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [ffmpeg](https://ffmpeg.org/) (for audio processing)
- [OpenAI API key](https://platform.openai.com/api-keys) (only needed for production mode)

## Quick Start

1. **Clone and configure environment**:
   ```bash
   git clone https://github.com/DeVFirmino/MalteseTranscriber.git
   cd MalteseTranscriber
   cp .env.example .env   # or edit .env directly
   ```

2. **Set your OpenAI key** (optional in dev mode):
   ```
   OPENAI_API_KEY=sk-proj-your-key-here
   ASPNETCORE_ENVIRONMENT=Development
   FRONTEND_URL=http://localhost:3000
   ```

3. **Run the backend**:
   ```bash
   cd src/MalteseTranscriber.API
   dotnet run
   ```
   Backend starts at `http://localhost:5000`.

4. **Run the frontend** (once scaffolded):
   ```bash
   cd frontend
   npm install
   npm run dev
   ```
   Frontend starts at `http://localhost:5173`.

5. **Open browser** → click "Start Recording" → speak in Maltese → see live transcription and translation.

## Running Tests

```bash
dotnet test
```

Tests use xUnit with FluentAssertions and NSubstitute for mocking. Test files are in `tests/MalteseTranscriber.Tests/`.

## Dev Mode vs Production Mode

| Feature | Development | Production |
|---------|------------|------------|
| Whisper API | `FakeWhisperService` (hardcoded Maltese phrases) | Real OpenAI Whisper API |
| Translation | `FakeTranslationService` (dictionary lookup) | Real GPT-4o API |
| OpenAI key | Not required | Required |
| Serilog | Console + file | Console + file |
| Error details | Detailed stack traces | Generic error messages |

Set `ASPNETCORE_ENVIRONMENT=Development` in `.env` to use fake services (no API costs).

## Project Structure

```
MalteseTranscriber/
├── src/
│   ├── MalteseTranscriber.API/            # ASP.NET host, middleware, SignalR hub
│   │   ├── Hubs/
│   │   │   └── TranscriptionHub.cs        # SignalR hub: StartSession, SendAudioChunk, EndSession
│   │   ├── Middleware/
│   │   │   ├── GlobalExceptionMiddleware.cs  # Maps exceptions to HTTP error responses
│   │   │   └── RateLimitingExtensions.cs     # Rate limiting: global, transcription, SignalR
│   │   ├── Services/
│   │   │   └── SignalRTranscriptionNotifier.cs  # Pushes results to clients via SignalR groups
│   │   ├── wwwroot/
│   │   │   └── test.html                  # Vanilla JS test page (pre-React)
│   │   └── Program.cs                     # DI wiring, middleware pipeline, CORS
│   │
│   ├── MalteseTranscriber.Core/           # Domain: interfaces, models, validators (no dependencies)
│   │   ├── Interfaces/
│   │   │   ├── IWhisperService.cs         # Speech-to-text contract
│   │   │   ├── ITranslationService.cs     # Translation contract
│   │   │   ├── ITranscriptionPipeline.cs  # Orchestration contract
│   │   │   └── ITranscriptionNotifier.cs  # Client notification contract
│   │   ├── Models/
│   │   │   └── TranscriptionSession.cs    # Session state: audio buffer, overlap, history
│   │   ├── Requests/
│   │   │   ├── StartSessionRequest.cs     # DTO for session start
│   │   │   └── AudioChunkRequest.cs       # DTO for audio chunk
│   │   └── Validators/
│   │       ├── StartSessionRequestValidator.cs   # SessionId rules
│   │       └── AudioChunkRequestValidator.cs     # Base64, size, chunk index rules
│   │
│   └── MalteseTranscriber.Infrastructure/ # Implementations: services, audio processing
│       ├── AudioConverter.cs              # PCM → WAV (44-byte header + data)
│       ├── WhisperService.cs              # OpenAI Whisper API client (Maltese)
│       ├── FakeWhisperService.cs          # Dev: cycles through 5 hardcoded Maltese phrases
│       ├── TranslationService.cs          # GPT-4o context-aware translation
│       ├── FakeTranslationService.cs      # Dev: dictionary lookup for known phrases
│       └── TranscriptionPipeline.cs       # Orchestrates: buffer → WAV → Whisper → translate → notify
│
├── tests/
│   └── MalteseTranscriber.Tests/          # Unit tests (xUnit + FluentAssertions + NSubstitute)
│
├── .env                                   # Environment variables (not committed)
├── .gitignore
└── MalteseTranscriber.sln
```
