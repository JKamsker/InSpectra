# Follow-up Smell Catalog

This is the seed smell library for follow-up investigation swarms. Use it
together with [Runbook](Runbook.md). Extend it only when a genuinely new smell
family is discovered and recorded in [Logbook](Logbook.md).

## Structural

1. **Forbidden top-level buckets** inside any project root: `Runtime`,
   `Infrastructure`, `Models`, `Support`, `Helpers`, `Misc`. Enforced by
   `ArchitectureForbiddenBucketsTests` at the project level, but a sub-folder
   named `Support/` one level in is still a smell worth auditing.
2. **Mini-misc buckets**: folders mixing two different ownership ideas, such
   as the F1 `Targets/` and `Execution/` split. Look for folders where half
   the files belong to concept A and half to concept B.
3. **Duplicate semantic roots**: the same concept (`OpenCli`, `Execution`,
   `Documents`) as a folder name in multiple unrelated branches. The charter
   says not to repeat semantic roots unless they truly mean the same owned
   concept. The F2 canonical-OpenCli merge is the reference fix.
4. **Flat mode / flat module subtrees**: a mode or logical module whose files
   all live at one level while its siblings have substructure. Phase F4 fixed
   `Modes/Hook/` this way. Check all other `Modes/*/` and peer sibling sets
   for similar drift.
5. **Namespace ↔ folder mismatches**: the active architecture test catches
   these at build time, but only for `.cs` files. Check block-scoped
   namespaces and files that deliberately avoid namespace declaration, such as
   `StartupHook.cs`, for new exceptions.
6. **Junk multi-type files**: the charter explicitly bans `*Models.cs` type
   dumping grounds. Grep for files defining more than one top-level
   `class`/`record`/`interface` where the types are not tightly coupled.

   Do not key the search on filename. The g1 phase found 7 files that matched
   the pattern, but the g4 phase then found 5 more multi-type files whose
   filenames looked like service names:
   `HookToolProcessInvocationResolver.cs`, `ToolDescriptorResolver.cs`,
   `DotnetRuntimeCompatibilitySupport.cs`, `DotnetToolSettingsReader.cs`,
   `OpenCliCommandTreeBuilder.cs`.

   The authoritative scan is by type count:

   ```bash
   for f in $(find src -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*"); do
     count=$(grep -cE "^(public |internal |file )?(sealed |abstract |static |partial |readonly )*(class|record|interface|struct|enum) [A-Z]" "$f")
     [ "$count" -gt 1 ] && echo "$count  $f"
   done | sort -rn
   ```

   Legitimate exceptions:

   - **Sealed discriminated unions**: an abstract base type followed by
     several sealed concrete variants is a closed algebraic data type.
     Splitting hurts readability because the whole case analysis lives
     together. `SystemCommandLineMethodValues.cs` and
     `SystemCommandLineConstructorValues.cs` are the reference examples.
   - **Tight external-API DTO clusters**: if the types collectively model a
     single external contract and are only ever used together, rename the file
     off the `*Models.cs` anti-pattern but keep the cluster inline.
     `NuGetApiDtos.cs` and `NuGetApiSpecDtos.cs` are the reference examples.
   - **Service + intermediate DTO**: if the secondary type is referenced only
     from within the same file, the inline pairing is acceptable. The check is
     `git grep -l TypeName | wc -l`; if the count is `1`, the type is
     file-private in practice even if declared `internal`.
   - **Related constant classes**: `AnalysisConstants.cs` declares
     `AnalysisMode`, `AnalysisDisposition`, and `ResultKey` together and they
     belong as the enum-like vocabulary for the acquisition layer.

## Dependency / Layering

7. **Cross-mode imports**: enforced by a test, but the raw grep is still a
   useful gut check:

   ```text
   grep -rn "using InSpectra\.Gen\.Engine\.Modes\.\(Help\|CliFx\|Hook\|Static\)" \
     src/InSpectra.Gen.Engine/Modes/
   ```

8. **Tooling → Modes**: enforced. Re-run the raw grep anyway.
9. **Contracts → Tooling / Contracts → Modes**: enforced. Same gut check.
10. **Cycles inside Gen** between `Commands`, `UseCases`, `Rendering`,
    `OpenCli`, `Output`, and `Execution`. The F2 fix documents the pattern.

    As of the current thin-shell queue handling, this is partly test-enforced by
    `ArchitectureGenInternalLayeringTests`:

    - `Shell_output_does_not_depend_on_commands`
    - `Engine_opencli_does_not_depend_on_rendering_or_use_cases`
    - `Engine_rendering_does_not_depend_on_use_cases`
    - `Engine_execution_does_not_depend_on_modes_rendering_or_use_cases`
    - `Engine_targets_does_not_depend_on_modes_or_rendering`

    The helper also asserts `filesScanned > 0` so an accidental folder rename
    cannot trip a vacuous green.

    Raw grep / gut checks:

    ```text
    grep -rn "using InSpectra\.Gen\.Commands" src/InSpectra.Gen/Output/
    grep -rn "using InSpectra\.Gen\.Engine\.\(Rendering\|UseCases\)" src/InSpectra.Gen.Engine/OpenCli/
    grep -rn "using InSpectra\.Gen\.Engine\.UseCases" src/InSpectra.Gen.Engine/Rendering/
    grep -rn "using InSpectra\.Gen\.Engine\.\(Modes\|Rendering\|UseCases\)" src/InSpectra.Gen.Engine/Execution/
    grep -rn "using InSpectra\.Gen\.Engine\.\(Modes\|Rendering\)" src/InSpectra.Gen.Engine/Targets/
    ```

    Additional roots not covered by the 5 facts, such as `Composition/`, are
    still grep-only unless promoted.
11. **App-shell → deep internals**: active tests enforce that `InSpectra.Gen`
    may only import from `InSpectra.Gen.Engine.Composition`,
    `InSpectra.Gen.Engine.Contracts`,
    `InSpectra.Gen.Engine.UseCases.Generate`, and
    `InSpectra.Gen.Engine.Rendering.Contracts`, and the new
    `ArchitectureEnginePublicSurfaceTests` guard keeps engine implementation
    types from leaking publicly outside those contract-oriented namespaces.
    The shell-source scan is still regex-based, so fully-qualified, alias, or
    `global using static` edges still merit manual audit. Also check
    `InSpectra.Gen` → `InSpectra.Gen.StartupHook` edges, which do not have
    the same coverage.
12. **Dead `using` statements**: always a tell after a move. `dotnet build`
    will not fail, but a regex scan will surface stale paths.

## Composition / Wiring

13. **Overbroad composition seams**: `AddInSpectra*()` methods registering
    services that do not belong to the module they name. F3's OpenCli seam
    narrowing is the reference. Audit:
    - `AddInSpectraOpenCli`
    - `AddInSpectraGenerateUseCases`
    - `AddInSpectraRendering`
    - `AddInSpectraEngine`
    - `AddTargetServices`
14. **Non-test `InternalsVisibleTo`**: enforced by a test. Also check each
    `AssemblyInfo.cs` for unexpected attributes.
15. **`[ModuleInitializer]` with `CA2255` suppression**: phase 2a uses this
    as a deliberate workaround for a layering constraint. Every other
    occurrence needs a documented justification.
16. **DI registrations duplicating types**: if a type is registered in two
    different composition methods, only the last registration wins. Grep all
    `ServiceCollectionExtensions` files for duplicates.

## Type / API Smells

17. **Type erasure with `object` plus runtime cast**: phase 2a's
    `CliFrameworkProvider.StaticAnalysisFrameworkAdapter.Reader : object` is a
    charter-motivated workaround. Any other `object` field immediately cast at
    every use site is a smell unless it carries an explicit rationale.
18. **Missing `CancellationToken` on async methods**: scan public async
    methods. The charter does not say this explicitly, but the C# coding-style
    rule does.
19. **Synchronous I/O on async paths**: `File.ReadAllText`, `File.Exists`, and
    similar inside async methods are usually bugs unless sync-over-async is an
    explicit choice.
20. **Silent catch blocks / swallowed exceptions**: `catch (Exception)`
    followed by an empty block or vague logging. Each one needs either a
    rethrow or a structured error.
21. **`*Service` naming for types that are not actually services**, or
    `*Support` naming for types that are really domain behavior. Rename or
    split.
22. **Multiple unrelated top-level types per file**: split into one file per
    primary type unless a documented exception applies.
23. **Public types that should be `internal`**: anything inside a module that
    is not exposed through the engine's root `Composition/`, `Contracts/`,
    `Rendering/Contracts/`, or public use-case seams should usually be
    `internal`. Cross-check with `InternalsVisibleTo` and the
    `ArchitectureEnginePublicSurfaceTests` exported-type guard.

## Documentation / Test Hygiene

24. **Stale comments**: phrases like "currently skipped", "will be done in
    phase N", and "moved to X" are strong follow-up candidates.
25. **Dead code**: methods or files with no callers.
26. **Stale XML doc comments**: `<see cref="..."/>` pointing at types that
    moved or were deleted.
27. **TODO / FIXME / HACK comments**: enumerate them and force a decision:
    tracked issue, stale and deletable, or real outstanding work.

## Test / Fixture Smells

28. **Stale snapshot fixtures**: if a live suite regenerates snapshots, any
    changed file is a smell until either the fixture or the code is reconciled.
29. **Trivially green tests**: tests whose assertions cannot fail because the
    enumeration returns zero items. Guard them with count assertions.
30. **Orphan test data**: files under `tests/.../TestData/` or similar that no
    active test reads.
