# Follow-up: hunt and fix similar code smells

> **Intended audience**: a fresh Claude Code / autonomous agent session with no
> carry-over context from the prior restructuring work. Read this file top to
> bottom, then execute.

## Mission

Between commit `5f9f894` (exclusive) and `3cdc378` (HEAD at the time of this
doc), the repo received a large architectural restructure. Multiple **classes
of code smell** were identified and fixed. Your job is to hunt for **more
instances of the same classes** — or **new smells of similar shape** — that
slipped through because they were outside the direct scope of each phase.

You do **not** need to invent new categories from scratch. The categories to
look for are already demonstrated by the fix commits. Replay them against the
current tree, catch survivors, and fix them using the same orchestration
pattern the prior work used (implementation subagent → parallel verifier
swarm → fix-verify loop → commit → next).

The outer loop is: **investigation swarm → fix phase → validation swarm →
loop validation until clean → loop investigation**. When a full investigation
pass finds zero new smells, stop.

## Reference material — study before starting

Before taking any action, **read every commit** between `5f9f894` (exclusive)
and `HEAD`:

```bash
git log --oneline 5f9f894..HEAD
```

At the time of writing, that range includes 38 commits grouped into:

- Steps 1–11 — the initial architectural refactor
- Step 6b — cross-mode cleanup
- Phases 1, 2a, 3 — post-refactor cleanup
- Phases A–D — live-test port
- Phases F1–F5 — Feedback1.md response

For each commit, read the full commit message. The messages document the
exact smell pattern, the fix, and the rationale. Do **not** skim. Build a
mental library of "smells this repo knows how to name." Your investigation
quality depends on pattern recognition from these messages.

Reference docs also worth reading:

- `docs/architecture/ARCHITECTURE.md` — the charter that the fixes enforce
- `docs/Tasks/Restructure/Task.md` — historical plan (has a banner marking it
  as such)
- `docs/Tasks/Restructure/Feedback1.md` — the feedback that drove phases F1–F5
- `tests/InSpectra.Gen.Tests/Architecture/*.cs` — the 10 active policy tests
  that the charter is enforced by

## Known smell categories (from the reference commits)

This list is your **seed**. Your investigation must cover every category here
and be open to new variants you spot along the way.

### Structural

1. **Forbidden top-level buckets** inside any project root — `Runtime`,
   `Infrastructure`, `Models`, `Support`, `Helpers`, `Misc`. Enforced by
   `ArchitectureForbiddenBucketsTests` at the project level, but a sub-folder
   named `Support/` one level in is still a smell worth auditing.
2. **Mini-misc buckets** — folders mixing two different ownership ideas (e.g.
   the F1 `Targets/` and `Execution/` split). Look for folders where half the
   files belong to concept A and half to concept B.
3. **Duplicate semantic roots** — the same concept (`OpenCli`, `Execution`,
   `Documents`) as a folder name in multiple unrelated branches. Charter says
   "Do not repeat semantic roots unless they truly mean the same owned
   concept." The F2 canonical-OpenCli merge is a reference fix.
4. **Flat mode / flat module subtrees** — a mode or logical module whose
   files all live at one level while its siblings have substructure. Phase F4
   fixed `Modes/Hook/` this way. Check all other `Modes/*/` and any other
   "peer sibling" sets for similar drift.
5. **Namespace ↔ folder mismatches** — the active architecture test catches
   these at build time, but only for `.cs` files. Check for namespaces
   declared in block-scoped form, or files that deliberately avoid namespace
   declaration (like `StartupHook.cs`) to see if any new exceptions drifted in.
6. **Junk multi-type files** — the charter explicitly bans `*Models.cs` type
   dumping grounds. Grep for files defining more than one top-level
   `class`/`record`/`interface` where the types are not tightly coupled.

### Dependency / layering

7. **Cross-mode imports** — enforced by a test, but run the raw grep anyway:
   ```
   grep -rn "using InSpectra\.Gen\.Acquisition\.Modes\.\(Help\|CliFx\|Hook\|Static\)" \
     src/InSpectra.Gen.Acquisition/Modes/
   ```
   Every hit whose owning mode ≠ imported mode is a violation the test should
   catch, but a new mode subfolder or a conditional import could slip past.
8. **Tooling → Modes** — enforced. Same gut check.
9. **Contracts → Tooling / Contracts → Modes** — enforced. Same gut check.
10. **Cycles inside Gen** between `Commands`, `UseCases`, `Rendering`,
    `OpenCli`, `Output`, `Execution`. The F2 fix documents the pattern. The
    architecture tests do **not** catch cycles inside `Gen` — only the
    cross-module ones. You must grep manually. A good starting heuristic:
    ```
    # A depending on B where B depends on A
    for pair in OpenCli:Rendering Rendering:UseCases UseCases:Commands Rendering:OpenCli Commands:UseCases ; do
      a=${pair%:*} ; b=${pair#*:}
      grep -l "using InSpectra.Gen.$b" src/InSpectra.Gen/$a/**/*.cs 2>/dev/null | head -5
    done
    ```
11. **App-shell → deep internals** — active test enforces `InSpectra.Gen`
    may only import from `InSpectra.Gen.Acquisition.Composition` and
    `Contracts`. But check `InSpectra.Gen` → `InSpectra.Gen.StartupHook` for
    a similar drift — there's no active test for that edge.
12. **Dead `using` statements** — always a tell. After a move, unused usings
    point at the old namespace and grep for them will surface stale paths
    that should be cleaned up. `dotnet build` won't fail; a regex scan will.

### Composition / wiring

13. **Overbroad composition seams** — `AddInSpectra*()` methods registering
    services that don't belong to the module they name. F3's OpenCli seam
    narrowing is the reference. Audit each of the 5 seams:
    - `AddInSpectraOpenCli`
    - `AddInSpectraGenerateUseCases`
    - `AddInSpectraRendering`
    - `AddInSpectraAcquisition`
    - `AddTargetServices` (still private in `InSpectra.Gen/Composition`)
    For each, every type registered must live inside that module.
14. **Non-test `InternalsVisibleTo`** — enforced by a test. Also check each
    `AssemblyInfo.cs` for unexpected attributes.
15. **`[ModuleInitializer]` with `CA2255` suppression** — Phase 2a uses this
    as a deliberate workaround for a layering constraint. Every other
    occurrence of `CA2255` in the codebase needs a documented justification.
16. **DI registrations duplicating types** — if a type is registered in two
    different composition methods, only the last registration wins. Grep for
    duplicates across all `ServiceCollectionExtensions` files.

### Type / API smells

17. **Type erasure with `object` plus runtime cast** — Phase 2a's
    `CliFrameworkProvider.StaticAnalysisFrameworkAdapter.Reader : object`
    is a charter-motivated workaround. Any other `object` field whose value
    is immediately cast to a typed interface at every use site is a smell.
    Either the type can be strengthened, or the cast needs a rationale doc
    comment explaining why.
18. **Missing `CancellationToken` on async methods** — scan public async
    methods. The charter doesn't explicitly require `CancellationToken`
    propagation but the C# coding-style rule in
    `~/.claude/rules/csharp/coding-style.md` does. Any `public Task<...>`
    that lacks a `CancellationToken` parameter is a smell.
19. **Synchronous I/O on async paths** — `File.ReadAllText`, `File.Exists`
    from inside an async method is almost always a bug. `await
    File.ReadAllTextAsync(...)` is the fix unless the caller explicitly wants
    the sync-over-async behavior.
20. **Silent catch blocks / swallowed exceptions** — `catch (Exception)`
    followed by empty block or vague logging. Each one needs to either
    rethrow or produce a structured error. Reference: `agent` rule
    `silent-failure-hunter`.
21. **`*Service` naming for types that aren't services** — or `*Support`
    naming for types that are really domain methods. Rename or split.
22. **Multiple unrelated top-level types per file** — split into one file
    per primary type. Exception: tiny private DTO clusters with a strong
    reason to stay inline.
23. **Public types that should be `internal`** — anything inside a module
    that isn't exposed through `Composition/` or `Contracts/` should be
    `internal`. Grep for `public sealed class` inside `Modes/*`, `Tooling/*`,
    and equivalent. Cross-reference against `InternalsVisibleTo` — if the
    type is only consumed by the test assembly, `internal` is correct.

### Documentation / test hygiene

24. **Stale comments** — post-restructure, any comment that says "currently
    skipped", "will be done in phase N", "moved to X" is stale. Grep for
    these phrases and update or delete.
25. **Dead code** — methods or entire files that are never called. Run a
    static analyzer (`dotnet tool run dotnet-ts-prune` has a .NET analog) or
    grep for method names with zero callers.
26. **Stale XML doc comments** — `<see cref="..."/>` pointing at types that
    moved or were deleted. `dotnet build` may emit warnings for these; check
    the build log.
27. **TODO / FIXME / HACK comments** — enumerate them. Each one either has a
    tracked issue, or it's stale and can be deleted, or it's a real
    outstanding item that warrants a fix.

### Test / fixture smells

28. **Stale snapshot fixtures** — the Phase D fixture regeneration found 11
    fixtures that had drifted from the actual output. Run the live suite
    with `INSPECTRA_LIVE_UPDATE_SNAPSHOTS=1` in a scratch branch; any file
    that changes is a smell. Either the fixture or the code is stale.
29. **Trivially green tests** — tests whose assertion can't fail because the
    enumeration returns zero items. Guard each active test with a count
    assertion (the existing architecture tests already do this — use them as
    reference).
30. **Orphan test data** — files under `tests/.../TestData/` or similar that
    are never read by any test. Use grep to correlate.

## Orchestration

### Outer loop: investigate → fix → validate → loop

Max 3 outer iterations. Stop earlier if an investigation pass finds zero new
smells.

Each outer iteration:

1. **Investigation swarm** — spawn 6+ parallel read-only subagents, each
   focused on a different slice of the smell categories above. Give each
   subagent a precise brief; don't overlap too much. Each subagent writes a
   short structured report listing findings with file path, line number,
   category, and suggested fix approach.
2. Aggregate findings. De-duplicate across subagents. Rank by severity:
   - **BLOCKER** — breaks the charter or an active architecture test
   - **HIGH** — layering smell, compile-time cycle risk, dead code
   - **MEDIUM** — naming drift, stale comments, isolated dep leaks
   - **LOW** — documentation, TODO, cosmetic
   Drop LOW findings unless they're cheap and incidental to a higher fix.
3. Group HIGH and BLOCKER findings into **phases** — 3–8 files per phase is
   a good target. Don't batch unrelated fixes.
4. **For each phase**:
   a. Spawn an **implementation subagent** with a precise brief, including:
      - Files to touch
      - Specific moves / edits
      - Expected final state
      - Validation gates (`dotnet build`, `dotnet test`, specific greps)
      - Charter constraints to honor
      - Explicit `do not touch` list
   b. Read the implementation subagent's report. If it reports a blocker,
      resolve it before verification.
   c. Spawn **6 parallel verifier subagents**, each with a narrow scope:
      - build/test correctness
      - folder / namespace correctness
      - consumer-using update correctness
      - charter alignment
      - targeted test filter (hit the specific tests exercising the change)
      - regression sweep (git status cleanliness, no stray files, csproj
        untouched)
   d. If any verifier reports FAIL, spawn a **fix subagent** for that
      specific finding. Then re-run the verifier that failed. Loop until 2
      consecutive passes.
   e. Commit with a structured message following the existing commit format:
      `refactor(arch): phase gN — <short summary>` and a body that explains
      the smell, the fix, the validation counts, and any deferrals.
5. **After all phases in this iteration commit**, run the full validation
   swarm (same 6 verifiers but scope expanded to the whole iteration's
   combined diff).
6. If validation is clean, **start the next outer iteration**: a fresh
   investigation swarm. The goal is to catch smells that were invisible
   until the current iteration's fixes landed.

### Stop conditions

- Investigation finds **zero** HIGH/BLOCKER findings across all subagents.
- 3 outer iterations have completed.
- A CI run on the pushed branch is green including `live-tests` (via
  `workflow_dispatch`).

### Non-goals / explicit do-nots

- **No feature work.** Every change must preserve behavior. Tests must stay
  at 280 / 0 / 0 on the gate-off run and 35 / 0 / 0 on the gate-on live run
  (or adjust up if genuinely new tests are added, with equivalent pass
  count).
- **No Phase 4 project splits.** Do not extract `OpenCli` or `Rendering` into
  separate `.csproj` projects. The charter says Phase 4 happens only when
  dependency direction is genuinely clean, and that's a judgment call to
  defer.
- **No architecture rule tightening without buy-in.** Don't add a new
  architecture test class without documenting the rationale in the commit.
  Use existing tests as templates.
- **No rewriting historical docs.** `docs/Tasks/Restructure/Task.md` and
  `Feedback1.md` are historical records. Update only `docs/architecture/
  ARCHITECTURE.md` if the charter actually needs to change.
- **No bypassing the verify-fix loop.** Even for "trivial" fixes, run the
  verifier swarm. The prior refactor found real bugs in "trivial" moves
  (Phase B `HookOpenCliBuilder` null argument name).

## Tooling and conventions

- Use `git mv` for every file move (preserves history).
- Prefer the dedicated tool for each job: `Glob` for file patterns, `Grep`
  for code searches, `Read` for single-file reads. Reserve `Bash` for shell
  operations (build, test, git).
- Parallelize aggressively where independent. The investigation swarm should
  always be 6+ parallel calls in one message.
- Each subagent prompt must be self-contained — assume zero context.
- Commit cadence: one phase per commit. Do not squash phases. Do not
  interleave phases.

## CI / push expectations

- Push after each phase commit OR at the end of each outer iteration —
  pick one cadence and stick with it. Pushing per-phase is safer; pushing
  per-iteration is faster.
- After each push, watch the pull_request CI run (`build-test` job). If it
  fails, fix before starting the next iteration.
- Once the final outer iteration finishes, trigger a `workflow_dispatch`
  run to exercise the live-tests job. It must be green before declaring
  done.
- Monitor via `gh run watch <id> --exit-status`. Do not poll in a sleep
  loop.

## Final deliverable

When you declare done:

1. Summary of findings per outer iteration (count, categories touched).
2. Summary of phases committed with one-line descriptions and commit SHAs.
3. CI run IDs (pull_request + workflow_dispatch) with conclusions.
4. Any findings explicitly deferred, with rationale.
5. Any **new** smell categories you discovered (not in the seed list above)
   so this doc can be updated.

The target end state is: **zero HIGH/BLOCKER smells detectable by a fresh
investigation swarm**, plus all 10 architecture policy tests + 280 unit
tests + 35 live tests green in CI.

Good hunting.
