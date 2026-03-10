# Frontend Guideline: Maltese -> English Transcriber

## Scope
This frontend is a single-purpose product page:
- One route: `/`
- One workflow: click `Transcribe`, show live Maltese text, show English translation, manage session
- No portfolio content
- No technical-stack marketing text in visible UI

## Product Rules
1. Keep the UI focused on the user task, not architecture.
2. Every visible label should be transcription-oriented (`Transcribe`, `Stop`, `Clear`, `Copy`, `Export`).
3. Session state must always be visible: status badge, permission state, elapsed time, and transcript panels.
4. Errors must be user-readable and shown inline above controls.
5. Keyboard accessibility is mandatory for all actions.

## Information Architecture
- Header: product title + theme toggle + compact session indicator
- Main area:
  - Controls row
  - Session metrics + waveform
  - Maltese transcript panel
  - English transcript panel
- Footer: small product description only

## Component Boundaries
- `src/pages/DemoPage.tsx`
  - Compose the full page
  - No transport details in JSX text
- `src/features/transcription/hooks/useTranscriptionSession.ts`
  - Own session lifecycle and transcript event handling
  - Expose a clean state/action API for the page
- `src/features/transcription/components/*`
  - Pure presentational components
  - No infrastructure coupling
- `src/infrastructure/*`
  - Browser APIs and transport clients only
  - No UI concerns

## Copy and Tone Rules
- Use short action-driven copy.
- Avoid platform wording in the interface (`SignalR`, `.NET`, `hub`, `backend`) unless required for an actual error.
- Avoid generic marketing copy (`portfolio`, `case study`, `recruiter`, `showcase`).

## State and Data Rules
- Keep state transitions explicit (`idle`, `connecting`, `recording`, `stopping`, `error`).
- Never mutate transcript arrays in place.
- Keep partial and final segments distinct.
- Session stop and clear must release audio/transport resources.

## Accessibility Rules
- Keep visible labels for all controls.
- Ensure focus-visible styles are present for buttons/links.
- Use `aria-live` for partial transcript updates.
- Maintain color contrast in both light and dark themes.

## Performance Rules
- Avoid unnecessary re-renders in large transcript lists.
- Keep animation lightweight (`transform`/`opacity`).
- Avoid shipping unused UI frameworks or component libraries.

## Testing Rules
- Unit tests:
  - reducer transitions
  - transcript rendering states
  - session hook transitions and error paths
- Integration tests:
  - start -> recording -> stop flow
  - permission denied flow
  - clear session flow
  - copy/export actions
- Route test:
  - root page renders with core controls

## Current Code Smell Audit (2026-03-10)

### P1: Hidden dual-mode complexity in a single-page product
- Evidence:
  - Forced sample mode in page mount: `src/pages/DemoPage.tsx:38`
  - Full live-mode branches still in hook: `src/features/transcription/hooks/useTranscriptionSession.ts:74`, `:191`
  - Live-mode env config still active: `src/infrastructure/config/appConfig.ts:23`, `:30`
- Why it is a smell:
  - The current UI is intentionally single-purpose, but the state model still carries two products (`sample` and `live`).
  - This increases maintenance and creates hidden behavior when config requests `live`.
- Recommendation:
  - Choose one direction:
  - A) Keep single-page product only: remove `live` branches and env mode switching.
  - B) Keep dual-mode product: reintroduce explicit mode UI and tests for both paths.

### P2: Catch-all redirect masks invalid routes
- Evidence:
  - `src/App.tsx:13`
- Why it is a smell:
  - Redirecting all unknown paths to `/` hides route mistakes and makes debugging harder.
- Recommendation:
  - Add a minimal not-found page or explicit error boundary for invalid paths.

### P2: Dependency surface is larger than current product needs
- Evidence:
  - Many template-era packages remain in `package.json` (`@radix-ui/*`, `recharts`, `vaul`, `react-hook-form`, etc.): `package.json:17-65`
- Why it is a smell:
  - More dependency updates, larger audit surface, and slower install/build maintenance.
- Recommendation:
  - Run an import usage audit and remove packages not imported by shipped features.

### P2: Audio capture uses deprecated browser processing API
- Evidence:
  - `createScriptProcessor` usage: `src/infrastructure/audio/browserAudioInputService.ts:86`
- Why it is a smell:
  - `ScriptProcessorNode` is legacy and less reliable long-term than `AudioWorklet`.
- Recommendation:
  - Migrate to `AudioWorklet` when stabilizing live microphone capture.

### P3: Page component is becoming too broad
- Evidence:
  - Mixed responsibilities in `src/pages/DemoPage.tsx:29-202` (layout + timer + metrics + action wiring)
- Why it is a smell:
  - Harder to test and evolve safely.
- Recommendation:
  - Extract:
  - `SessionHeader`
  - `SessionMetrics`
  - `TranscriptWorkspace`

### P3: App-level UI test is shallow
- Evidence:
  - `src/App.test.tsx:7-21`
- Why it is a smell:
  - Current route test does not validate start/stop transitions, error rendering, or export behavior.
- Recommendation:
  - Add integration tests targeting interaction flow and side effects.

## Actions Taken In This Pass
- Fixed elapsed timer update behavior during active sessions:
  - `src/pages/DemoPage.tsx:46-62`
  - The elapsed metric now updates every second while session is active.

## Implementation Checklist (Next Iteration)
- [ ] Decide and lock one mode strategy (`sample-only` vs `dual-mode`)
- [ ] Reduce dependency set to what the shipped page uses
- [ ] Add not-found handling strategy for invalid routes
- [ ] Add integration tests for session interactions
- [ ] Plan `AudioWorklet` migration for live capture stability
