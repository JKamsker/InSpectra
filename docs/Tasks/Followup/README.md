# Follow-up Docs

This folder replaces the old monolithic
`docs/Tasks/Restructure/Followup.md`.

## Current Status

- Working branch: `feat/merge-tool`
- Current follow-up docs live on the branch tip; verify `HEAD` before
  resuming work.
- Latest fully validated pushed tip: `9195242`
- Seven outer iterations shipped phases `g1`–`g39` on `feat/merge-tool`, the
  queue-driven thin-shell phase `g40`, the installed-tool process-safety phase
  `g41`, the packaged-tool verification phase `g42`, the hosted follow-up
  fix `g43`, the Playwright hosted-CI phase `g44`, the website truthfulness
  phase `g45`, the frontend file-limit phase `g46`, the static-package-route
  phase `g47`, the hosted test-support follow-up phase `g48`, the backend
  failure-diagnostics phase `g49`, the hosted OpenCli test-support follow-up
  phase `g50`, the installed-tool multi-TFM determinism phase `g51`, the
  viewer-bundle rebuild-failure phase `g52`, the install-host / failure-detail
  phase `g53`, and the discovery-contract carry-forward phase `g54` are now
  pushed and hosted validated.
- The pushed tip `9195242` is hosted validated:
  - `61` frontend unit tests
  - `12` Playwright E2E tests
  - `43` hosted live tests
  - green `pull_request` run `24311087147`
  - green `workflow_dispatch` run `24311095925`
  - local Docker validation on the same tip:
    non-live solution subset `341 / 341`, targeted live regeneration `8 / 8`,
    and targeted live rerun `8 / 8`
- The latest green `workflow_dispatch` validation is on pushed tip `9195242`:
  - green `workflow_dispatch` run `24311095925`, including `live-tests`
- `g54` carried the fixed-package auto resolver contract over from
  `InSpectra-Discovery`, kept `Cake.Tool` on the intended native path by
  sharing the `cli opencli` defaults, replaced the unstable Linux `mgcb`
  case with `SaigonMio.Generata`, and rebaselined the accepted discovery
  divergences that still produce correct current outcomes.
- The original zero-BLOCKER/HIGH/MEDIUM stop condition was not reached.
  `g54` expanded the hosted live regression surface and fixed the discovered
  Cake native-invocation regression, but the hosted action/version determinism
  HIGH plus the remaining backend MEDIUM clusters from the post-`g53`
  ledger still remain, so the next fresh swarm should stay focused on the
  dotnet projects.
- Active todo-next queue:
  - no non-completed queued items remain
  - latest hosted-validated phase: `g54` (`9195242`) with green
    `pull_request` run `24311087147` and green `workflow_dispatch` run
    `24311095925`
  - next work item is a fresh dotnet/backend investigation swarm from the
    current tree
  - `TN-2026-04-12-04` completed on `g44` (`99a2c5a`) with green
    `pull_request` run `24301203450`:
    [TodoNext/2026-04-12-playwright-ci-and-e2e-hygiene.md](TodoNext/2026-04-12-playwright-ci-and-e2e-hygiene.md)
  - `TN-2026-04-12-03` completed on `g42`/`g43`
    (`6ccb5b7`, `6bb272d`):
    [TodoNext/2026-04-12-packaged-tool-hook-verification.md](TodoNext/2026-04-12-packaged-tool-hook-verification.md)
  - `TN-2026-04-12-02` completed on `g41`
    (`29a526c`):
    [TodoNext/2026-04-12-installed-tool-process-safety.md](TodoNext/2026-04-12-installed-tool-process-safety.md)
  - `TN-2026-04-12-01` completed on `g40`
    (`8b3c0bc`)

## Current Handoff State

- Source of truth for current open work:
  [Logbook](Logbook.md#current-open-items-after-g54-hosted-validation-2026-04-12)
- Source of truth for how to resume the loop:
  [Runbook](Runbook.md)
- Source of truth for mandatory queued work before the next swarm:
  [Todo Next Queue](TodoNext.md)
- Source of truth for what smells to replay:
  [Smell Catalog](SmellCatalog.md)
- Reusable handoff prompt for the next agent:
  [HandoffPrompt](HandoffPrompt.md)

## Read Order

1. [Runbook](Runbook.md) for the operating brief.
2. [Todo Next Queue](TodoNext.md) for mandatory queued work before the next
   fresh swarm.
3. [Smell Catalog](SmellCatalog.md) for the seed smell library and exceptions.
4. [Logbook](Logbook.md) for shipped history, lessons learned, CI results, and
   the current open-findings ledger.
5. [HandoffPrompt](HandoffPrompt.md) if you want the reusable agent prompt.

## File Map

- [Runbook](Runbook.md)
  - Mission, reference material, orchestration, stop conditions, non-goals,
    subagent policy, CI/push expectations, and final deliverable format.
- [Smell Catalog](SmellCatalog.md)
  - The structural, layering, composition, API, docs, and test smell families
    that future investigation swarms should replay against the tree.
- [Todo Next Queue](TodoNext.md)
  - Mandatory pre-swarm queue for injected or carry-forward tasks, with
    detailed briefs in `TodoNext/`.
- [Logbook](Logbook.md)
  - Iteration-by-iteration findings, full shipped phase summaries, test/CI
    counts, lessons learned, and the still-open HIGH/MEDIUM/LOW findings.
- [HandoffPrompt](HandoffPrompt.md)
  - Reusable stateless prompt that delegates all mutable state to these docs.

## Maintenance

- Update this file when the current validated tip or high-level status changes.
- Append execution history, validation counts, and open-item changes to
  [Logbook](Logbook.md).
- Update [Todo Next Queue](TodoNext.md) whenever a queued task is added,
  started, completed, deferred, or rejected.
- Update [Runbook](Runbook.md) only when the operating procedure changes.
- Update [Smell Catalog](SmellCatalog.md) only when the seed categories or
  explicit exceptions change.
