# InSpectra Architecture Charter

> **Status:** Proposed Phase 1 charter, 2026-04-10.
>
> This document defines the intended module boundaries, dependency direction, and placement rules for the backend restructure. It is intentionally a charter, not a full migration map, test plan, or implementation spec.

## Core principle

**Capability first, variant second, mechanism third.**

When a tree needs more shape, prefer:

**product -> capability -> variant -> stage**

The goal is stable seams for growth. New work should land in a known owner instead of introducing a new top-level bucket.

## Module charter

### `InSpectra.Gen` — app shell

Owns the `inspectra` program as a thin CLI adapter:

- `Program.cs`
- command parsing and command settings
- raw CLI / environment-variable / working-directory interpretation
- shell output shaping for human and JSON modes
- shell composition
- packaging and startup-hook integration
- usage-facing helper code such as command-side project resolution

It does **not** own normalized product logic after command values are resolved.
That work belongs in the engine.

A charter-aligned shape is:

```text
src/InSpectra.Gen/
  Program.cs
  Commands/
    <Capability>/
      <Variant>/
  Output/
  Targets/
    Inputs/
  Composition/
```

### `InSpectra.Discovery.Tool` — discovery backend shell

Owns discovery-specific orchestration and CLI workflows that sit on top of the
shared backend:

- queue planning and backfill workflows
- discovery analysis orchestration
- promotion flows and notes generation
- repository docs/index generation
- discovery-specific machine output and summaries

It may depend on `InSpectra.Lib` for shared contracts, composition, and
intentionally exposed `Tooling/*` helpers, plus `InSpectra.Gen.StartupHook` for
startup-hook asset packaging. It does not own duplicated copies of shared lib
tooling.

### `InSpectra.Gen.Engine` — normalized backend

Owns the product logic behind the shell:

- OpenCLI domain model, schema, validation, serialization, and enrichment
- target acquisition across exec / package / dotnet flows
- acquisition modes such as help, CliFx, static, and hook
- shared tooling such as process, packages, NuGet, framework detection, and workspaces
- rendering from OpenCLI into Markdown and HTML
- normalized generate/render use cases
- engine composition and orchestration

The engine contains several logical submodules, but they stay in one assembly for
this follow-up:

```text
src/InSpectra.Gen.Engine/
  Contracts/
  OpenCli/
  Modes/
    <Mode>/
  Tooling/
  Execution/
  Targets/
    Inputs/
    Sources/
  Rendering/
    Contracts/
    Pipeline/
    Markdown/
    Html/
      Bundle/
  UseCases/
    Generate/
  Orchestration/
  Composition/
```

The key ownership rule is:

- shell owns raw CLI semantics
- engine owns normalized application and product semantics

### Engine logical modules

Inside `InSpectra.Gen.Engine`, the main logical owners are:

- `OpenCli/` for the canonical OpenCLI document domain
- `Modes/`, `Tooling/`, `Execution/`, and `Targets/` for acquisition and materialization
- `Rendering/` for OpenCLI-to-doc transforms
- `UseCases/Generate/` for normalized acquisition/generation orchestration
- `Composition/` and `Orchestration/` for engine assembly seams

### `InSpectra.Gen.StartupHook`

Owns in-process capture:

- runtime capture
- reflection and runtime helpers
- framework-specific patches

Its boundary stays focused on capture and hook-specific runtime behavior.

### `InSpectra.UI`

Owns the frontend.

### `InSpectra.Gen.Core` — optional and tiny

Shared code is a last resort. A small core module is justified only for truly cross-module primitives or errors that are semantically generic and actually reused across module boundaries.

## Dependency charter

Allowed production direction:

```text
InSpectra.Gen             -> Core, Engine, StartupHook
InSpectra.Discovery.Tool  -> InSpectra.Lib, InSpectra.Gen.StartupHook
InSpectra.Gen.Engine      -> Core
InSpectra.Gen.StartupHook -> none
InSpectra.Gen.Core        -> none
InSpectra.UI              -> none of the backend modules
```

Key constraints:

- `InSpectra.Gen.Engine` must not depend on the app shell or startup-hook internals.
- The app shell may depend on `StartupHook` for packaging and startup-hook integration, but not on hook internals.
- `InSpectra.Discovery.Tool` may depend on `InSpectra.Lib` public surfaces and startup-hook packaging assets, but not on startup-hook internals.
- The app shell must stay thin: it maps raw command values into normalized requests and adapts normalized results back into shell output.
- `InSpectra.Gen` may depend only on the engine composition root plus intentionally exposed engine contracts and service interfaces. It must not reach into engine implementation namespaces.
- Non-test `InternalsVisibleTo` is not part of the target architecture.

Target-state rule: the app shell composes the backend through a single public engine entry point: `services.AddInSpectraEngine()`. Inside the engine, narrower submodule seams such as `AddInSpectraOpenCli()`, `AddInSpectraGenerateUseCases()`, and `AddInSpectraRendering()` may exist, but they are engine-internal assembly structure, not shell-facing reach-in points.

The app shell must not reference concrete implementation namespaces or types from engine internals. Cross-module references go through the engine composition root plus explicit public engine surfaces such as `Contracts/`, `UseCases/Generate/`, and `Rendering/Contracts/`.

If `StartupHook` contributes services or registrations that the app shell composes, it should expose a single public entry point. Otherwise, the app-shell dependency stays limited to the startup-hook integration surface.

If enforcement is added, the app-shell composition file is **not** exempt from the no-deep-internals rule.

### Intra-engine dependency rules

Inside `InSpectra.Gen.Engine`:

- `Contracts/` must not depend on `Tooling/` or `Modes/`.
- `Tooling/` must not depend on `Modes/`.
- One mode must not depend on another mode.
- `OpenCli/` must not depend on `Rendering/` or `UseCases/`.
- `Rendering/` must not depend on `UseCases/`.
- `Execution/` must not depend on `Modes/`, `Rendering/`, or `UseCases/`.
- `Targets/` must not depend on `Modes/` or `Rendering/`.
- `Markdown/` and `Html/` do not reference each other directly.

Additional placement guidance inside the engine:

- `OpenCli/` is pure document logic only.
- `Modes/` answer how a target was inspected.
- `Targets/` answer what source material was resolved or built.
- `Tooling/` and `Execution/` hold reusable support for multiple modes or targets.
- mode-specific conversion into OpenCLI lives under that mode as `Projection/`, not as another `OpenCli/` root.

## Placement rules

When adding code, choose the owner in this order:

1. If it is about the `inspectra` program itself, raw CLI semantics, or shell output policy, it belongs in `InSpectra.Gen`.
2. If it is normalized backend logic behind the shell, it belongs in `InSpectra.Gen.Engine`.
3. If it executes inside the inspected process, it belongs in `InSpectra.Gen.StartupHook`.
4. Only if it is truly cross-module and generic should it move to `Core`.

Hook-specific tie-break:

- Code that runs inside the inspected process always lives in `StartupHook`.
- Hook-mode orchestration outside the inspected process belongs in `InSpectra.Gen.Engine`.

Inside `InSpectra.Gen.Engine`, choose the owner in this order:

1. OpenCLI document rules -> `OpenCli/`
2. target inspection mode logic -> `Modes/<Mode>/`
3. source/target materialization -> `Targets/`
4. reusable backend support -> `Tooling/` or `Execution/`
5. documentation rendering -> `Rendering/`
6. normalized orchestration -> `UseCases/` or `Orchestration/`

## Naming rules

Naming stays principle-based:

- Prefer owner-first names that answer one question at a time.
- Namespaces must follow the owning project name plus the relative folder path. Folder moves require matching namespace moves.
- No new top-level `Runtime`, `Infrastructure`, `Models`, `Support`, `Helpers`, or `Misc` roots.
- Generic names are acceptable only one level below a concrete owner and never as the main discovery mechanism. `Commands/Common`, `Modes/Help/Parsing`, `Rendering/Pipeline`, and `OpenCli/Validation` can be reasonable when the owner is already explicit.
- `OpenCli` is the canonical domain root. Mode-specific folders that currently use `OpenCli` should become `Projection` under their real owner mode.
- Do not repeat semantic roots such as `OpenCli`, `Execution`, or `Documents` across unrelated branches unless they truly mean the same owned concept. When used, keep them under an explicit owner rather than as standalone roots.
- Do not introduce multi-type junk files such as `*Models.cs` unless they are tiny private DTO clusters with a very strong reason.

This charter does **not** impose artificial file-shape doctrine such as exact `*Support.cs` pairing rules, hard one-primary-type-per-file rules, token heuristics for Core placement, or fixed DI method signatures.

## Extension seams

New work should land in existing seams:

- new source kind -> `Engine/Targets/Sources/<Name>/`
- new acquisition mode -> `Engine/Modes/<Name>/`
- new static-analysis adapter -> `Engine/Modes/Static/<Owner>/`
- new startup-hook framework patch -> `StartupHook/Frameworks/<Name>/`
- new renderer or output format -> `Engine/Rendering/<Format>/`
- new OpenCLI transform or rule -> `Engine/OpenCli/<Stage>/`

These seams describe the intended extension points. They do not justify inventing new top-level buckets or placing mode-specific acquisition code outside `Modes/<Mode>/`.

## Rollout

### Phase 1 — charter

Phase 1 is charter only: capture module ownership, allowed and forbidden dependencies, placement rules, naming rules, and extension seams. No code moves, no DI changes, and no project splits occur in this phase.

### Phase 2 — enforcement

Add policy checks for the charter-level rules in this document:

- namespace and folder alignment
- no forbidden top-level buckets
- dependency direction consistent with the charter
- no non-test `InternalsVisibleTo`
- no app-shell references to forbidden acquisition namespaces
- no cross-mode dependencies

These checks act as guardrails for new code and migration work. Some repo-wide rules may only turn fully green as their matching Phase 3 moves land, so Phase 2 establishes non-regressive enforcement rather than assuming the legacy tree is already fully compliant. Enforce dependency direction using checks appropriate to the current module boundaries.

For the current shell-to-engine rule, `InSpectra.Gen` may reference only `InSpectra.Gen.Engine.Composition`, `InSpectra.Gen.Engine.Contracts`, `InSpectra.Gen.Engine.UseCases.Generate`, `InSpectra.Gen.Engine.Rendering.Contracts`, and intentionally exposed public service interfaces. `Modes/`, `Tooling/`, `Targets/Sources/`, `Execution/`, and concrete rendering namespaces remain forbidden to the app shell.

### Phase 3 — move code without changing behavior

Apply the structure to the existing codebase:

- remove catch-all roots such as `Runtime`
- keep `InSpectra.Gen` as a thin shell
- rename mode-specific `OpenCli` folders to `Projection/`
- consolidate backend logic under `InSpectra.Gen.Engine`
- move DI composition behind `AddInSpectraEngine()` so the app shell registers the engine, not concrete internals
- keep the OpenCLI domain and rendering concerns under explicit engine owners

### Phase 4 — extract assemblies if needed

If the cleaned boundaries justify it later, additional assembly splits may be considered. They are not required for the thin-shell charter.

## Summary

The target architecture is:

- app shell -> thin CLI adapter
- engine -> normalized backend
- OpenCLI -> canonical engine domain
- acquisition/modes/targets/tooling -> engine internals
- rendering -> engine rendering module
- startup hook -> in-process capture

Inside each module: **capability first, variant second, mechanism third.**
