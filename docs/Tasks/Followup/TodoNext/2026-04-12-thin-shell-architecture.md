# Todo Next: Finalize the InSpectra.Gen Thin-Shell Architecture

- ID: `TN-2026-04-12-01`
- State: `Completed`
- Added: `2026-04-12`
- Source: user-injected task via `docs/Tasks/Followup/tmp.txt`
- Queue entry: [Todo Next Queue](../TodoNext.md)
- Pickup rule: this item must be triaged before the next fresh investigation
  swarm begins.
- Exit rule: mark the queue item `Completed`, `Deferred`, or `Rejected` with
  dated rationale and update [Logbook](../Logbook.md) accordingly.
- Completed locally on `2026-04-12` in phase `g40`
  (`8b3c0bc7e3e20a4df4897b253e9432fbdaf844cc`).
- Validation on that local tip:
  - `dotnet test InSpectra.Gen.sln` ✅ (`325 / 0 / 0`)
  - architecture filter ✅ (`17 / 0 / 0`)
  - targeted engine architecture slice ✅ (`3 / 0 / 0`)
- Resume point after completion: continue with the next fresh investigation
  swarm from `8b3c0bc7e3e20a4df4897b253e9432fbdaf844cc` after push / hosted CI.

## Normalized Brief

You are working on a C#/.NET solution that has already gone through a substantial structural refactor.

## Goal

Finish the architectural cleanup by enforcing this rule:

* `InSpectra.Gen` must be a **thin CLI shell**
* everything that is not **UI / CLI plumbing** must live behind the shell in the backend module
* the current backend project is named `InSpectra.Gen.Acquisition`, but its responsibility has grown beyond “acquisition”
* therefore, the backend project should be treated as the **engine** and renamed accordingly unless there is a strong technical reason not to

The desired end state is:

* `InSpectra.Gen` = app shell / command-line adapter layer
* backend module = normalized product logic
* `InSpectra.Gen.Core` = tiny shared kernel only
* `InSpectra.Gen.StartupHook` remains separate
* architecture docs and tests must match the final structure

## Important context

A previous refactor already improved the repo a lot. The old repo-wide smell was:

* no single dominant organizing axis
* folders answered different questions in different places
* feature hierarchy, layer hierarchy, and strategy hierarchy were mixed together
* app shell imported deep backend internals

That broad smell is mostly fixed.

The current state is **structurally good**, but there are still unresolved ownership seams.

### Current architectural direction

The correct design direction is:

* raw CLI command parsing, environment-variable interpretation, shell output concerns, and shell composition stay in `InSpectra.Gen`
* normalized requests, orchestration, OpenCLI domain logic, rendering, target/source materialization, process execution, workspace handling, and other product logic belong in the backend module

### Very important nuance

Do **not** use a simplistic rule like “move anything that does not print to console.”

Use this actual boundary:

* shell owns **raw CLI/env semantics**
* backend owns **normalized application/product logic**

Examples:

#### Should stay in shell

Things that interpret command-line options, shell defaults, env vars, current working directory, or map CLI settings into normalized requests.

Typical examples:

* `Program.cs`
* `Commands/**`
* `Output/**`
* shell composition/root wiring
* command/request mapping helpers
* command value resolution
* CLI option parsing support
* project argument resolution if it is primarily usage-facing and command-line oriented

#### Should move to backend

Things that perform real product work after command values are normalized.

Typical examples:

* `OpenCli/**`
* `Rendering/**`
* generate/render orchestration
* target/source factories
* process execution helpers
* executable discovery
* workspace creation
* dotnet build output resolution
* package/local/native acquisition work
* normalized generation/rendering pipelines

## Known remaining issues from prior review

These were previously identified and should be explicitly addressed:

1. `InSpectra.Gen` still contains backend logic instead of just shell logic.
2. `InSpectra.Gen.Acquisition` is no longer just acquisition; it acts like an engine/backend.
3. There was a local smell where some command/request helpers were placed under a render-specific namespace/folder even though they were generic CLI value resolution helpers.
4. `Rendering/Contracts` and `Rendering/Pipeline` had a dependency knot around types like `IDocumentRenderService` and `AcquiredRenderDocument`.
5. `Targets` and `UseCases/Generate` had unresolved bidirectional ownership due to things like build settings and source resolution helpers.
6. Docs and architecture tests no longer fully matched the code.
7. Some backend concerns had duplicated workspace/process behavior because they could not depend cleanly on the right layer.

Also note this key point from the latest review:

* there used to be a blocker because rendering reused shared process helpers
* that blocker is now basically gone because rendering has its own local bundle process support
* so process/workspace/source/native concerns can now be moved behind the shell more cleanly than before

## Primary task

Perform a complete architectural fixup so the codebase clearly reflects a thin-shell design.

## Required outcomes

### 1. Make `InSpectra.Gen` a true shell

Keep only shell concerns in `InSpectra.Gen`.

This should include:

* `Program.cs`
* CLI commands
* command settings / command option types
* shell output formatting / output publishing
* shell composition/root registration
* shell-side request mapping from raw command settings to normalized backend requests
* env var / current working directory / CLI default resolution
* other purely command-line adapter logic

Anything beyond that should be moved out.

### 2. Move backend logic out of `InSpectra.Gen`

Move all non-shell logic behind the shell into the backend module.

This includes, unless a concrete code reason proves otherwise:

* OpenCLI logic
* rendering logic
* generation orchestration
* acquisition orchestration
* process execution helpers
* workspace helpers
* target/source factories
* materialization/build-output resolution
* normalized request/response contracts for backend execution
* any product/domain services that are not specifically about CLI adaptation

### 3. Rename the backend module

Because the current project name `InSpectra.Gen.Acquisition` is now misleading, rename it to something consistent with its real role.

Preferred name:

* `InSpectra.Gen.Engine`

A similarly precise name is acceptable only if there is a compelling reason.

After renaming:

* update project names
* update namespaces
* update references
* update solution files
* update docs
* update architecture tests
* update any comments and composition registration names

### 4. Resolve the shell/backend boundary cleanly

Do not allow shell-specific concepts to leak into backend contracts.

In particular, review backend-side request/result models for shell leakage such as:

* `Quiet`
* `Verbose`
* `NoColor`
* shell-specific summary text
* stdout-specific transport decisions embedded in backend contracts

Backend contracts should describe product work, not CLI UX policy.

If needed:

* split shell-facing options from backend execution options
* make backend results transport-agnostic
* keep human-facing summaries in shell adapters, not backend core flows

### 5. Resolve local naming/placement drift

Fix cases where code is placed or named by one feature but is actually generic/shared.

Especially:

* generic command value resolution helpers should not live in a render-specific area unless they are genuinely render-specific
* `Contracts` folders should actually mean contracts, or be renamed/split if they are really shared foundation/behavior
* avoid introducing new “misc” or “support” junk drawers unless the name is truly justified and the scope is tight

### 6. Break remaining dependency knots

Resolve any remaining bidirectional or semantically inverted dependencies, especially around:

* rendering contracts vs rendering pipeline
* targets/sources vs generate/use-case DTOs
* any shell-to-engine or engine-to-shell inversion

The dependency direction should tell the same story as the folder/project layout.

### 7. Remove duplicate implementations caused by bad boundaries

If the repo currently contains duplicated helpers or parallel implementations because code could not be referenced across the boundary, consolidate them as part of the new engine structure.

Examples to look for:

* temporary workspace helpers
* process support wrappers
* target/source execution helpers
* orchestration glue that exists twice because of project boundaries

### 8. Make the documentation and architecture tests truthful

Update the architecture docs and tests to reflect the final design.

That includes:

* project/module descriptions
* diagrams if present
* allowed dependency directions
* naming conventions
* architecture tests
* comments that still describe the previous transition state

Docs/tests must describe the final structure, not the old migration plan.

## Constraints

### Preserve behavior

This is a structural refactor, not a feature rewrite.

Do not intentionally change functional behavior unless required to complete the refactor safely.

### Avoid churn for its own sake

Do not create artificial folder depth or overly ceremonial abstractions just for symmetry.

Only move/split/rename where it improves ownership clarity.

### Keep `InSpectra.Gen.Core` tiny

Do not dump random shared code into Core.

Only place code in Core if it is a real shared kernel with stable, low-level, broadly reusable abstractions.

Core must not become a “misc shared” escape hatch.

### Keep `StartupHook` separate

Do not fold startup-hook concerns into the engine unless there is an unavoidable technical reason.

### Prefer a clean end state over halfway measures

Do not stop at “Acquisition now contains everything but still has the wrong name and wrong docs.”

If the backend becomes the engine, complete that move consistently.

## Design guidance

Use the following mental model.

### Shell

The shell is an adapter from:

* command-line args
* Spectre/CLI command settings
* env vars
* current process context
* user-facing output policy

into:

* normalized engine requests

It also adapts engine results back into:

* stdout/stderr
* files
* structured CLI output
* exit codes
* user-facing summaries

### Engine

The engine owns:

* acquisition/generation/rendering flows
* OpenCLI model/domain handling
* orchestration
* process execution
* source/target materialization
* build/package/native analysis
* backend contracts and normalized data flow

### Core

Core owns only stable primitives genuinely shared across projects.

## Concrete review checklist

Use this checklist while making changes.

### Shell project should not own:

* rendering implementations
* OpenCLI domain implementations
* process runners
* executable resolvers
* workspaces
* target/source materialization
* package/native/local acquisition internals
* backend orchestration services

### Shell project may own:

* command classes
* command settings
* CLI-specific validation
* env-var handling
* current-directory/default resolution
* shell composition
* shell output policies
* mapping from raw CLI settings to engine requests/results

### Engine project should not depend on shell:

* no `Commands`
* no shell output abstractions unless intentionally modeled as a generic interface at a lower layer
* no Spectre-specific command types
* no CLI-only request settings
* no shell UX flags in core execution models unless explicitly separated as adapter-layer metadata

### Folder/package names should answer one clear question

Prefer stable ownership-oriented buckets over mixed semantic buckets.

## Deliverables

1. Implement the structural refactor.
2. Rename the backend project to `InSpectra.Gen.Engine` unless blocked by a concrete reason.
3. Update namespaces/usings/project references accordingly.
4. Update architecture docs and tests.
5. Build and run tests if available.
6. Provide a concise summary of:

   * what moved
   * what was renamed
   * what dependency knots were resolved
   * any places where you intentionally left something in the shell and why
   * any follow-up items that remain and why they were not addressed now

## Acceptance criteria

The task is successful if, after completion:

* `InSpectra.Gen` reads clearly as a CLI shell
* the backend project reads clearly as an engine, not merely “acquisition”
* non-shell product logic is no longer stranded in the shell project
* project/folder names match actual responsibilities
* dependency directions match the architectural story
* docs/tests describe the final state truthfully
* there are no obvious new junk-drawer folders
* there is no major duplicated process/workspace/source logic caused by the old boundary
* the solution builds and tests pass, or any failures are clearly explained

## Preferred implementation approach

1. Identify shell-only code and freeze it in place.
2. Identify all backend/product logic still living in `InSpectra.Gen`.
3. Rename `InSpectra.Gen.Acquisition` to `InSpectra.Gen.Engine`.
4. Move backend logic into the engine in coherent subareas.
5. Repair namespaces and references.
6. Fix dependency inversions and contract leakage.
7. Consolidate duplicated helpers caused by the prior split.
8. Update docs and architecture tests.
9. Build/test and report results.

## Output format expected from the agent

At the end, report:

* final module layout
* major moves/renames
* why each remaining shell-side piece belongs in shell
* any unresolved tradeoffs
* build/test status

Do not give a vague summary. Be explicit about the architectural decisions.
