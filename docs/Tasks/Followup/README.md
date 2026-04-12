# Follow-up Docs

This folder replaces the old monolithic
`docs/Tasks/Restructure/Followup.md`.

## Current Status

- Working branch: `feat/merge-tool`
- Current docs tip: `fb4d5f8` (`docs(tasks): split followup docs into runbook and logbook`)
- Latest fully validated code tip: `a3390bb`
- Seven outer iterations shipped phases `g1`–`g39` on `feat/merge-tool`.
- The pushed tip `a3390bb` is locally and hosted validated:
  - `319 / 0 / 0` unit tests
  - `15` architecture policy tests
  - targeted live NuGet API slice `3 / 0 / 0`
  - green `pull_request` run `24296163756`
  - green `workflow_dispatch` run `24296167355`, including `live-tests`
- The original zero-BLOCKER/HIGH/MEDIUM stop condition was not reached.
  The final fresh swarm still left one HIGH and several MEDIUM findings open,
  and the outer loop stopped only because the user explicitly ended it after
  iteration 7.

## Current Handoff State

- Source of truth for current open work:
  [Logbook](Logbook.md#still-open-items-after-iteration-7)
- Source of truth for how to resume the loop:
  [Runbook](Runbook.md)
- Source of truth for what smells to replay:
  [Smell Catalog](SmellCatalog.md)
- Reusable handoff prompt for the next agent:
  [HandoffPrompt](HandoffPrompt.md)

## Read Order

1. [Runbook](Runbook.md) for the operating brief.
2. [Smell Catalog](SmellCatalog.md) for the seed smell library and exceptions.
3. [Logbook](Logbook.md) for shipped history, lessons learned, CI results, and
   the current open-findings ledger.
4. [HandoffPrompt](HandoffPrompt.md) if you want the reusable agent prompt.

## File Map

- [Runbook](Runbook.md)
  - Mission, reference material, orchestration, stop conditions, non-goals,
    CI/push expectations, and final deliverable format.
- [Smell Catalog](SmellCatalog.md)
  - The structural, layering, composition, API, docs, and test smell families
    that future investigation swarms should replay against the tree.
- [Logbook](Logbook.md)
  - Iteration-by-iteration findings, full shipped phase summaries, test/CI
    counts, lessons learned, and the still-open HIGH/MEDIUM/LOW findings.
- [HandoffPrompt](HandoffPrompt.md)
  - Reusable stateless prompt that delegates all mutable state to these docs.

## Maintenance

- Update this file when the current validated tip or high-level status changes.
- Append execution history, validation counts, and open-item changes to
  [Logbook](Logbook.md).
- Update [Runbook](Runbook.md) only when the operating procedure changes.
- Update [Smell Catalog](SmellCatalog.md) only when the seed categories or
  explicit exceptions change.
