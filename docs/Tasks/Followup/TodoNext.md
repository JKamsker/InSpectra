# Todo Next Queue

This queue is for tasks that must be picked up by the next follow-up iteration
before the normal fresh investigation swarm proceeds.

Use it for:

- user-injected tasks engineered outside the normal swarm flow
- carry-forward items intentionally queued for the next iteration
- large refactor briefs that need explicit triage before the loop resumes

## Queue Rules

1. Read this file before every new outer iteration.
2. Any item in `Ready`, `Carry`, or `In Progress` must be triaged before
   starting a fresh investigation swarm.
3. A queued item may be handled in one of four ways:
   - `Completed`: the work landed; include commit SHA(s) or a logbook ref.
   - `Deferred`: not done this iteration; add dated rationale and keep it in
     the queue.
   - `Rejected`: not accepted; add dated rationale and a logbook ref.
   - `In Progress`: actively being worked in the current iteration.
4. Do not silently skip queue items. Every non-completed item needs an explicit
   state and rationale.
5. Large items should live in dedicated detail files under `TodoNext/` and be
   linked from the queue row.
6. Update [Logbook](Logbook.md) whenever a queued item materially changes the
   active work, lands, is deferred, or is rejected.

## Active Queue

| ID | State | Added | Source | Summary | Detail | Exit rule |
|---|---|---|---|---|---|---|
| `TN-2026-04-12-04` | `Completed` | `2026-04-12` | Outer iteration 9 post-`g43` fresh-swarm wave 1 | Wire Playwright E2E into hosted PR CI and harden the local E2E suite so it no longer reuses stale rendered output or silently passes when the theme toggle is missing. | [2026-04-12-playwright-ci-and-e2e-hygiene.md](TodoNext/2026-04-12-playwright-ci-and-e2e-hygiene.md) | Completed on `g44` (`99a2c5a`) with green `pull_request` run `24301203450`; hosted CI now installs Playwright Chromium, runs `npm run test:e2e` against the Release build, the E2E helpers render fresh temp output, and the theme-toggle contract is assertive. See [Logbook](Logbook.md#current-open-items-after-g44-fresh-swarm-wave-1-2026-04-12). |
| `TN-2026-04-12-03` | `Completed` | `2026-04-12` | Outer iteration 8 post-`g41` fresh-swarm wave 1 | Add a hosted/package validation path that proves the packed tool still carries the required StartupHook payload layout after `.nupkg` install, closing the remaining packaged-tool verification HIGH. | [2026-04-12-packaged-tool-hook-verification.md](TodoNext/2026-04-12-packaged-tool-hook-verification.md) | Completed on `g42`/`g43` (`6ccb5b7`, `6bb272d`) with green `pull_request` run `24300661250`; the first follow-up run `24300589542` failed because the temp `NuGet.Config` used a relative local package source path, which `g43` corrected to an absolute path. See [Logbook](Logbook.md#current-open-items-after-g43-hosted-validation-2026-04-12). |
| `TN-2026-04-12-02` | `Completed` | `2026-04-12` | Outer iteration 8 fresh-swarm ranking | Decouple installed-tool process cleanup from the caller working directory by deriving sandbox cleanup roots from engine-managed sandbox state instead of user workspaces. | [2026-04-12-installed-tool-process-safety.md](TodoNext/2026-04-12-installed-tool-process-safety.md) | Completed on `g41` (`29a526c`) with `354 / 0 / 0` unit tests, `17` architecture tests, green `pull_request` run `24300030057`, and the queue-handling ledger in [Logbook](Logbook.md#current-open-items-after-g41-hosted-validation-2026-04-12). |
| `TN-2026-04-12-01` | `Completed` | `2026-04-12` | User-injected via `tmp.txt` | Finalize the thin-shell architecture: make `InSpectra.Gen` a true shell, move backend logic behind it, and rename `InSpectra.Gen.Acquisition` to an engine-shaped backend if justified by the code. | [2026-04-12-thin-shell-architecture.md](TodoNext/2026-04-12-thin-shell-architecture.md) | Completed on local phase `g40` (`8b3c0bc`) with `325 / 0 / 0` unit tests, `17` architecture tests, and the queue-handling ledger in [Logbook](Logbook.md#current-open-items-after-thin-shell-queue-handling-2026-04-12). |
