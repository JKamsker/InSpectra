# Reusable Handoff Prompt

Use this prompt as-is for the next agent. It is intentionally stateless: the
prompt delegates all mutable state to the follow-up docs in this folder.

```text
You are picking up the InSpectra follow-up “infinite loop” task.

Before taking action, read the follow-up docs top to bottom in this order:
1. docs/Tasks/Followup/README.md
2. docs/Tasks/Followup/Runbook.md
3. docs/Tasks/Followup/SmellCatalog.md
4. docs/Tasks/Followup/Logbook.md

Important:
- The old path docs/Tasks/Restructure/Followup.md is only a redirect stub.
  Do not use it as the source of truth.
- Treat the Followup doc set as the source of truth for:
  - current branch/tip context
  - latest validated tip and CI state
  - current open HIGH/MEDIUM/LOW findings
  - historical false positives that must not be re-raised
  - the active stop condition and orchestration pattern
- Resume the original indefinite outer loop from the current tree. Do not stop
  after one iteration unless the documented stop condition is actually met or
  the user explicitly overrides it.
- LOW findings do not block the stop condition, but they must still be
  aggregated and recorded. Do not ignore LOW findings.
- Use gpt-5.4 with high reasoning for subagents if available, and spawn
  subagents with fork_context: false.
- Follow the documented orchestration pattern:
  investigation swarm -> aggregate/rank -> phases of 3–8 files ->
  implementation subagent per phase -> 6-verifier swarm per phase ->
  fix-verify loop -> commit per phase -> push -> hosted CI ->
  fresh investigation swarm.
- Update the follow-up docs as the work progresses:
  - update Logbook.md for history, counts, findings, and lessons
  - update README.md if the high-level current state changes
  - update Runbook.md or SmellCatalog.md only if the procedure or smell
    library changes

Do not re-raise any false positive or intentional exception already recorded in
the follow-up docs unless you have genuinely new evidence.

When you finish, provide:
1. Summary of findings per outer iteration.
2. Summary of phases committed with SHAs.
3. CI run IDs (pull_request + workflow_dispatch) with conclusions.
4. Any findings explicitly deferred, with rationale.
5. Any new smell categories discovered.

Before editing, verify the current branch, HEAD, and worktree yourself in case
they moved since the docs were last updated.
```
