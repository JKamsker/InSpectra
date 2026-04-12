# Todo Next: Playwright CI And E2E Hygiene

- ID: `TN-2026-04-12-04`
- State: `Completed`
- Added: `2026-04-12`
- Source: outer iteration 9 post-`g43` fresh-swarm wave 1 on hosted-green tip
  `6bb272d`
- Queue entry: [Todo Next Queue](../TodoNext.md)
- Pickup rule: this is the next contained implementation phase selected after
  the post-`g43` wave-1 convergence.
- Exit rule: mark the queue item `Completed`, `Deferred`, or `Rejected` with
  dated rationale and update [Logbook](../Logbook.md) accordingly.
- Active start: resumed from clean `feat/merge-tool` tip `f2f07dc` on
  `2026-04-12`
- Completed: `g44` (`99a2c5a`) with green `pull_request` run `24301203450`

## Why This Phase Goes Next

Wave 1 after `g43` mostly reconfirmed the known remaining clusters rather than
surfacing a materially new family. This slice is the most contained next HIGH:

- hosted PR CI still does not execute the existing Playwright E2E suite
- the adjacent E2E mediums are in the same slice:
  stale `.rendered` reuse and the conditional theme-toggle pass
- the write set can stay narrow to CI plus the existing Playwright helpers/specs

That makes it a better next phase than the broader static-HTML contract or
frontend code-size work, which would touch more product surface at once.

## Confirmed Problem Statement

Wave-1 evidence on `6bb272d`:

- `.github/workflows/ci.yml` still runs frontend unit tests plus build, but
  never installs Playwright browsers or runs `npm run test:e2e`
- `src/InSpectra.UI/e2e/render-helpers.ts` reuses the checked-in
  `e2e/.rendered` bundle whenever no overrides are passed, so local E2E can
  go green against stale rendered output
- `src/InSpectra.UI/e2e/html-render.spec.ts` makes the theme-toggle assertion
  conditional on the toggle existing, which turns a missing toggle into a
  silent pass

## Completed Scope

Keep the phase focused on the hosted Playwright gap and its adjacent test debt:

- added a PR-safe Playwright lane to hosted CI, including Playwright browser
  install plus Release/no-build execution against the hosted build
- made the E2E suite render fresh temp output instead of reusing
  `e2e/.rendered`
- made the theme-toggle contract assertive and round-trip checked instead of
  conditional

Expected file cluster:

- `.github/workflows/ci.yml`
- `src/InSpectra.UI/e2e/render-helpers.ts`
- `src/InSpectra.UI/e2e/html-render.spec.ts`
- any minimal Playwright/config package file needed for CI wiring

## Deferred To Later Phases

These remain open but are intentionally not mixed into this phase:

- static HTML public-contract drift (`--show-home`,
  `--enable-nuget-browser`, `--enable-package-upload`, and related claims)
- frontend code-size policy enforcement and large TSX splits
- process/viewer diagnostics preservation
- installed-tool determinism and action path/version determinism
- architecture-scanner hardening and static-analysis degradation handling
