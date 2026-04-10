# InSpectra Architecture Charter

> **Status:** Adopted 2026-04-10. This is the binding charter for all backend code under `src/InSpectra.Gen*`. Every folder, namespace, and project reference must conform. Violations block merge.

The one-line version:

> **Capability first, variant second, mechanism third.**

And inside a module:

> **product → capability → variant → stage.**

The long-form rationale lives in `docs/Tasks/Restructure/Task.md`. This file is the enforceable summary.

### Why these rules exist

The repo drifts when a single folder tries to answer several different questions at once. Historically five axes have been conflated:

1. **User-facing capability** — the command or use case the user invokes (`generate`, `render`).
2. **Source kind** — where the CLI came from (`exec`, `package`, `dotnet`).
3. **Acquisition strategy** — how the CLI was inspected (`native`, `help`, `cliFx`, `static`, `hook`).
4. **Technical mechanism** — generic engineering words (`Runtime`, `Infrastructure`, `Execution`, `Documents`, `Processing`).
5. **Domain concept** — the OpenCLI document, rendering output, etc.

A folder is legible when it answers exactly one of these, in the order above. A folder is illegible when it mixes two or more. Every banned name in §4 and every intra-module rule in §2 exists because it would otherwise collapse two axes into one folder. When deciding where a new file goes, first ask which axis it lives on — not which word feels convenient.

---

## 1. Module ownership

InSpectra has exactly these backend modules. Each module has one reason to exist. Nothing else gets a top-level bucket.

A **module** is a long-lived capability boundary identified by a namespace root. Some modules are already separate assemblies today (`InSpectra.Gen`, `InSpectra.Gen.Acquisition`, `InSpectra.Gen.StartupHook`, `InSpectra.UI`). Others (`InSpectra.Gen.OpenCli`, `InSpectra.Gen.Rendering`, `InSpectra.Gen.Core`) exist **today as top-level folders inside `InSpectra.Gen`** with matching namespaces; Phase 4 extracts them into separate assemblies. **The ownership and dependency rules apply to modules whether or not they are separate assemblies yet** — a rule violation is just as much a violation when the modules share an assembly as when they don't.

| Module | Status today | Reason to exist | Owns |
|---|---|---|---|
| `InSpectra.Gen` | assembly | The `inspectra` CLI program | `Program.cs`, command classes, command settings, DI composition, output envelope (JSON vs human console output), app-level use cases, packaging/tool glue |
| `InSpectra.Gen.OpenCli` | folder in `InSpectra.Gen` (Phase 4: assembly) | Canonical OpenCLI document domain | `OpenCliDocument` and sibling model types, schema provider, loader, serializer, cloner, compatibility sanitization, XML enrichment, publishability / structural validation, document-level and option/structure sanitizers |
| `InSpectra.Gen.Acquisition` | assembly | Turn a target into an OpenCLI document | Executable resolution, target materialization, dotnet build output resolution, package installation / tool command resolution, process execution / sandboxing, NuGet / package archive inspection, framework detection, acquisition planning and attempt sequencing, mode analyzers (native, help, cliFx, static, hook) |
| `InSpectra.Gen.Rendering` | folder in `InSpectra.Gen` (Phase 4: assembly) | Turn an OpenCLI document into human-readable docs | Render contracts, normalized render model, markdown and html formatters, render stats, html bundle lookup / asset resolution |
| `InSpectra.Gen.StartupHook` | assembly | Run inside the inspected app and capture CLI metadata | Runtime capture, reflection/runtime helpers, framework-specific patches |
| `InSpectra.Gen.Core` *(empty until the first cross-module type lands; see §3.1)* | folder in `InSpectra.Gen` (Phase 4: assembly) | Cross-cutting primitives directly referenced by ≥ 2 of {`OpenCli`, `Acquisition`, `Rendering`, `StartupHook`, app shell} | `CliException`, `CliUsageException`, `CliDataException`, `CliSourceExecutionException`, and nothing else until there is real cross-module reuse |
| `InSpectra.UI` | assembly | Frontend viewer for HTML output | TypeScript / Vite app. Does not reference backend projects. |

### What the app shell does *not* own

`InSpectra.Gen` is a thin orchestration layer. It explicitly does **not** own:

- Analyzer implementations (help crawlers, CliFx inspectors, static-analysis scanners, hook support).
- OpenCLI schema validation, sanitization, or structural rules.
- Renderer internals (markdown/html formatting logic, bundle composition).
- Package inspection, NuGet archive reading, dnlib / `MetadataLoadContext` usage.
- Process execution / sandboxing primitives.

If the app shell needs any of these capabilities, it consumes them through the relevant module's composition method (§2.3) and public contracts — never by importing concrete implementation types.

### What does *not* get its own module

Convenience buckets are not modules. They are not even folders (see §4.2 for the full ban list and whitelist):

- No `Common`, `Shared`, `Utils`, `Utilities`, `Helpers`, `Support`, `Misc`, `Base` at module root.
- No `Infrastructure` as a generic catch-all for anything that touches I/O.
- No `Runtime` as a generic catch-all for DTOs, options, or "stuff the runtime needs".
- No `Model` / `Models` as a generic catch-all mixing domain and view models.

If a proposal needs one of these names as a top-level concept, the concept is wrong and the naming is hiding that.

---

## 2. Dependency rules

### 2.1 Allowed directions

Each row below lists every edge a module is *permitted* to declare. `→ {}` means the module declares no dependencies on any other module in the repo. Everything not listed is forbidden.

```text
InSpectra.Gen              → {Core, OpenCli, Acquisition, Rendering}
InSpectra.Gen.Rendering    → {Core, OpenCli}
InSpectra.Gen.Acquisition  → {Core, OpenCli}
InSpectra.Gen.OpenCli      → {Core}
InSpectra.Gen.StartupHook  → {Core} or {}          (either is valid; zero dependencies preferred)
InSpectra.Gen.Core         → {}
InSpectra.UI               → {}                    (no backend references)
*.Tests (any test project) → any production assembly in this repo
```

Test projects are exempt from the production direction rules: every `*.Tests` project may reference any production project it needs. This exemption applies only to test projects; production code must still obey the table above.

### 2.2 Forbidden

Cross-module:

- `Acquisition → Rendering`
- `Rendering → Acquisition`
- `OpenCli → Acquisition | Rendering | App | StartupHook`
- `Core → anything`
- `StartupHook → Acquisition | Rendering | App | OpenCli`

App-shell specific:

- `App → deep acquisition internals` (defined in §2.3).
- `App → analyzer implementation types` (help crawlers, CliFx inspectors, static-analysis scanners, hook support).
- `App → OpenCLI validation / sanitization / schema internals`.
- `App → renderer internals` (concrete formatter or bundle composer types).
- `App → package inspection / NuGet / dnlib / MetadataLoadContext types`.

Universal:

- **Any non-test `InternalsVisibleTo`** on a production assembly.

Today's known violations (to be fixed in Phase 3, tracked by the tests in §6):

- `src/InSpectra.Gen.Acquisition/Properties/AssemblyInfo.cs:4` exposes internals to the `inspectra` assembly. Must be removed. Only `InSpectra.Gen.Acquisition.Tests` is allowed.
- `src/InSpectra.Gen/Composition/ServiceCollectionExtensions.cs` (`AddAcquisitionAnalyzers`, lines 74–93) registers ~15 concrete Acquisition types directly. Must be replaced with a call to `AddInSpectraAcquisition()` (see §2.3).

### 2.3 What "deep acquisition internals" means

**Definition (precise, testable):** a "deep acquisition internal" is any type whose namespace matches `InSpectra.Gen.Acquisition.*` *except* types whose namespace starts with exactly `InSpectra.Gen.Acquisition.Contracts` or `InSpectra.Gen.Acquisition.Composition` (followed by `.` or end-of-string). Everything else inside the `Acquisition` namespace — including `Acquisition/Modes/*`, `Acquisition/Sources/*`, `Acquisition/Tooling/*`, and any legacy folders still present before Phase 3 (`Analysis/`, `Help/`, `StaticAnalysis/`, `Infrastructure/`, `NuGet/`, `Packages/`, `Frameworks/`, `Runtime/`) — is a deep internal.

The same principle applies to `OpenCli`, `Rendering`, and `StartupHook` once they expose `Contracts/` and `Composition/` folders: any file outside those two is internal.

**The rule:** the app shell may only consume a module through two surfaces:

1. **Public contracts** under `<Module>/Contracts/*` (DTOs, result types, service interfaces, public exceptions).
2. **A single composition method** under `<Module>/Composition/ServiceCollectionExtensions.cs`.

Each module declares exactly one composition method, declared as a `public static` extension on `IServiceCollection` returning `IServiceCollection`. The method takes `IServiceCollection` as its only required parameter. A second optional parameter is permitted when and only when it is an `Action<TOptions>` configuration callback that follows the standard .NET options pattern; no other parameter shapes are allowed.

```csharp
// src/InSpectra.Gen.OpenCli/Composition/ServiceCollectionExtensions.cs
public static IServiceCollection AddInSpectraOpenCli(this IServiceCollection services);

// src/InSpectra.Gen.Acquisition/Composition/ServiceCollectionExtensions.cs
public static IServiceCollection AddInSpectraAcquisition(this IServiceCollection services);

// src/InSpectra.Gen.Rendering/Composition/ServiceCollectionExtensions.cs
public static IServiceCollection AddInSpectraRendering(
    this IServiceCollection services,
    Action<ViewerBundleLocatorOptions>? configure = null);
```

**Composition-method exemption.** Each `<Module>/Composition/ServiceCollectionExtensions.cs` file is exempt from the "no deep internals" rule *for its own module*: it may `new` / `typeof` / `using` any type inside its own module, because registering them is the reason the file exists. Private helper methods or types declared *inside that single file* are also exempt by virtue of being part of it.

Sibling files in `<Module>/Composition/` — for example a companion `ServiceCollectionExtensions.HelperRegistrations.cs` partial or a utility `ModuleOptionsBinder.cs` — do **not** automatically inherit the exemption. Such helpers are allowed, but their content must either (a) stay within the module's public `Contracts/` surface, or (b) be declared as a `partial` of the same `ServiceCollectionExtensions` type so that §6 Test 6 treats them as part of the exempt file. No sibling file in `Composition/` may reference another module's deep internals, ever.

The exemption only applies to a module's *own* internals. `InSpectra.Gen/Composition/ServiceCollectionExtensions.cs` is the app-shell composition file and is not exempt from the deep-internals rule for the `Acquisition`, `OpenCli`, `Rendering`, or `StartupHook` modules — it must call each module's `AddInSpectra*` method, never reach in directly.

**What the app shell must not name.** After Phase 2 / early Phase 3, `InSpectra.Gen/Composition/ServiceCollectionExtensions.cs` must contain only the three `AddInSpectra*` calls plus command registration. It must not name any of: `CliFxMetadataInspector`, `DnlibAssemblyScanner`, `HookInstalledToolAnalysisSupport`, `InstalledToolAnalyzer`, `StaticAnalysisRuntime`, `CommandRuntime`, `OpenCliBuilder`, or any other deep-internal type from any module. If a test in §6 finds such a name in the app shell, the charter has been broken.

### 2.4 Intra-module rules

**Inside `Acquisition`:**

- `Sources/` depend on nothing except `Tooling/` and `Contracts/`.
- `Modes/` depend on nothing except `Tooling/`, `Contracts/`, and `OpenCli`.
- **One mode must not depend on another mode.** Help does not depend on CliFx. Static does not depend on Hook. Etc.
- `Tooling/` must not depend on `Modes/` or `Sources/`.
- `Contracts/` depends on nothing else inside Acquisition.
- **There is no `Modes/Shared/`, `Modes/Common/`, or `Modes/Base/` folder.** Anything reused across modes must live in `Tooling/` (if it is a generic utility) or in `Contracts/` (if it is a data type). A sibling "shared modes" folder would silently become a sixth mode and undo the no-cross-mode rule.

**Inside `Rendering`:**

- Format implementations (`Markdown/`, `Html/`) depend on nothing except `Pipeline/`, `Contracts/`, and `OpenCli`.
- `Markdown/` and `Html/` must not reference each other directly.
- `Pipeline/` must not know about any specific format.

**Inside `OpenCli`:**

- Pure document logic only.
- No process execution, no filesystem orchestration beyond the loader/saver boundary, no NuGet, no rendering, no DI wiring of non-OpenCli types.

---

## 3. Placement rules

When someone adds a file, they ask these questions in order and stop at the first yes.

1. **Is it about the `inspectra` program itself — argv, DI, output envelope, a command class, or an app-level use case orchestrator?**
   → `InSpectra.Gen`.
    - **New command class** → `Commands/<Capability>/<Variant>/`.
    - **New app-level use case** (coordinates one or more modules for a command) → `UseCases/<Capability>/`.
    - **New output formatter** (JSON envelope, human console) → `Output/`.
2. **Is it about the OpenCLI document itself, independent of how it was obtained or rendered?**
   → `InSpectra.Gen.OpenCli`. Place it under the `OpenCli/` subfolder that matches its stage: `Model/`, `Schema/`, `Serialization/`, `Validation/`, or `Enrichment/`.
3. **Is it about turning a target into an OpenCLI document?**
   → `InSpectra.Gen.Acquisition`. Then pick the right seam. The sub-bullets are ordered by precedence: **check them top-to-bottom and stop at the first match**. This resolves apparent overlaps (e.g. a mode-specific type that also happens to be publicly consumable: the mode-internal bullet wins).
    - **New source kind** (exec, package, dotnet, …) → `Acquisition/Sources/<Name>/`.
    - **New acquisition mode** (native, help, cliFx, static, hook, …) → `Acquisition/Modes/<Name>/`.
    - **New static-analysis framework adapter** → `Acquisition/Modes/Static/Frameworks/<Name>/`.
    - **New internal type used by exactly one mode and not referenced outside that mode** (including mode-specific exceptions, parsers, inference helpers) → that mode's own folder, never `Tooling/` and never `Contracts/`. A type is "referenced outside the mode" if any `.cs` file outside `Acquisition/Modes/<ThatMode>/` contains a `using` or type reference to it; mere potential use does not count.
    - **New tool helper reused by ≥ 2 sources or modes.** Examples include process runners, NuGet clients, package archive readers, path normalizers, framework detectors, logging adapters, JSON utilities. This list is illustrative, not exhaustive: anything generic enough to be shared by multiple sources or modes belongs here. → `Acquisition/Tooling/<Category>/`.
    - **New public contract** — a DTO, result type, service interface, or exception that is currently referenced from outside `Acquisition/` (by the app shell, by another module, or by a test project) — → `Acquisition/Contracts/`. A type qualifies for `Contracts/` only when at least one such external reference exists in the repo *at the time the file is added*; potential future use does not qualify. If a type that currently lives in a mode folder later acquires an external caller, it is moved to `Contracts/` in the same PR that adds the caller, not before.
4. **Is it about turning an OpenCLI document into docs?**
   → `InSpectra.Gen.Rendering`.
    - **New output format** → `Rendering/<Format>/`.
    - **New stage in the shared pipeline** (applied regardless of format) → `Rendering/Pipeline/<Stage>/`.
    - **New public contract** exposed to the app shell → `Rendering/Contracts/`.
5. **Does the code execute inside the inspected application process?**
   → `InSpectra.Gen.StartupHook`.
    - **New patched framework** → `StartupHook/Frameworks/<Name>/`.
6. **Is it truly shared by ≥ 2 modules, semantically generic, and objectively unlikely to grow module-specific concerns?**
   → `InSpectra.Gen.Core`. See §3.1 for the precise Core gate.

**Fallthrough.** Every production file must match one of questions 1–6. If a file seems to match none, that almost always means the concept has been drawn at the wrong level; the correct answer is to re-read the five axes in "Why these rules exist" and re-ask the tree with sharper framing. If it still genuinely matches none, the charter must be amended before the file is committed — see §5 for the amendment rule.

**Test code.** This section defines the layout of production code under `src/`. Test projects under `tests/` are free to organize themselves as they see fit — mirroring the `src/` structure is encouraged but not required. The charter's only binding rule for test code is §2.1's test-project exemption (test projects may reference any production assembly) plus the rules in §6 that the policy tests live in `tests/InSpectra.Gen.Tests/`. Nothing else in this charter constrains how tests are organized internally.

### 3.1 The Core gate

`InSpectra.Gen.Core` is deliberately kept tiny. A type qualifies for Core only when **all three** of these gates pass:

1. **Cross-module usage (objective).** The type is **directly referenced** by source code in at least two of the following modules: `InSpectra.Gen.OpenCli`, `InSpectra.Gen.Acquisition`, `InSpectra.Gen.Rendering`, `InSpectra.Gen.StartupHook`, or the app shell (`InSpectra.Gen`). "Directly referenced" means it appears as one of: a `using` target, a base type or interface, a field type, a constructor or method parameter type, a return type, a generic type argument, or a `typeof`/`nameof` expression. Comment mentions, test-only references, and references from inside the same module do not count. Usage entirely within one module does not qualify — a type used by three sub-folders of `Acquisition` is still an Acquisition-internal type.
2. **Semantically generic (objective).** When the type's identifier is split into PascalCase segments, none of the segments appears in this module-specific vocabulary list: `OpenCli`, `Acquisition`, `Rendering`, `StartupHook`, `CliFx`, `Markdown`, `Html`, `NuGet`, `Dnlib`, `Hook`, `Native`, `Help`, `Exec`, `Package`, `Dotnet`. These are the words that name modules or name the variants owned by a specific module; a type containing any of them is module-specific by definition. Generic English words like `Target`, `Source`, `Tool`, `Process`, `Path`, or `Framework` are **not** banned, because they describe concepts that multiple modules legitimately share — so a `CliTargetNotFoundException` or `ProcessRunnerFault` is eligible. Segment matching is exact and case-insensitive; substring matches against the banned list do not count (`SourceControl` is not banned by `Source`).
3. **Promotion requires proven reuse (objective).** A type may only be placed in Core as part of a commit that *also* adds the second consumer. Creating a type in Core on the bet that a second consumer will arrive later is forbidden — Core is populated by moving an already-reused type out of its original module, never by speculation. Types that *will* become cross-cutting (`CliException` and friends) stay in their current owner module until the Phase 3 commit that introduces their second caller.

All three gates must pass. A type that fails any one of them stays in its current owner module.

**Bootstrap order.** The `Core/` folder may be absent entirely until the first qualifying type lands. The §5 seam table row "New shared exception type → `Core/Errors/`" presupposes that Core already exists; before Phase 3 introduces the first cross-module exception move, no `Core/` folder is created. A new exception type that only has one consumer stays in its originating module — it is not placed in a speculative `Core/Errors/` folder and moved later.

**Core substructure.** When Core grows, it grows as named category folders such as `Core/Errors/` or `Core/Primitives/` — one subfolder per category. The §4.2 banned-name list applies inside Core exactly as it applies everywhere else: no `Core/Misc/`, `Core/Util/`, `Core/Common/`, `Core/Shared/`.

### 3.2 Canonical internal shapes

**`InSpectra.Gen`**

```text
src/InSpectra.Gen/
  Program.cs
  Composition/
  Commands/
    Common/                 # shared settings bases
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
    Json/
```

**`InSpectra.Gen.OpenCli`** (today: folder inside `InSpectra.Gen`)

```text
OpenCli/
  Contracts/                # public re-exports of Model + published exceptions
  Model/                    # OpenCliDocument, OpenCliCommand, OpenCliOption, …
  Schema/
  Serialization/            # loader, serializer, cloner
  Validation/
    Documents/
    Options/
    Structure/
  Enrichment/               # XML enrichment, normalization
  Composition/              # AddInSpectraOpenCli
```

The `Contracts/` folder is the surface the app shell and other modules see; most of OpenCli's model types are re-exported from `Model/` via type aliases or public `using` forwards so the canonical types live in exactly one place. `Composition/ServiceCollectionExtensions.cs` is the only entry point the app shell is permitted to call (§2.3).

**`InSpectra.Gen.Acquisition`** (today: separate project)

```text
src/InSpectra.Gen.Acquisition/
  Contracts/                # public interfaces, DTOs, results, exceptions re-exported from Core
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
      Signatures/
      Projection/           # was Help/OpenCli/
    CliFx/
      Crawling/
      Metadata/
      Execution/
      Projection/           # was Analysis/CliFx/OpenCli/
    Static/
      Attributes/
      Inspection/
      Frameworks/
      Projection/           # was StaticAnalysis/OpenCli/
    Hook/
      Invocation/
      Capture/
      Projection/
  Tooling/
    Process/
    NuGet/
    Packages/
    FrameworkDetection/
    Paths/
    Json/
  Composition/              # AddInSpectraAcquisition
```

**`InSpectra.Gen.Rendering`** (today: folder inside `InSpectra.Gen`)

```text
Rendering/
  Contracts/
  Pipeline/
    Model/                  # NormalizedCliDocument, NormalizedCommand, ResolvedOption
  Markdown/
  Html/
    Bundle/                 # was Viewer/
    Assets/
  Composition/
```

**`InSpectra.Gen.StartupHook`**

```text
src/InSpectra.Gen.StartupHook/
  StartupHook.cs
  Frameworks/
    <Framework>/
      ...
```

StartupHook does **not** reference `InSpectra.Gen.OpenCli` at runtime. The hook captures raw command metadata in its own private representation (Plain C# objects, serialized as JSON when they leave the hooked process), and the Hook acquisition mode inside `InSpectra.Gen.Acquisition/Modes/Hook/` is responsible for deserializing that raw capture and projecting it into `OpenCliDocument` via its `Projection/` folder. This keeps StartupHook's forbidden edge `StartupHook → OpenCli` intact: the conversion happens on the Acquisition side of the process boundary, not inside the hook.

---

## 4. Naming rules

### 4.1 Namespaces follow folders

Every namespace must equal `{AssemblyName}.{RelativeFolderPath.Replaced('/', '.')}`. File-scoped namespaces are mandatory. This is already the repo norm; the rule is to keep it.

### 4.2 Banned folder names at the root of a module

These names are not allowed as the primary discovery mechanism at the root of any module. Both the singular and plural forms are banned unless explicitly whitelisted below:

- `Runtime`
- `Infrastructure`
- `Model` / `Models`
- `Support`
- `Helpers`
- `Misc`
- `Common` / `Shared` / `Utils` / `Utilities` / `Base`

**"Concrete owner" defined.** A folder `F/X/` satisfies the concrete-owner rule when the enclosing folder `F` is one of the following — and `F` must be *immediately* above `X`, not two or more levels up:

- A **module root** listed in §1: `InSpectra.Gen`, `OpenCli`, `Acquisition`, `Rendering`, `StartupHook`, `Core`.
- A **§3 sub-seam folder**: any of `Commands/<Capability>/`, `Commands/<Capability>/<Variant>/`, `UseCases/<Capability>/`, `Acquisition/Sources/<Source>/`, `Acquisition/Modes/<Mode>/`, `Acquisition/Modes/Static/Frameworks/<Framework>/`, `Acquisition/Tooling/<Category>/`, `Rendering/<Format>/`, `Rendering/Pipeline/<Stage>/`, `StartupHook/Frameworks/<Framework>/`.
- A **§3.2 canonical-shape stage folder**: any folder named in the canonical shapes of a module as a stage, such as `OpenCli/Validation/`, `OpenCli/Serialization/`, `Acquisition/Modes/Help/Crawling/`, `Rendering/Pipeline/Model/`, etc.

Example: `Acquisition/Modes/Static/Inspection/` is fine because `Static` is a §3 mode-seam folder (a concrete owner), and `Inspection` sits directly below it. `Acquisition/Inspection/` is not, because `Acquisition` is the module root with no mode-seam between it and `Inspection` — `Inspection` at that depth is a generic noun answering no question.

The terms "real owner" and "concrete owner" refer to the exact same definition above. The rest of the charter uses **"concrete owner"** consistently; "real owner" appears only in the heading of this paragraph as a label for the rule itself.

**Depth rule.** Generic names may appear *once*, directly below a concrete owner. They may not appear twice on a path (no `Modes/Static/Common/Shared/`) and they may not themselves acquire sub-generics (no `Commands/Common/Base/`). §2.4's intra-module rules apply in parallel with this depth rule.

**Whitelisted exceptions** (and only these):

- `Commands/Common/` — shared command settings base classes, directly under `Commands/`. Not `Commands/Generate/Common/` or `Commands/Render/Common/`.
- `OpenCli/Model/` — the canonical OpenCLI document model types. Singular `Model` is the domain convention here; `Models` plural is still banned.
- `Rendering/Pipeline/Model/` — the normalized render view model types (`NormalizedCliDocument`, `NormalizedCommand`, `ResolvedOption`). Again singular; plural is banned.

No other `Common`, `Model`, or `Shared` folders are permitted. New whitelist entries require a charter amendment before the code lands.

### 4.3 Reserved semantic roots

Certain words name canonical concepts and may not be reused as folder names in unrelated branches. Reusing them scatters a single concept across the tree. The term "concrete owner" used below is the same one defined in §4.2.

| Reserved root | May only appear as a folder leaf name at | Rationale |
|---|---|---|
| `OpenCli` | The top-level folder of the `InSpectra.Gen.OpenCli` module — i.e. `src/InSpectra.Gen/OpenCli/` during Phase 3 and `src/InSpectra.Gen.OpenCli/OpenCli/` post-Phase 4. (File names like `OpenCliDocument.cs` are not affected by this rule; it applies to folder leaf names only.) | There is exactly one canonical OpenCLI domain. Mode-specific builders are not "OpenCli modules"; they are projections. |
| `Execution` | A folder sitting directly inside one of the `Acquisition/Modes/<AnyMode>/` mode-seam folders (e.g. `Acquisition/Modes/CliFx/Execution/`). Must not sit directly under any module root, and must not sit under `Acquisition/Tooling/` (process-execution primitives live at `Acquisition/Tooling/Process/`, not `Tooling/Execution/`). | "Execution" is not a top-level concern. If it appears, it must describe which mode is executing what. |
| `Documents` | A folder sitting directly inside `OpenCli/Validation/` (e.g. `OpenCli/Validation/Documents/`). Must not sit anywhere else. | "Documents" is ambiguous at module root. It is allowed only as a validation-stage sub-folder inside OpenCli. |

**Mode-specific conversion to OpenCLI** belongs under the mode as either `Projection/` or `Mapping/`. Both names are permitted — pick whichever reads more naturally for that mode, then use it consistently within the mode. `Builder/`, `Converter/`, `OpenCli/`, and any other synonym are forbidden. A single mode must not contain both a `Projection/` and a `Mapping/` folder; it is one or the other. Examples: `Modes/Help/Projection/`, `Modes/CliFx/Projection/`, `Modes/Static/Projection/`.

The reserved tokens `OpenCli`, `Execution`, and `Documents` must not appear as folder leaf names in any position not listed in the table above.

### 4.4 File/type rules

- **One primary type per file.** The file name equals the primary type name. Private nested helpers inside the primary type are fine.
- **Multi-type files must meet all three tests.** A file may contain more than one top-level type only when: (a) it declares at most three top-level types; (b) all of them belong to the same bounded concept (e.g. a single discriminated union, a sealed hierarchy, an enum and its associated attribute); and (c) none of the types exist as standalone concepts outside the file. Files named `*Models.cs`, `*Types.cs`, `*Dtos.cs`, or any other plural-noun catch-all are forbidden regardless of contents.
- **No `*Support.cs` files** unless the file pairs exactly 1:1 with a specific sibling primary-type file in the same folder. **Pairing rule:** for a file named `<X>Support.cs`, the folder must contain a file named exactly `<X>.cs` — i.e. strip the literal suffix `Support` from the base name, and the result must be the base name of a sibling file. Partial matches are not sufficient: `HookCaptureDeserializerSupport.cs` requires exactly `HookCaptureDeserializer.cs` in the same folder, not `HookCapture.cs`. Support files without a matched sibling are a §4.2 violation under a different name. This rule is enforced by §6 Test 12.
- **Interfaces live in the same folder as their primary implementation.** There is no `Abstractions/` folder. The only exception is a module's own `Contracts/` folder, which houses interfaces that cross the module boundary and is already allowed by §2.3.

### 4.5 No `InternalsVisibleTo` to non-test assemblies

If the app needs a type from Acquisition, promote it to a public contract or expose it through the module's composition method. No exceptions.

---

## 5. Allowed extension seams

Extensions must land in a known seam. Inventing a new top-level bucket is a policy violation.

| New thing | Where it goes |
|---|---|
| New source kind (e.g. `WinGet`) | `Acquisition/Sources/WinGet/` |
| New acquisition mode (e.g. `Completions`) | `Acquisition/Modes/Completions/` |
| New static-analysis framework adapter | `Acquisition/Modes/Static/Frameworks/<Name>/` |
| New startup-hook framework patch | `StartupHook/Frameworks/<Name>/` |
| New output format (e.g. `Man`, `Json`) | `Rendering/<Format>/` |
| New OpenCLI transform | `OpenCli/Enrichment/` or `OpenCli/Validation/` |
| New command class | `Commands/<Capability>/<Variant>/` |
| New acquisition tool helper (process, NuGet, package, path) | `Acquisition/Tooling/<Category>/` |
| New shared exception type | `Core/Errors/` |

If none of the existing seams fit, the proposal must amend this charter *before* the code lands. Adding code first and "noting the new bucket later" is not allowed.

---

## 6. Enforcement

Phase 2 adds repository policy tests under `tests/InSpectra.Gen.Tests/` (alongside the existing `RepositoryCodeFilePolicyTests.cs`, which uses `FixturePaths.RepoRoot` and emits `(Path, Reason)` tuples). Every rule in this charter has at least one test. Each test is its own `[Fact]` so failures are targeted. All tests below are implementable in xUnit + `System.IO` + `System.Text.RegularExpressions`; no third-party architecture-testing library (NetArchTest, ArchUnitNET) is required.

The shared test pattern is: enumerate the relevant `.cs` or `.csproj` files under `src/`, collect violations as a list, and fail with a formatted bullet list of `path → reason` entries. Every test message includes the baseline count (if any) so Phase 3 can track convergence.

| # | Rule | Implementation sketch | Baseline |
|---|---|---|---|
| 1 | **Namespace equals folder path.** Every `.cs` file under `src/` declares a file-scoped namespace equal to `{AssemblyName}.{RelativeFolderPath.Replaced('/', '.')}`. **Skip list** (files excluded from this check): `GlobalUsings.cs`, `Program.cs`, `StartupHook.cs` (contractually no namespace), every `AssemblyInfo.cs`, every file whose path contains `/obj/`, `/bin/`, or `/Generated/`, and every file whose first non-empty line contains `<auto-generated>`. | Read each file, extract the first `namespace X;` declaration with regex `^\s*namespace\s+([\w.]+)\s*;`, compute the expected value from the relative path, fail on mismatch. | Should pass today. |
| 2 | **No banned folders except whitelist.** Walk `src/InSpectra.Gen*/` recursively. A folder is a violation if its leaf name matches the §4.2 ban list (`Runtime`, `Infrastructure`, `Model`, `Models`, `Support`, `Helpers`, `Misc`, `Common`, `Shared`, `Utils`, `Utilities`, `Base`) AND its *module-relative path* is not an exact match for one of the §4.2 whitelist entries. **Module-relative path** means: the path from the csproj directory (e.g. `src/InSpectra.Gen/`) to the folder, with forward slashes, with no leading slash. Whitelist entries are matched as exact equality against this module-relative path. The three entries are: `Commands/Common`, `OpenCli/Model`, `Rendering/Pipeline/Model`. A hypothetical `src/SomeOther/Commands/Common` in a different csproj would not be whitelisted, because module-relative matching is scoped to the csproj that contains the folder. | Directory enumeration, leaf-name check, module-relative equality compare. No glob trickery. | **Fails** today: `InSpectra.Gen/Runtime/`, `InSpectra.Gen/Models/`, `InSpectra.Gen.Acquisition/Runtime/`, `InSpectra.Gen.Acquisition/Infrastructure/`. |
| 3 | **Reserved semantic roots.** Walk all folders under `src/InSpectra.Gen*/`. The test inspects **folder leaf names only** — file names like `OpenCliDocument.cs` are never considered. For every folder whose leaf name equals `OpenCli`, `Execution`, or `Documents`, check the allowed-positions rules below. Any folder that matches none of its rule is a violation. **Allowed positions** (each rule specifies a set of permitted module-relative parent-folder paths; module-relative path is defined in Test 2): *(a)* leaf `OpenCli` is allowed only when the folder's module-relative path equals exactly `OpenCli` inside the `InSpectra.Gen` csproj (transitional) or the `InSpectra.Gen.OpenCli` csproj (post-Phase-4); *(b)* leaf `Execution` is allowed only when its immediate parent's module-relative path matches `Modes/<AnyMode>` inside the `InSpectra.Gen.Acquisition` csproj, where `<AnyMode>` is any one of `Native`, `Help`, `CliFx`, `Static`, `Hook`. During Phase 3 the test is **also** permitted to accept legacy pre-migration parents listed here explicitly: `Analysis/CliFx`, `Analysis/Hook`, `Help`, `StaticAnalysis`. Any parent not in one of these two sets is a violation. `Execution` under `Acquisition/Tooling` is never allowed. *(c)* leaf `Documents` is allowed only when its immediate parent's module-relative path equals `OpenCli/Validation` (transitional: inside `InSpectra.Gen`; post-Phase-4: inside `InSpectra.Gen.OpenCli`). | Linear folder walk, constant work per folder, set-based parent-path lookup. | **Fails** today (`InSpectra.Gen/OpenCli/Documents/`, `InSpectra.Gen.Acquisition/Analysis/CliFx/OpenCli/`, `Help/OpenCli/`, `StaticAnalysis/OpenCli/`, `Analysis/Execution/`). |
| 4 | **No non-test `InternalsVisibleTo`.** Scan every `.cs` file under `src/` (not just `AssemblyInfo.cs` — the attribute is legal in any file) for `[assembly: InternalsVisibleTo("...")]`. Also scan every `.csproj` under `src/` for `<InternalsVisibleTo Include="..." />` MSBuild items. For each target assembly name found, require it to end in `.Tests` (case-insensitive); anything else is a violation. | Regex `\[assembly:\s*InternalsVisibleTo\("([^"]+)"\)\]` across all `.cs`; XML item scan across all `.csproj`. | **Fails** today: `src/InSpectra.Gen.Acquisition/Properties/AssemblyInfo.cs:4` → `inspectra`. |
| 5 | **Project reference direction.** Parse every `.csproj` under `src/` for `<ProjectReference Include="..." />`. Build an edge list `(from, to)` where `from` and `to` are csproj file names (minus `.csproj`). Every production-to-production edge must match the §2.1 allowed-directions table. Test projects (`*.Tests.csproj`) are exempt by §2.1's last row: they may reference any production project. | XML parse + table lookup. | Should pass today. |
| 6 | **App does not reference deep module internals.** Scan every `.cs` file under `src/InSpectra.Gen/` **except** `src/InSpectra.Gen/Composition/ServiceCollectionExtensions.cs` (that single file is the composition entry point and is exempt per §2.3). For each file, collect every `using InSpectra.Gen.<Module>…;` directive where `<Module>` is `Acquisition`, `OpenCli`, `Rendering`, or `StartupHook`. Classification rule: split the directive on `.`, starting at the top: segments `[0]='InSpectra'`, `[1]='Gen'`, `[2]=<Module>`, `[3]=<Sub>` (if present), …. **A directive of length 3** (`using InSpectra.Gen.<Module>;` — directly importing the module root namespace) is a violation, because it implies reaching into the module without going through `Contracts` or `Composition`. **A directive of length ≥ 4** is allowed iff segment `[3]` equals literally `Contracts` or `Composition`. Anything else is a violation. Do not use a negative-lookahead regex — use explicit string split and index checks. | String split; O(n) per file. | **Fails** today (the exempt file has 15+ deep-internal types, and several non-exempt files — under `Commands/`, `OpenCli/Acquisition/`, `Runtime/Acquisition/` — import `InSpectra.Gen.Acquisition.Runtime.*`). |
| 7 | **Modes do not reference other modes.** Enumerate `src/InSpectra.Gen.Acquisition/Modes/<X>/` folders. For each folder `X`, scan every `.cs` file beneath it for any substring match of `InSpectra.Gen.Acquisition.Modes.<Y>` where `Y ≠ X`, in either a `using` directive or a fully qualified type reference in the file body. | Two-pass string search. | Gated `Skip = "enabled when Acquisition/Modes/ exists (Phase 3)"`. Passes vacuously until then. Must be un-skipped in the same PR that introduces the first `Modes/<X>/` folder. |
| 8 | **Markdown and Html do not reference each other.** Scan `src/InSpectra.Gen/Rendering/Markdown/**/*.cs` (plus the post-Phase-4 assembly path `src/InSpectra.Gen.Rendering/Markdown/`) for any `using InSpectra.Gen.Rendering.Html`; scan the symmetric Html path for any `using InSpectra.Gen.Rendering.Markdown`. Both formats may depend on `InSpectra.Gen.Rendering.Pipeline`, `InSpectra.Gen.Rendering.Contracts`, and `InSpectra.Gen.OpenCli.*`. | Regex scan with two direction checks. | Should pass today. |
| 9 | **OpenCli does not reference other modules.** Scan every `.cs` file whose path is under `InSpectra.Gen/OpenCli/` (transitional) or `InSpectra.Gen.OpenCli/` (post-Phase-4) for any `using` that starts with one of these **seven literal forbidden prefixes**: `InSpectra.Gen.Acquisition`, `InSpectra.Gen.Rendering`, `InSpectra.Gen.StartupHook`, `InSpectra.Gen.Commands`, `InSpectra.Gen.UseCases`, `InSpectra.Gen.Output`, `InSpectra.Gen.Composition`. The forbidden list IS the enumerated set of app-shell namespace roots — no pattern matching, no guessing. | Regex scan; literal-prefix blacklist. | **Fails** today (the current `InSpectra.Gen/OpenCli/Acquisition/` subfolder imports `InSpectra.Gen.Runtime.Acquisition` and similar legacy roots). Turns green when those files move to `UseCases/Generate/` in Phase 3. |
| 10 | **`Modes/Shared/` and siblings forbidden.** No folder named `Shared`, `Common`, `Base`, `Utils`, `Utilities`, or `Helpers` may exist as an immediate child of `Acquisition/Modes/`. | Single-level directory enumeration. | Gated `Skip = "enabled when Acquisition/Modes/ exists (Phase 3)"`. Passes vacuously until then. |
| 11 | **Composition methods exist, are public, and have the correct signature.** Via reflection on loaded assemblies from the test project's own probe path, locate three methods: `AddInSpectraOpenCli`, `AddInSpectraAcquisition`, `AddInSpectraRendering`. **Assembly scope:** during Phase 3 while `OpenCli`, `Rendering`, and `Core` are still folders inside `InSpectra.Gen`, the test loads the single `InSpectra.Gen.dll` assembly (the `typeof(InSpectra.Gen.Program).Assembly` handle is sufficient) and searches all types within it. Post-Phase-4, when each module becomes a separate assembly, the test loads each module's assembly by name (`InSpectra.Gen.OpenCli.dll`, `InSpectra.Gen.Acquisition.dll`, `InSpectra.Gen.Rendering.dll`) via `Assembly.Load` and searches each one. The test code must detect which phase the repo is in by checking which assemblies exist in the probe path, and choose the appropriate scope automatically. Each method must satisfy: *(a)* `public static`; *(b)* marked as an extension method on `IServiceCollection` via `[Extension]` / `this IServiceCollection` first parameter; *(c)* returns `IServiceCollection`; *(d)* declared in a type named `ServiceCollectionExtensions` under a namespace whose last segment is literally `Composition`; *(e)* parameter count is exactly 1 (the `this IServiceCollection`) or exactly 2, where the second parameter's type is an `Action<TOptions>` or `Action<TOptions>?` for some `TOptions`. Any additional parameter shapes (e.g. `string connectionString`) are violations even if the method compiles. Note: `StartupHook` is intentionally excluded — it does not expose a composition method because it is not DI-wired. | `Assembly.Load` + `GetTypes()` + `MethodInfo.GetParameters()` + `GetCustomAttribute<ExtensionAttribute>`. | **Fails** today (none exist yet). Added as a baseline-failing test during Phase 3 so the green transition is visible. |
| 12 | **`*Support.cs` exact-pair rule (§4.4).** For each file matching `*Support.cs` under `src/`, compute `strippedName = fileBaseName.Substring(0, fileBaseName.Length - "Support".Length)` (i.e. remove the literal `Support` suffix from the base name). A sibling file named exactly `strippedName + ".cs"` must exist in the same folder. Example: `HookCaptureDeserializerSupport.cs` requires exactly `HookCaptureDeserializer.cs` — not `HookCapture.cs`, not `HookCaptureDeserializerImpl.cs`. Shorter prefixes do not satisfy the rule. This rule is a charter extension not explicitly required by `Task.md`; it codifies the "no poorly-named helpers" principle by catching orphan `*Support.cs` files that would otherwise slip past §4.2's folder-name rules. | Directory enumeration + exact filename compare. | **Fails** today in several Acquisition folders (e.g. `Help/Inference/*Support.cs`, `Infrastructure/Commands/*Support.cs` without a directly-named paired sibling). Baseline failures are the §4.4 cleanup target. |
| 13 | **A single mode uses only one of `Projection/` or `Mapping/`.** Enumerate every `Acquisition/Modes/<X>/` folder. If the folder contains a direct child named `Projection` AND a direct child named `Mapping`, that is a violation. Having exactly one of the two (or neither) is allowed. | Directory glob, one level deep inside each mode folder. | Gated `Skip = "enabled when Acquisition/Modes/ exists (Phase 3)"`. Passes vacuously until then. |
| 14 | **`Core/` has no outbound module references.** Scan every `.cs` file under `InSpectra.Gen/Core/` (transitional) or `InSpectra.Gen.Core/` (post-Phase-4) for any `using` directive that starts with one of these **eight literal forbidden prefixes** (Test 9's seven prefixes followed by `InSpectra.Gen.OpenCli` as the eighth): `InSpectra.Gen.Acquisition`, `InSpectra.Gen.Rendering`, `InSpectra.Gen.StartupHook`, `InSpectra.Gen.Commands`, `InSpectra.Gen.UseCases`, `InSpectra.Gen.Output`, `InSpectra.Gen.Composition`, `InSpectra.Gen.OpenCli`. Any hit is a violation. Core sits below OpenCli in the dependency graph and cannot reference it, which is why the OpenCli prefix is added. This enforces `Core → nothing` from §2.1. | Regex scan; literal-prefix blacklist. | Passes vacuously today because `Core/` does not exist yet. Becomes live the first time any file is placed under `Core/`. No Skip attribute needed — the test body short-circuits when the folder is absent. |

Tests 2, 3, 4, 6, 9, 11, and 12 will fail against the current tree when first added. That is the intended baseline. Phase 3 moves the code to turn them green one by one. **The tests are committed *before* the moves so each move has an objective definition of "done". No new code may make an already-passing test fail.**

**Baseline snapshots.** For each test whose baseline is not zero, the expected baseline count is written as a literal constant inside the test itself (e.g. `const int ExpectedBaselineViolations = 15;`). The test body counts violations and fails if the count is *greater* than the constant; it does not fail when the count is equal or smaller. When a Phase 3 move reduces the count, the same PR updates the constant to the new (lower) value, so baselines only ratchet down. No external baseline file is used. A test whose baseline is zero (e.g. Test 8, Test 10 once un-gated) does not need a constant.

Tests 7, 10, and 13 depend on folders that do not yet exist. They are marked `Skip = "enabled when Acquisition/Modes/ exists (Phase 3)"` and must be un-skipped in the same PR that introduces the first `Modes/<X>/` folder. Test 14 uses a different strategy — it runs unconditionally and short-circuits when `Core/` is absent — because Core may appear at any time and must be guarded from the moment it first exists.

---

## 7. Rollout (reference)

This charter corresponds to Phase 1 of the restructure plan in `docs/Tasks/Restructure/Task.md`.

1. **Phase 1 — Charter.** This document. No code changes.
2. **Phase 2 — Enforce.** Add the policy tests from §6. They initially fail against the known violations; that is the baseline. New code must not add new failures.
3. **Phase 3 — Move files without behaviour changes.** Apply the move-map from `Task.md`. Kill `Runtime/` as a catch-all, introduce `Modes/`, rename mode-specific `OpenCli/` folders to `Projection/`, consolidate process execution into `Acquisition/Tooling/Process/`, centralize the OpenCLI domain.
4. **Phase 4 — Split assemblies.** Extract `InSpectra.Gen.OpenCli`, `InSpectra.Gen.Rendering`, and (optionally) `InSpectra.Gen.Core` as separate projects once their internal boundaries are clean.

Steps 2 and 3 can overlap: start moving files as soon as the corresponding policy test exists to keep the target shape honest.

---

## 8. The one-sentence restatement

**App shell** orchestrates thinly. **OpenCLI** owns the document. **Acquisition** turns targets into documents. **Rendering** turns documents into docs. **Startup hook** captures from inside. Inside each: **capability first, variant second, mechanism third.**

New work lands in a known seam. When no seam fits, amend this charter first.
