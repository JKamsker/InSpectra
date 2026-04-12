# Todo Next: Installed-Tool Process Safety

- ID: `TN-2026-04-12-02`
- State: `Completed`
- Added: `2026-04-12`
- Source: outer iteration 8 fresh-swarm ranking on pushed tip `a08c0f2`
- Queue entry: [Todo Next Queue](../TodoNext.md)
- Pickup rule: this item is the active implementation phase selected from the
  post-`a08c0f2` fresh swarm.
- Exit rule: mark the queue item `Completed`, `Deferred`, or `Rejected` with
  dated rationale and update [Logbook](../Logbook.md) accordingly.
- Landed on: `g41` (`29a526c`)
- Hosted validation: green `pull_request` run `24300030057`
- Final local validation:
  - `dotnet test InSpectra.Gen.sln --no-restore` ✅ (`185 / 0 / 0`
    `InSpectra.Gen.Engine.Tests`, `169 / 0 / 0` `InSpectra.Gen.Tests`)
  - `dotnet test tests/InSpectra.Gen.Tests/InSpectra.Gen.Tests.csproj --no-restore --filter Architecture`
    ✅ (`17 / 0 / 0`)

## Outcome

`g41` completed the queued installed-tool process-safety slice:

- help, CliFx, hook, package-native, and package-xmldoc flows now preserve the
  real caller working directory while scoping cleanup to engine-owned sandbox
  roots only
- the public dispatcher seam keeps cleanup authority internal-only, while the
  installed-tool/package path forwards engine-owned cleanup roots through the
  internal dispatcher and native process runner seams
- timeout cleanup now escalates even when output drain succeeds, dotnet-hosted
  sandbox cleanup matches managed entry-command lines, hook compatibility/help
  retries replay correctly after environment changes, and compatibility retries
  now override stale pre-existing env values instead of stopping on key
  presence
- CliFx root-qualified child command names are normalized before enqueueing, so
  the crawler no longer duplicates parent/root segments on follow-up captures
- regression coverage now covers null cleanup-root boundaries, dispatcher and
  OpenCli propagation seams, timeout cleanup, dotnet-hosted sandbox matching,
  hook retry replay, compatibility env overrides, native process cleanup
  forwarding, and root-qualified CliFx command entries

## Why This Phase Goes First

The fresh swarms surfaced several open HIGH clusters. This phase was selected
first because it closes a runtime safety issue with a contained write set:

- installed-tool help/hook crawls still pass the caller working directory as the
  sandbox cleanup root
- cancellation or stalled output draining can therefore escalate cleanup to the
  user workspace instead of an engine-owned sandbox
- the issue is clustered enough to fix without mixing unrelated UI/CI or
  packaging work into the same phase

## Confirmed Problem Statement

The installed-tool analyzers need two different directories:

- the real command working directory, which may legitimately be the caller
  workspace
- the cleanup root for engine-owned transient state, which should be limited to
  sandbox directories created by the engine

The current implementation still conflates those roles in the help/hook crawl
path. Representative evidence from the fresh swarms:

- `Crawler` and `CliFxHelpCrawler` pass `workingDirectory` through to
  `DotnetRuntimeCompatibilitySupport.InvokeWithCompatibilityRetriesAsync(...)`
- `HookInstalledToolAnalysisSupport` passes `request.WorkingDirectory` as the
  `sandboxRoot`
- `CommandProcessSupport.TerminateSandboxProcesses(...)` walks the process table
  and kills candidates by executable-path prefix match under the supplied root

That makes the cleanup scope depend on the caller workspace instead of the
engine-managed sandbox state created for installed-tool analysis.

## Planned Scope

Keep the phase within a single process-safety slice:

- derive the cleanup root from the engine-managed sandbox environment rather
  than the caller working directory
- preserve the real command working directory semantics
- add focused regression tests for the help/hook path

Expected file cluster:

- `src/InSpectra.Gen.Engine/Tooling/Process/CommandSandboxEnvironmentSupport.cs`
- `src/InSpectra.Gen.Engine/Modes/Help/Crawling/Crawler.cs`
- `src/InSpectra.Gen.Engine/Modes/CliFx/Crawling/CliFxHelpCrawler.cs`
- `src/InSpectra.Gen.Engine/Modes/Hook/Execution/HookInstalledToolAnalysisSupport.cs`
- targeted engine tests covering the new sandbox-root behavior

## Deferred To Later Phases

These remain open but are intentionally not mixed into this phase:

- hosted Playwright CI wiring and Playwright stabilization
- static HTML feature-contract gaps (`--enable-url`, `--show-home`,
  `--enable-nuget-browser`, `--enable-package-upload`)
- StartupHook package end-to-end verification
- installed-tool multi-TFM determinism
- stdout/timeout diagnostic preservation
