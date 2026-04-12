# Follow-up Docs

This folder replaces the old monolithic
`docs/Tasks/Restructure/Followup.md`.

## Current Status

- Working branch: `feat/merge-tool`
- Current follow-up docs live on the branch tip; verify `HEAD` before
  resuming work.
- Latest fully validated pushed tip: `99a2c5a`
- Seven outer iterations shipped phases `g1`–`g39` on `feat/merge-tool`, and
  the queue-driven thin-shell phase `g40`, the installed-tool
  process-safety phase `g41`, the packaged-tool verification phase `g42`, and
  the hosted follow-up fix `g43` and the Playwright hosted-CI phase `g44` are
  now pushed and hosted validated.
- The pushed tip `99a2c5a` is hosted validated:
  - `30` frontend unit tests
  - `12` Playwright E2E tests
  - `354 / 0 / 0` backend unit tests
  - `17` architecture policy tests
  - green `pull_request` run `24301203450`
- The latest green `workflow_dispatch` validation is still on pushed tip
  `a3390bb`:
  - green `workflow_dispatch` run `24296167355`, including `live-tests`
- Outer iteration 10 fresh-swarm wave 1 restarted from `99a2c5a` and
  converged on the remaining pre-existing HIGH/MEDIUM clusters.
- The original zero-BLOCKER/HIGH/MEDIUM stop condition was not reached.
  `g44` closed the hosted Playwright CI HIGH plus its adjacent local E2E
  mediums, but multiple other ranked HIGH/MEDIUM clusters remain open after
  the fresh post-`99a2c5a` wave-1 ranking.
- Active todo-next queue:
  - no non-completed queued items remain
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
  [Logbook](Logbook.md#current-open-items-after-g44-fresh-swarm-wave-1-2026-04-12)
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
