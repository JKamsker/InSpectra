You are picking up the InSpectra follow-up “infinite loop” task.

Before taking action, read the follow-up docs top to bottom in this order:
1. docs/Tasks/Followup/README.md
2. docs/Tasks/Followup/Runbook.md
3. docs/Tasks/Followup/TodoNext.md
4. docs/Tasks/Followup/SmellCatalog.md
5. docs/Tasks/Followup/Logbook.md

Important:
- The old path docs/Tasks/Restructure/Followup.md is only a redirect stub.
  Do not use it as the source of truth.
- Treat the Followup doc set as the source of truth for:
  - current branch/tip context
  - latest validated tip and CI state
  - mandatory queued work in TodoNext.md
  - current open HIGH/MEDIUM/LOW findings
  - historical false positives that must not be re-raised
  - the active stop condition and orchestration pattern
- Before starting the next fresh investigation swarm, process every non-completed
  item in docs/Tasks/Followup/TodoNext.md. Do not silently skip queued work.
- Resume the original indefinite outer loop from the current tree. Do not stop
  after one iteration unless the documented stop condition is actually met or
  the user explicitly overrides it.
- LOW findings do not block the stop condition, but they must still be
  aggregated and recorded. Do not ignore LOW findings.
- Use gpt-5.4 with high reasoning for all subagents if available, and spawn
  subagents with fork_context: false.
- Read-only / research subagents may be parallelized, with a standard cap of
  6 in one wave.
- Write-capable subagents must be serialized. Do not run multiple write
  subagents against the repo at the same time.
- Research waves are adaptive, not fixed:
  - start with one wave of up to 6 read-only subagents
  - prompts should be diverse by slice, but overlap is acceptable
  - spawn a new wave only while the findings remain materially diverse
  - if wave 1 mostly converges on the same findings, stop there and move to
    fixing
  - if wave 2 mostly repeats wave 1, do not run wave 3
  - hard cap: 4 research waves per outer loop
- Follow the documented orchestration pattern:
  investigation swarm -> aggregate/rank -> phases of 3–8 files ->
  implementation subagent per phase -> 6-verifier swarm per phase ->
  fix-verify loop -> commit per phase -> push -> hosted CI ->
  fresh investigation swarm.
- Update the follow-up docs as the work progresses:
  - update TodoNext.md when queued work is added, started, completed, deferred,
    or rejected
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
