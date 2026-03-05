# CLAUDE.md

## Git Rules
- NEVER add Co-Authored-By or any Claude attribution in commits
- Work on `develop` branch; `main` is for PRs only
- Conventional commits: `feat:`, `fix:`, `test:`, `docs:`, `chore:`, `style:`
- Granular commits — separate each piece of functionality (7-10 per feature)
- Remote: https://github.com/DeVFirmino/MalteseTranscriber.git

## Architecture
Clean Architecture with 3 layers:
- **Core** (`src/MalteseTranscriber.Core/`) — interfaces, models, validators, requests. Zero dependencies.
- **Infrastructure** (`src/MalteseTranscriber.Infrastructure/`) — implementations. Depends on Core only.
- **API** (`src/MalteseTranscriber.API/`) — ASP.NET host, SignalR hub, middleware. Wires everything via DI.

Key decisions:
- `ITranscriptionNotifier` defined in Core to break circular dependency (Infrastructure needs to push to SignalR clients in API)
- `FakeWhisperService` + `FakeTranslationService` used in Development mode (no OpenAI API costs)
- Real `WhisperService` + `TranslationService` used in Production mode (requires OPENAI_API_KEY)
- Audio format: PCM 16kHz/16-bit/mono, 3-second chunks (96KB), 500ms overlap buffer (16KB)
- SignalR hub at `/hubs/transcription` with events: OnMalteseTranscription, OnEnglishTranslation, OnError

## Testing
- xUnit + FluentAssertions + NSubstitute
- Naming: `Method_Should_Result_When_Condition`
- AAA pattern: `// Arrange` / `// Act` / `// Assert`
- Test folders mirror project: `tests/.../Validators/`, `tests/.../Infrastructure/`, `tests/.../API/`
- Use TDD where appropriate
- Currently 49 tests passing

## Commands
- Backend: `cd src/MalteseTranscriber.API && dotnet run` (port 5000)
- Frontend: `cd frontend && npm run dev` (port 5173)
- Tests: `dotnet test`
- Docker: `docker build -t maltese-transcriber .`

## Tech Stack
.NET 10 (RC), SignalR, OpenAI Whisper + GPT-4o, React 19 + Vite 7, FluentValidation, Serilog, Docker

## User Preferences
- User speaks Portuguese — may give instructions in Portuguese
- Wants internal documentation so they understand what's happening
- Prefers visible project location: `~/Documents/MalteseTranscriber`
