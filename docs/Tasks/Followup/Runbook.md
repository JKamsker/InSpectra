# Follow-up Runbook

This is the active operating brief for future follow-up iterations. Read this
file together with [README](README.md) and [Smell Catalog](SmellCatalog.md)
before taking action.

## Mission

Between commit `5f9f894` (exclusive) and `3cdc378` (HEAD at the time of the
original brief), the repo received a large architectural restructure. Multiple
classes of code smell were identified and fixed. The job of each follow-up
iteration is to hunt for more instances of the same classes, or new smells of
similar shape, that slipped through because they were outside the direct scope
of each phase.

Do not invent a new agenda from scratch. Replay the established smell
families against the current tree, catch survivors, and fix them using the
same orchestration pattern the prior work used: implementation subagent,
parallel verifier swarm, fix-verify loop, commit, then next phase.

The outer loop is:

`investigation swarm -> fix phase -> validation swarm -> loop validation until clean -> loop investigation`

When a full investigation pass finds zero new smells at MEDIUM severity or
above, and hosted CI is green on the pushed tip, the original mission is done.

## Reference Material

Before taking any action, read every commit between `5f9f894` (exclusive) and
`HEAD`:

```bash
git log --oneline 5f9f894..HEAD
```

At the time of the original brief, that range included 38 commits grouped into:

- Steps 1–11: the initial architectural refactor
- Step 6b: cross-mode cleanup
- Phases 1, 2a, 3: post-refactor cleanup
- Phases A–D: live-test port
- Phases F1–F5: `Feedback1.md` response

For each commit, read the full commit message. The messages document the exact
smell pattern, the fix, and the rationale. Do not skim. Build a mental library
of "smells this repo knows how to name."

Reference docs worth reading:

- `docs/architecture/ARCHITECTURE.md`
- `docs/Tasks/Restructure/Task.md`
- `docs/Tasks/Restructure/Feedback1.md`
- `tests/InSpectra.Gen.Tests/Architecture/*.cs`
- [Smell Catalog](SmellCatalog.md)
- [Logbook](Logbook.md)

## Investigation Seed

The seed smell library lives in [Smell Catalog](SmellCatalog.md). Investigation
swarms should cover every category there and stay alert for nearby variants.
If a genuinely new smell family is discovered, record it in
[Logbook](Logbook.md) and then promote it into the catalog.

## Orchestration

### Outer Loop

Run the outer loop until a fresh investigation pass surfaces zero findings at
MEDIUM severity or above, meaning zero BLOCKER, zero HIGH, and zero MEDIUM.
LOW findings alone do not block the original stop condition, but they must
still be aggregated and either fixed opportunistically or logged explicitly.

There is no hard cap on iterations. Do not stop after an arbitrary number of
rounds, and do not stop just because the previous round was quiet. Fixes from
one iteration can expose new smells in the next, so each round must start from
a fresh investigation swarm on the current post-fix tree rather than from a
cached conclusion.

Each outer iteration:

1. Investigation swarm:
   spawn 6+ parallel read-only subagents, each focused on a different slice of
   the smell categories. Each subagent should report file path, line number,
   category, severity, and a suggested fix shape.
2. Aggregate findings:
   deduplicate across subagents and rank by severity.
   - BLOCKER: breaks the charter or an active architecture test
   - HIGH: layering smell, compile-time cycle risk, dead code
   - MEDIUM: naming drift, stale comments, isolated dependency leaks
   - LOW: documentation, TODO, cosmetic, low-risk hygiene debt
3. Group BLOCKER/HIGH/MEDIUM findings into phases:
   target 3–8 files per phase and do not batch unrelated fixes.
4. For each phase:
   - Spawn one implementation subagent with a precise brief.
   - Read its report and resolve blockers before verification.
   - Spawn 6 verifier subagents with narrow scopes:
     build/test correctness, folder/namespace correctness, consumer-using
     updates, charter alignment, targeted tests, regression sweep.
   - If any verifier fails, spawn a focused fix subagent and rerun the failed
     verifier until two consecutive passes.
   - Commit with the existing phase message style:
     `refactor(arch): phase gN - <short summary>`
5. After all phases in the iteration commit:
   run the full validation swarm across the whole combined diff.
6. If validation is clean:
   start the next outer iteration with a fresh investigation swarm.

### Stop Conditions

The only original stop condition is:

- a fresh full investigation swarm finds zero BLOCKER findings, zero HIGH
  findings, and zero MEDIUM findings on the current post-fix tree, and
- CI on the pushed branch is green, including the `live-tests` job triggered
  via `workflow_dispatch`.

A quiet iteration does not count until it has been followed by a fresh
investigation swarm on the post-fix tree, and that swarm must itself find
nothing at MEDIUM or above.

### Non-goals / Explicit Do-nots

- No feature work. Every change must preserve behavior.
- No Phase 4 project splits. Do not extract `OpenCli` or `Rendering` into
  separate `.csproj` projects in this follow-up.
- No architecture rule tightening without buy-in. Do not add a new
  architecture test class without documenting the rationale.
- No rewriting historical docs such as `Task.md` and `Feedback1.md`.
- No bypassing the verify-fix loop, even for "trivial" fixes.

## Tooling And Conventions

- Use `git mv` for file moves when a single destination file should retain
  history.
- Prefer the dedicated tool for each job: `Glob` for patterns, `Grep` for
  searches, `Read` for focused file reads, shell for build/test/git.
- Parallelize aggressively where the work is independent.
- Each subagent prompt must be self-contained.
- Commit cadence is one phase per commit. Do not squash phases.

## CI / Push Expectations

- Push after each phase commit or at the end of each outer iteration. Pick one
  cadence and stay consistent.
- After each push, watch the `pull_request` CI run. If it fails, fix before
  starting the next iteration.
- Once the final outer iteration finishes, trigger a `workflow_dispatch` run
  to exercise `live-tests`.
- Monitor with `gh run watch <id> --exit-status`. Do not poll in a sleep loop.

## Final Deliverable

When declaring done, report:

1. Summary of findings per outer iteration, including count and categories
   touched.
2. Summary of phases committed with one-line descriptions and commit SHAs.
3. CI run IDs for `pull_request` and `workflow_dispatch`, with conclusions.
4. Any findings explicitly deferred, with rationale.
5. Any new smell categories discovered so the docs can be updated.

The target end state for the original mission is:

- zero BLOCKER/HIGH/MEDIUM smells detectable by a fresh investigation swarm,
- `15` architecture policy tests green,
- `319` unit tests green, and
- the hosted `35` live tests green in CI on the final pushed tip.
