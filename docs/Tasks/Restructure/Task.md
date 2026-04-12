> **HISTORICAL.** This document captured the planning state before the Step 1–11 refactor. The target state described here has been implemented and is enforced by architecture policy tests in tests/InSpectra.Gen.Tests/Architecture/. See docs/architecture/ARCHITECTURE.md for the current charter.

Yes. The repo needs an **architecture charter**, not just folder cleanup.

The shortest version is:

**Capability first, variant second, mechanism third.**

Right now the tree often does the opposite. It mixes:

* user-facing command/use-case: `Commands/Generate`, `Commands/Render`
* source kind: exec / dotnet / package
* acquisition strategy: native / help / clifx / static / hook
* technical mechanism: `Execution`, `Infrastructure`, `Runtime`, `Parsing`, `Documents`
* domain concept: `OpenCli`, `Rendering`, `Models`

That is why it feels slippery. A folder should answer one question. Today many top-level folders answer different questions.

A good future-proof hierarchy for this repo is:

**product → capability → variant → stage**

For InSpectra, that means:

* **product**: app shell, acquisition engine, startup hook, UI
* **capability**: OpenCLI domain, acquisition, rendering
* **variant**: help, CliFx, static, hook / markdown, html / exec, package, dotnet
* **stage**: parsing, crawling, metadata, projection, validation, process, bundle

That gives you stable growth without rethinking the tree every time.

---

## The philosophy I would adopt

### 1. Top-level projects own long-lived capabilities, not convenience buckets

A project should exist because it has a stable reason to live separately:

* different deployment/runtime target
* separate package/public API
* heavy dependency island
* distinct lifecycle or test surface

By that standard, these are the right long-lived backend modules:

* `InSpectra.Gen` → the CLI app shell
* `InSpectra.Gen.OpenCli` → canonical OpenCLI domain
* `InSpectra.Gen.Acquisition` → “turn something into OpenCLI”
* `InSpectra.Gen.Rendering` → “turn OpenCLI into docs”
* `InSpectra.Gen.StartupHook` → injected runtime capture
* `InSpectra.UI` → frontend

A tiny `InSpectra.Gen.Core` is also justified if you need shared exceptions/primitives. Keep it tiny or it becomes a junk drawer.

### 2. Inside a module, organize by variant, not by generic technical words

Words like `Runtime`, `Infrastructure`, `Models`, `Support`, `Helpers` are usually signs that the real concept has not been named yet.

In your repo, this is visible immediately:

* `InSpectra.Gen` has **38 C# files** under `Runtime`, but that folder mixes settings, request DTOs, render contracts, and output envelopes.
* `InSpectra.Gen.Acquisition` has **70 files** under `Help`, **45** under `Analysis`, and **41** under `StaticAnalysis`, which tells me the missing middle layer is something like `Modes/`.

The goal is not “more folders.” The goal is **folders that represent stable concepts**.

### 3. Use one canonical domain home for OpenCLI

Right now OpenCLI logic is split across multiple places:

* typed OpenCLI models in `InSpectra.Gen/Models`
* loading/schema/serialization in `InSpectra.Gen/OpenCli/Documents`
* normalization/enrichment in `InSpectra.Gen/OpenCli/Processing`
* OpenCLI sanitization/validation/options/structure also inside `InSpectra.Gen.Acquisition/OpenCli/*`
* mode-specific “OpenCli builders” under help / CliFx / static / hook

That is too many homes for the central domain.

The rule should be:

* **mode-agnostic OpenCLI logic** belongs in `OpenCli`
* **mode-specific conversion to OpenCLI** belongs under that mode as `Projection` or `Mapping`

So `Help/OpenCli`, `Analysis/CliFx/OpenCli`, and `StaticAnalysis/OpenCli` are not really “OpenCli” modules. They are **projections** from a source-specific representation into the OpenCLI domain.

That naming shift matters a lot.

### 4. The app shell must be thin

`InSpectra.Gen` should know about:

* command parsing
* output mode selection
* JSON vs human output
* DI composition
* application use cases like “generate” and “render”

It should **not** know concrete deep analyzer types.

Right now `ServiceCollectionExtensions` imports concrete types like `CliFxMetadataInspector`, `DnlibAssemblyScanner`, `HookInstalledToolAnalysisSupport`, `InstalledToolAnalyzer`, and more. That means the app is wiring deep acquisition internals directly instead of depending on module entry points.

Future-proof rule:

* each module exposes one composition method
* app registers modules, not internals

For example:

* `services.AddInSpectraOpenCli()`
* `services.AddInSpectraAcquisition()`
* `services.AddInSpectraRendering()`

and the app should not need to know the concrete classes behind them.

### 5. Shared code is a last resort

A file should move to shared/common/core only if:

* it is used by multiple modules
* it is semantically generic
* it is likely to stay generic

Otherwise it stays close to its owner.

For example, current `CliUsageException` / `CliDataException` / `CliSourceExecutionException` living under `InSpectra.Gen.Acquisition/Runtime` is backwards, because other parts of the app use them too. Those belong in a tiny cross-cutting core/errors module.

By contrast, most “process support” code should stay acquisition-side unless rendering also genuinely needs it.

---

## What belongs where

Here is the ownership model I would use.

### `InSpectra.Gen` — app shell

Owns only the `inspectra` tool as a program.

Belongs here:

* `Program.cs`
* command classes and their settings
* output envelope / human console output
* app-level use cases
* DI composition
* packaging glue

Does **not** own:

* analyzer implementations
* OpenCLI validation/sanitization
* renderer internals
* package inspection / NuGet / dnlib

A good internal shape is:

```text
src/InSpectra.Gen/
  Program.cs
  Composition/
  Commands/
    Generate/
      Exec/
      Package/
      Dotnet/
    Render/
      File/
  UseCases/
    Generate/
    Render/
  Output/
```

### `InSpectra.Gen.OpenCli` — canonical document domain

Owns everything that is about the OpenCLI document itself, regardless of how it was acquired or rendered.

Belongs here:

* `OpenCliDocument`, `OpenCliCommand`, `OpenCliOption`, etc.
* schema provider / schema validation
* document loader and serializer
* compatibility sanitization
* document cloning
* XML enrichment
* publishability / structural validation
* document-level option/structure sanitizers

Does **not** own:

* crawling
* package inspection
* framework detection
* markdown/html rendering

A good shape:

```text
src/InSpectra.Gen.OpenCli/
  Model/
  Schema/
  Validation/
  Serialization/
  Enrichment/
```

### `InSpectra.Gen.Acquisition` — turn a target into OpenCLI

Owns all acquisition logic.

This module has two different axes and they need different homes:

* **sources**: exec / package / dotnet
* **modes**: native / help / CliFx / static / hook

That distinction is the key to making this project predictable.

A good shape:

```text
src/InSpectra.Gen.Acquisition/
  Contracts/
  Sources/
    Exec/
    Package/
    Dotnet/
    Targets/
  Modes/
    Native/
    Help/
      Crawling/
      Parsing/
      Inference/
      Projection/
    CliFx/
      Crawling/
      Metadata/
      Projection/
    Static/
      Frameworks/
      Inspection/
      Projection/
    Hook/
      Invocation/
      Capture/
      Projection/
  Tooling/
    Process/
    NuGet/
    Packages/
    FrameworkDetection/
  Composition/
```

Belongs here:

* executable resolution and target materialization
* dotnet build output resolution
* package installation / tool command resolution
* process execution/sandboxing for acquisition
* NuGet/package archive inspection
* framework detection
* acquisition planning and attempt sequencing
* mode analyzers

Does **not** own:

* generic OpenCLI document rules
* rendering
* console output shaping

### `InSpectra.Gen.Rendering` — OpenCLI to docs

Owns the rendering pipeline and output-format implementations.

Belongs here:

* render contracts
* normalized render model
* markdown/html formatters
* render stats
* bundle lookup / html asset resolution

Does **not** own:

* CLI command parsing
* acquisition
* package inspection
* OpenCLI schema validation

A good shape:

```text
src/InSpectra.Gen.Rendering/
  Contracts/
  Pipeline/
  Markdown/
  Html/
    Bundle/
    Assets/
  Composition/
```

### `InSpectra.Gen.StartupHook`

This one is already closer to a good structure.

Keep it focused on:

* runtime capture
* reflection/runtime helpers
* framework-specific patches

Its current framework-first organization is actually reasonable because the dominant axis there really is the patched framework.

---

## How current folders would map

These are the highest-value moves.

### In `InSpectra.Gen`

* `Runtime/Acquisition/*` → `InSpectra.Gen.Acquisition/Contracts/*`
* `Runtime/Rendering/*` → `InSpectra.Gen.Rendering/Contracts/*`
* `Runtime/Output/*` → `InSpectra.Gen/Output/*`
* `Runtime/Settings/*` → `InSpectra.Gen/Commands/Common/*` or `InSpectra.Gen/Commands/Shared/*`
* `Targets/*` and `Execution/*` → `InSpectra.Gen.Acquisition/Sources/*` or `Tooling/Process/*`
* `OpenCli/Documents/*` → `InSpectra.Gen.OpenCli/Schema|Validation|Serialization/*`
* `OpenCli/Processing/OpenCliXmlEnricher.cs` → `InSpectra.Gen.OpenCli/Enrichment/*`
* `Viewer/*` → `InSpectra.Gen.Rendering/Html/Bundle/*`

Also, `Models` should be split:

* canonical document model types like `OpenCliDocument` → `OpenCli/Model`
* render-facing normalized types like `NormalizedCliDocument`, `NormalizedCommand`, `ResolvedOption` → `Rendering/Pipeline` or `Rendering/Model`

Right now `Models` mixes domain model and render view model, which is another reason navigation feels blurry.

### In `InSpectra.Gen.Acquisition`

* `Help/*` → `Modes/Help/*`
* `Analysis/CliFx/*` → `Modes/CliFx/*`
* `StaticAnalysis/*` → `Modes/Static/*`
* `Analysis/Hook/*` → `Modes/Hook/*`

Then rename the repeated `OpenCli` subfolders to `Projection`:

* `Help/OpenCli/*` → `Modes/Help/Projection/*`
* `Analysis/CliFx/OpenCli/*` → `Modes/CliFx/Projection/*`
* `StaticAnalysis/OpenCli/*` → `Modes/Static/Projection/*`

That leaves the name `OpenCli` reserved for the actual document domain.

Also collapse the technical buckets:

* `Infrastructure/Commands/*`
* `Analysis/Execution/*`
* any process runner/sandbox helpers from `InSpectra.Gen`

into one place:

* `Tooling/Process/*`

And regroup the package framework stuff:

* `NuGet/*`
* `Packages/*`
* `Analysis/Tools/ToolDescriptor*`
* `Frameworks/*`

under:

* `Tooling/NuGet/*`
* `Tooling/Packages/*`
* `Tooling/FrameworkDetection/*`

That makes ownership much clearer.

---

## Dependency rules that keep the repo sane

This is the part that makes it future-proof.

### Project-level rules

Allowed direction:

```text
InSpectra.Gen            -> Core, OpenCli, Acquisition, Rendering
InSpectra.Gen.Rendering  -> Core, OpenCli
InSpectra.Gen.Acquisition-> Core, OpenCli
InSpectra.Gen.OpenCli    -> Core
InSpectra.Gen.StartupHook-> Core (or none)
InSpectra.UI             -> none of the backend projects
```

Forbidden:

* Acquisition → Rendering
* Rendering → Acquisition
* OpenCli → Acquisition / Rendering / App
* App → deep acquisition internals
* any non-test `InternalsVisibleTo`

Today `InSpectra.Gen.Acquisition` exposes internals to `inspectra`; that should go away. If the app needs a thing, that thing should either be public contract, module composition, or moved behind an interface.

### Intra-module rules

Inside `Acquisition`:

* `Sources` may depend on `Tooling` and `Contracts`
* `Modes` may depend on `Tooling`, `Contracts`, and `OpenCli`
* one mode must not depend on another mode
* `Tooling` must not depend on `Modes`

Inside `Rendering`:

* format implementations depend on shared render pipeline and OpenCLI
* markdown and html do not reference each other directly

Inside `OpenCli`:

* pure document logic only
* no process, no filesystem orchestration beyond loader/saver boundaries, no NuGet, no rendering

---

## The rule for adding new code

This is the part that avoids “restructure every time.”

When someone adds code, they should ask these questions in order:

1. **Is this about the app shell?**
   Then it goes in `InSpectra.Gen`.

2. **Is this about the OpenCLI document itself?**
   Then it goes in `InSpectra.Gen.OpenCli`.

3. **Is this about obtaining OpenCLI from a target?**
   Then it goes in `InSpectra.Gen.Acquisition`.

4. **Is it a new source kind or a new acquisition mode?**
   Source kind → `Sources/<Name>`
   Mode → `Modes/<Name>`

5. **Is it about turning OpenCLI into docs?**
   Then it goes in `InSpectra.Gen.Rendering`.

6. **Does it execute inside the inspected app?**
   Then it belongs in `InSpectra.Gen.StartupHook`.

7. **Is it truly shared?**
   Only then consider `Core`.

This gives you stable extension seams:

* new source kind → `Acquisition/Sources/...`
* new analysis mode → `Acquisition/Modes/...`
* new static-analysis framework adapter → `Acquisition/Modes/Static/Frameworks/...`
* new startup-hook framework patch → `StartupHook/Frameworks/...`
* new renderer → `Rendering/<Format>/...`
* new OpenCLI transform → `OpenCli/...`

That is what “future-proof” really means here: **new work lands in a known seam instead of inventing a new top-level bucket**.

---

## Things I would explicitly ban

I would put these in the repo rules:

* no new top-level `Runtime`, `Infrastructure`, `Models`, `Support`, `Helpers`, `Misc`
* no mode-specific code outside `Modes/<Mode>`
* no app project references to concrete analyzer implementations
* no repeated semantic roots like `OpenCli`, `Execution`, or `Documents` in unrelated branches unless they truly mean the same owned concept
* no `InternalsVisibleTo` except tests
* no multi-type junk files like `*Models.cs` unless they are tiny private DTO clusters with a very strong reason

Generic names are allowed only one level below a real owner, never as the main discovery mechanism.

---

## How I would roll this out

Do it in this order so it stays manageable.

### Phase 1: define the charter

Add a short `docs/architecture/ARCHITECTURE.md` with:

* module ownership
* dependency rules
* placement rules
* naming rules
* allowed extension seams

### Phase 2: enforce it

You already have repo policy tests. Add architecture policy tests too:

* namespace matches folder path
* no forbidden top-level buckets
* no non-test `InternalsVisibleTo`
* project dependency rules
* app does not reference forbidden acquisition namespaces
* modes do not reference other modes

### Phase 3: move files without changing behavior

Start with the obvious high-value moves:

* kill `Runtime` as a catch-all
* introduce `Modes/`
* rename mode-specific `OpenCli` folders to `Projection`
* consolidate process execution into one acquisition-owned place
* move document logic to a dedicated OpenCLI home

### Phase 4: only then split assemblies further if needed

You do not need to create six new projects on day one.
The important thing is to adopt the ownership model first.

A good pragmatic path is:

1. apply the folder/namespace philosophy inside the current projects
2. move DI composition into each module
3. then extract `OpenCli` and `Rendering` as separate libraries once the boundaries are clean

---

## The one-sentence version

The repo should be organized as:

**app shell** → thin orchestration
**OpenCLI** → canonical document domain
**acquisition** → target to OpenCLI
**rendering** → OpenCLI to docs
**startup hook** → in-process capture

And inside those modules:

**capability first, variant second, mechanism third.**

That is the strategy that lets you add a new mode, renderer, framework adapter, or source type without inventing new roots or reshuffling the whole tree again.

The next useful step is turning this into a concrete move-map for the current folders and files.

---

## Current State (verified 2026-04-10)

> **Note (post-refactor): the tree has since been restructured through steps 1–11 plus phases F1–F4. The counts and paths in this section reflect the pre-refactor state. Do not use for navigation — use ARCHITECTURE.md and the repo tree itself.**

Repo was scanned with 8 overlapping subagents. The charter above still holds. Below is the delta between the charter and what is actually in the tree today, plus a precise move-map.

### Project footprint

| Project | C# files | Notes |
|---|---|---|
| `InSpectra.Gen` | 127 | App shell — still owns OpenCli domain + rendering + targets |
| `InSpectra.Gen.Acquisition` | 226 | Owns help/cliFx/static/hook analyzers and a cross-cutting `OpenCli/` subtree |
| `InSpectra.Gen.StartupHook` | 23 | Already close to charter shape |
| `InSpectra.UI` | — | Vite/TypeScript frontend |

Total backend surface: ~376 C# files.

### Verified charter violations

1. **Non-test `InternalsVisibleTo`** — `src/InSpectra.Gen.Acquisition/Properties/AssemblyInfo.cs:4` exposes internals to the `inspectra` assembly. Only `InSpectra.Gen.Acquisition.Tests` is allowed.
2. **App shell wires deep acquisition internals** — `src/InSpectra.Gen/Composition/ServiceCollectionExtensions.cs` (`AddAcquisitionAnalyzers`, lines 74–93) directly registers 15 concrete types from Acquisition namespaces: `CommandRuntime`, `OpenCliBuilder`, `CliFxMetadataInspector`, `CliFxOpenCliBuilder`, `CliFxCoverageClassifier`, `StaticAnalysisRuntime`, `DnlibAssemblyScanner`, `StaticAnalysisAssemblyInspectionSupport`, `StaticAnalysisOpenCliBuilder`, `StaticAnalysisCoverageClassifier`, `InstalledToolAnalyzer`, `CliFxInstalledToolAnalysisSupport`, `StaticInstalledToolAnalysisSupport`, `HookInstalledToolAnalysisSupport`, `IToolDescriptorResolver`.
3. **No module composition methods** — neither `InSpectra.Gen.Acquisition` nor `InSpectra.Gen.StartupHook` exposes an `AddInSpectra*` extension. The app shell reaches in directly.
4. **OpenCLI domain scattered across 5 locations** — canonical home does not exist yet:
   - `src/InSpectra.Gen/Models/OpenCli/` — 11 canonical model types (`OpenCliDocument`, `OpenCliCommand`, etc.)
   - `src/InSpectra.Gen/OpenCli/Documents/` — 6 files: schema provider, loader, serializer, cloner, sanitizers
   - `src/InSpectra.Gen/OpenCli/Processing/` — 3 files: normalizer, XML enricher, enrichment result
   - `src/InSpectra.Gen/OpenCli/Acquisition/` — 11 files: acquisition services, generation service, artifact writer, mode planner
   - `src/InSpectra.Gen.Acquisition/OpenCli/` — 28 files under `Documents/`, `Options/`, `Structure/` (sanitizers, validators, option collision, node validation)
5. **Render view model lives in `Models/`** — `NormalizedCliDocument.cs`, `NormalizedCommand.cs`, `ResolvedOption.cs` live in `src/InSpectra.Gen/Models/` even though they are only used by `Rendering/*`. They belong inside the rendering pipeline.
6. **`Runtime/` is a catch-all (40 files)** — mixes acquisition request DTOs (10), rendering contracts (12), JSON envelopes (4), output handlers (2), command settings bases (5), and top-level render request factories (5 files at root).
7. **Mode-specific `OpenCli` folders inside Acquisition** — four directories use the `OpenCli` name for what are really *projections* from a source representation into the OpenCLI domain:
   - `Analysis/CliFx/OpenCli/` (7 files)
   - `Help/OpenCli/` (7 files)
   - `StaticAnalysis/OpenCli/` (6 files)
   - (the top-level `OpenCli/` is a separate case — see #4)
8. **Technical catch-all buckets still exist** — `Analysis/`, `Analysis/Execution/`, `Infrastructure/Commands/`, `Runtime/` (exceptions), `Frameworks/`, `NuGet/`, `Packages/` are not yet grouped under `Modes/`, `Tooling/`, or `Core`.

### Things that are already fine

- `InSpectra.Gen.csproj` references `InSpectra.Gen.Acquisition` and `InSpectra.Gen.StartupHook` only. Acquisition has zero project references. Direction is correct.
- `InSpectra.Gen.StartupHook` has no backwards project references and is organised framework-first, which matches the charter.
- `Composition/ServiceCollectionExtensions.cs` already splits registration into `AddOpenCliServices`, `AddTargetServices`, `AddRenderingServices`, `AddAcquisitionAnalyzers`. Good seams — only the last one is leaking internals.
- Commands are thin. `ExecGenerateCommand`, `PackageGenerateCommand`, `DotnetGenerateCommand` each depend only on `IOpenCliGenerationService` plus request/output support — no deep reach-in.
- `RepositoryCodeFilePolicyTests.cs` already exists in `tests/InSpectra.Gen.Tests/` as an example of how repo-wide policy tests are wired. New architecture tests should follow the same xUnit + `FixturePaths` pattern.
- Namespaces already match folder paths (file-scoped, matching `{Project}.{RelativeFolderPath}`). No mass rename required — only the new structure needs to stay on this rule.

### Concrete move-map (for Phase 3)

This expands section "How current folders would map" with verified paths. Nothing here is executed in Phase 1 — the charter is written first.

**Inside `InSpectra.Gen`:**

| From | To |
|---|---|
| `Models/OpenCli/*` (11 files) | `OpenCli/Model/*` (new canonical home, eventually `InSpectra.Gen.OpenCli/Model/`) |
| `Models/NormalizedCliDocument.cs`, `NormalizedCommand.cs`, `ResolvedOption.cs` | `Rendering/Pipeline/Model/*` |
| `OpenCli/Documents/OpenCliSchemaProvider.cs` | `OpenCli/Schema/*` |
| `OpenCli/Documents/OpenCliDocumentLoader.cs`, `OpenCliDocumentSerializer.cs`, `OpenCliDocumentCloner.cs` | `OpenCli/Serialization/*` |
| `OpenCli/Documents/OpenCliCompatibilitySanitizer.cs`, `OpenCliJsonSanitizer.cs` | `OpenCli/Validation/*` (document-level) |
| `OpenCli/Processing/OpenCliNormalizer.cs`, `OpenCliXmlEnricher.cs`, `XmlEnrichmentResult.cs` | `OpenCli/Enrichment/*` |
| `OpenCli/Acquisition/*` (11 files) | `UseCases/Generate/*` (acquisition + generation services are application-level use cases) |
| `Runtime/Acquisition/*` (10 files) | Acquisition contracts → `Acquisition/Contracts/*` inside Acquisition project |
| `Runtime/Rendering/*` (12 files) | `Rendering/Contracts/*` |
| `Runtime/Json/*` (4 files) | `Output/Json/*` |
| `Runtime/Output/*` (2 files) | `Output/*` |
| `Runtime/Settings/*` (5 files) | `Commands/Common/*` |
| `Runtime/*.cs` (5 top-level render request factories) | `UseCases/Render/*` or `Commands/Render/RequestBuilders/*` |
| `Targets/DotnetBuildOutputResolver.cs`, `DotnetProjectArgsBuilder.cs`, `DotnetProjectResolver.cs`, `LocalCliFrameworkDetector.cs`, `LocalCliTargetFactory.cs`, `MaterializedCliTarget.cs`, `PackageCliTargetFactory.cs` | `Acquisition/Sources/Dotnet/*` and `Acquisition/Sources/Targets/*` |
| `Execution/*` (5 files: `CliInvocationEnvironmentFactory`, `ExecutableResolver`, `IProcessRunner`, `ProcessRunner`, `TemporaryWorkspace`) | `Acquisition/Tooling/Process/*` |
| `Viewer/*` (3 files) | `Rendering/Html/Bundle/*` |
| `Commands/Generate/{Exec,Package,Dotnet}*.cs` (flat) | `Commands/Generate/{Exec,Package,Dotnet}/*` (grouped) |
| `Commands/Render/File*.cs` (flat) | `Commands/Render/File/*` (grouped) |

**Inside `InSpectra.Gen.Acquisition`:**

| From | To |
|---|---|
| `Help/*` (70 files) | `Modes/Help/*` (preserve `Crawling/`, `Documents/`, `Inference/`, `Parsing/`, `Signatures/` subtree) |
| `Help/OpenCli/*` (7 files) | `Modes/Help/Projection/*` |
| `Analysis/CliFx/*` (18 files) | `Modes/CliFx/*` (preserve `Crawling/`, `Execution/`, `Metadata/` subtrees) |
| `Analysis/CliFx/OpenCli/*` (7 files) | `Modes/CliFx/Projection/*` |
| `StaticAnalysis/*` | `Modes/Static/*` (preserve `Attributes/`, `Inspection/`, `Models/` subtrees) |
| `StaticAnalysis/OpenCli/*` (6 files) | `Modes/Static/Projection/*` |
| `Analysis/Hook/*` (13 files) | `Modes/Hook/*` |
| `Analysis/Execution/*` (3 files) | `Tooling/Process/*` (merge with `InSpectra.Gen/Execution/*`) |
| `Analysis/Introspection/*` (4 files) | `Modes/Static/Introspection/*` or `Tooling/Introspection/*` (decide in Phase 3) |
| `Analysis/Results/*` (3 files) | `Contracts/Results/*` |
| `Analysis/Tools/*` (2 files) | `Tooling/Tools/*` (tool descriptor resolver) |
| `Infrastructure/Commands/*` (9 files) | `Tooling/Process/*` |
| `Infrastructure/Json/*` (3 files) | `Tooling/Json/*` (or lift to a tiny shared core) |
| `Infrastructure/Paths/*` (2 files) | `Tooling/Paths/*` |
| `Infrastructure/ApplicationLifetime.cs`, `InspectraProductInfo.cs` | `Contracts/*` or lift to shared core |
| `NuGet/*` (7 files) | `Tooling/NuGet/*` |
| `Packages/*` (11 files) | `Tooling/Packages/*` |
| `Frameworks/*` (3 files) | `Tooling/FrameworkDetection/*` |
| `Runtime/CliException.cs`, `CliUsageException.cs`, `CliDataException.cs`, `CliSourceExecutionException.cs` | Cross-cutting core/errors module (shared with `InSpectra.Gen`) |
| `OpenCli/Documents/*` (9 files) | canonical OpenCLI home (merge with `InSpectra.Gen/OpenCli/Documents/*`) |
| `OpenCli/Options/*` (14 files incl. `Collisions/`) | canonical OpenCLI home — `OpenCli/Validation/Options/*` |
| `OpenCli/Structure/*` (5 files) | canonical OpenCLI home — `OpenCli/Validation/Structure/*` |

**New composition seams (Phase 2/3):**

- `AddInSpectraOpenCli(this IServiceCollection)` — registers schema, loader, serializer, cloner, enricher, sanitizers, validators.
- `AddInSpectraAcquisition(this IServiceCollection)` — registers all mode analyzers, tooling, planners. Replaces `AddAcquisitionAnalyzers` in the app shell.
- `AddInSpectraRendering(this IServiceCollection)` — registers markdown/html render services, formatters, bundle locator.

After these land, `InSpectra.Gen/Composition/ServiceCollectionExtensions.cs` should contain *only* `AddInSpectraGen` plus the three `AddInSpectra*` calls and command-layer glue. The concrete-type list goes away.

### Phase 1 deliverable

Phase 1 is **charter only**: produce `docs/architecture/ARCHITECTURE.md` capturing module ownership, allowed/forbidden dependencies, placement rules, naming rules, and extension seams. No code moves, no DI changes, no project splits. Phases 2–4 follow.

