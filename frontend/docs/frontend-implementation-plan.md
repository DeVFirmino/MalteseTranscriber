# Maltese Transcriber Frontend Implementation Plan

## Objective
Modernize the current Lovable-generated frontend into a product-focused, production-minded React application that can later be hosted inside the same ASP.NET Core deployment as the existing SignalR backend.

The frontend must do two things well:

1. Present the Maltese Transcriber product clearly with no login requirement.
2. Stay architecturally clean so the real-time SignalR integration can be maintained without turning pages into backend-coupled code.

## Current-State Review
The original frontend provided a strong visual direction, but it was still a single-page demo with hardcoded transcript playback. It also mixed visual composition, demo logic, and transport assumptions inside one page component.

### Key problems in the original state
- The demo was timer-driven, not transport-driven.
- The route structure was effectively one page plus 404.
- The UI was desktop-heavy and not clearly organized for mobile.
- Query client scaffolding existed even though the product flow did not use it.
- The package manager state was inconsistent, which blocked reliable installation.
- There was no typed transcription contract between the page and a future SignalR client.

## Target UX and Page Architecture

### Public page map
- `/`
  Action:
  Present the product value, stack, and clear CTA.
  Reason:
  A landing page improves first impression and gives context before the demo.
  Expected outcome:
  Reviewers understand what the project does before interacting with it.
  Validation:
  A new visitor can identify the project purpose and navigate to the demo in one click.

- `/demo`
  Action:
  Provide the real transcription workspace with sample fallback and live hub mode.
  Reason:
  This is the proof page where the actual product interaction happens.
  Expected outcome:
  The user sees state changes, transcript rendering, export actions, and backend readiness.
  Validation:
  The page is usable without explanation and works even when no backend endpoint is configured.

- `/about`
  Action:
  Explain architecture, backend/frontend boundaries, and tradeoffs.
  Reason:
  Users and technical reviewers need a narrative around the implementation choices.
  Expected outcome:
  The project looks deliberate instead of looking like a generic UI shell.
  Validation:
  A reviewer can understand how SignalR, ASP.NET Core, and the frontend fit together.

- Legacy redirect from `/case-study` to `/about`
  Action:
  Keep a redirect for any older links during the transition.
  Reason:
  This avoids broken navigation while the public structure is simplified.
  Expected outcome:
  Old links still resolve to the current product information page.
  Validation:
  Visiting `/case-study` lands on `/about`.

- `404`
  Action:
  Keep a branded not-found experience within the same visual system.
  Reason:
  It avoids default fallback pages and keeps routing polished.
  Expected outcome:
  All routes feel intentional.
  Validation:
  Invalid paths still lead the user back to the primary journey.

## Clean Architecture Mapping

### App layer
Action:
Place routing, theme provider, and global layout in `src/app`.
Reason:
These concerns are cross-cutting and should not live inside feature pages.
Expected outcome:
App bootstrapping remains stable as features grow.
Validation:
Route and provider changes do not force edits inside feature logic.

### Domain layer
Action:
Define typed models for session status, microphone permission, transcript segments, and mode selection in `src/domain`.
Reason:
The UI and infrastructure should share a consistent language.
Expected outcome:
State transitions become explicit and testable.
Validation:
Reducer tests can verify behavior without rendering the whole app.

### Feature layer
Action:
Move transcription workflow logic into `src/features/transcription`.
Reason:
Pages should compose the experience, not own transport logic.
Expected outcome:
The demo page becomes a thin composition layer.
Validation:
Most behavior changes happen inside feature files, not route files.

### Infrastructure layer
Action:
Implement browser audio capture, SignalR client transport, config parsing, and sample fallback in `src/infrastructure`.
Reason:
These pieces are environment-specific and should be replaceable.
Expected outcome:
The live backend and sample fallback share the same frontend contract.
Validation:
Hook tests can swap real clients for fake clients without rewriting page code.

## SignalR Integration Design

### Frontend responsibilities
- Request microphone permission.
- Capture browser audio.
- Open and monitor the SignalR session.
- Render partial and final transcript segments.
- Handle reconnect and error feedback.
- Provide copy/export actions.

### Backend responsibilities
- Session orchestration rules.
- Speech-to-text processing.
- Translation generation.
- Final transcript correctness.
- Any persistence, analytics, or future multi-user behavior.

### Transport assumptions
- The hub URL is provided through `VITE_SIGNALR_HUB_URL`.
- Live mode is disabled or fails safely when the hub URL is missing.
- The frontend uses automatic reconnect and surfaces reconnect state in the UI.
- Sample mode remains available when the backend is offline.

## Best Practices Enforced

### UX and information architecture
- Keep the main CTA visible on the home page.
- Put the live workspace on its own page instead of mixing it with marketing content.
- Use explicit state labels for idle, connecting, recording, reconnecting, stopping, and error.
- Keep login out of the first release because it adds friction without strengthening the core transcription experience.

### Frontend engineering
- Use typed domain models for state and transcript segments.
- Keep page files thin and move workflow logic into hooks plus reducer-based state.
- Hide SignalR event names and microphone implementation details behind interfaces.
- Keep side effects in infrastructure or hooks, not in presentational components.
- Prefer accessible semantic HTML and visible focus states.

### Responsive design
- Use a stacked layout on small screens.
- Preserve the two-transcript mental model on larger screens.
- Avoid fixed desktop-only frames for the primary interaction.

### Tooling
- Standardize on `npm`.
- Remove dead package-manager artifacts where safe.
- Keep install, lint, test, and build commands reproducible.

## Detailed Implementation Phases

### Phase 1: Rebuild app shell
Action:
Replace the one-page app entry with a routed layout, top navigation, footer, and theme toggle.
Reason:
This creates the structure needed for a three-page product experience.
Expected outcome:
The app has a coherent public journey instead of a single isolated screen.
Validation:
Navigation tests confirm that Home, Demo, and About render correctly.

### Phase 2: Introduce typed transcription feature
Action:
Create domain types, reducer-driven state, a transcription session hook, and feature-specific UI components.
Reason:
This is the core architectural cleanup that prepares the frontend for real backend integration.
Expected outcome:
The demo workflow no longer depends on page-local timers.
Validation:
Reducer and hook tests confirm state transitions, transcript accumulation, and resets.

### Phase 3: Add infrastructure adapters
Action:
Build a browser audio adapter, a SignalR client adapter, and a sample fallback client.
Reason:
The live and sample experiences should share one feature contract.
Expected outcome:
The frontend can run in sample mode today and live backend mode later.
Validation:
Hook tests with fakes verify connection, reconnect, error, and stop behavior.

### Phase 4: Write documentation
Action:
Document the architecture, actions, reasoning, test expectations, and risks.
Reason:
The frontend should be handoff-ready for future implementation work and easy to explain in interviews.
Expected outcome:
Another engineer can follow the plan without guessing.
Validation:
The markdown includes action, reason, expected outcome, and validation for each major change.

### Phase 5: Stabilize tooling
Action:
Repair lockfile consistency and install the missing dependencies required by the new architecture.
Reason:
Implementation without a reproducible install/build/test path is fragile.
Expected outcome:
`npm install`, `npm run lint`, `npm test`, and `npm run build` all work.
Validation:
Run the full verification sequence at the end.

## Tests to Add and Why

### Reducer tests
Action:
Test session start, segment accumulation, error transitions, and reset behavior.
Reason:
Reducer logic defines the contract for the entire feature.
Expected outcome:
State changes stay predictable as UI complexity grows.
Validation:
Unit tests assert exact state transitions from explicit actions.

### Component tests
Action:
Test transcript panel placeholder, final segments, and partial updates.
Reason:
Transcript rendering is the core visible output of the product.
Expected outcome:
UI behavior stays stable while internal logic evolves.
Validation:
Rendered output matches the expected transcript state.

### Hook integration tests
Action:
Test sample-mode start, live-mode permission failure, and reconnect state handling with fake adapters.
Reason:
The hook is where transport, domain state, and UI expectations meet.
Expected outcome:
Critical orchestration paths remain stable without requiring the real backend in tests.
Validation:
The hook reaches the expected status and transcript outputs under controlled fake events.

### Navigation test
Action:
Test the public route flow from Home to Demo.
Reason:
The app is now multi-page and that must remain stable.
Expected outcome:
Top-level navigation continues to support the intended product journey.
Validation:
The test renders the app and confirms page transitions.

## Common Pitfalls and Mitigation
- Pitfall:
  Treating the frontend as the transcription engine.
  Mitigation:
  Keep backend logic behind the SignalR boundary and avoid duplicating backend responsibilities in the browser.

- Pitfall:
  Shipping a demo that fails when the backend is unavailable.
  Mitigation:
  Keep sample mode functional and clearly labeled.

- Pitfall:
  Hiding important state changes.
  Mitigation:
  Use explicit status badges, error banners, and permission indicators.

- Pitfall:
  Letting transport details leak into route components.
  Mitigation:
  Keep page components composition-focused and push orchestration into the feature hook and adapters.

## Action-by-Action Rationale
- Action:
  Add routed Home, Demo, and About pages.
  Reason:
  This improves clarity for users and keeps the product narrative focused.
  Expected outcome:
  A reviewer understands the project before and after using the demo.
  Validation:
  Route test plus manual click-through.

- Action:
  Create `useTranscriptionSession`.
  Reason:
  One hook should own session orchestration and state transitions.
  Expected outcome:
  The demo page remains focused on presentation.
  Validation:
  Hook tests cover start, stop, reconnect, and error paths.

- Action:
  Add `BrowserSignalRTranscriptionClient` and `SampleTranscriptionClient`.
  Reason:
  Real backend mode and sample fallback should implement the same interface.
  Expected outcome:
  The UI can switch transport modes without changing page logic.
  Validation:
  Live and sample tests use the same hook contract.

- Action:
  Add export and copy actions.
  Reason:
  This gives the demo a more realistic product feel without introducing accounts.
  Expected outcome:
  The visitor can take the transcript away immediately.
  Validation:
  Manual verification in the running app and smoke coverage in tests.

- Action:
  Add theme support and responsive layout.
  Reason:
  The referenced frontend patterns emphasized adaptability and usability.
  Expected outcome:
  The app reads well on mobile and desktop while keeping its current visual identity.
  Validation:
  Responsive manual QA and layout assertions in page-level smoke tests.

## Risks and Mitigation
- Risk:
  Backend event payloads may differ from the assumed payload fields.
  Mitigation:
  Keep event mapping isolated in the SignalR adapter so changes stay local.

- Risk:
  Browser audio APIs differ across environments.
  Mitigation:
  Surface unsupported-browser states clearly and keep sample mode available.

- Risk:
  Over-pruning dependencies could break generated UI primitives.
  Mitigation:
  Remove only obviously unused architecture scaffolding first and verify with a full build.

## Final Acceptance Checklist
- `npm install` completes successfully.
- `npm run lint` passes.
- `npm test` passes.
- `npm run build` passes.
- The app has Home, Demo, About, and 404 routes.
- Sample mode works without backend access.
- Live mode surfaces permission and backend readiness clearly.
- Transcript export works.
- Theme switching works.
- The documentation remains aligned with the implemented structure.
