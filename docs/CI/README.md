# InSpectra CI/CD Integration Guide

Generate InSpectraUI documentation in your CI pipeline. One workflow call,
zero hand-maintained dependencies.

## Why a CI step?

`inspectra` runs locally just fine — but most teams want their CLI reference
to update automatically as the source changes. This guide covers the
**`JKamsker/InSpectra@v1`** GitHub Action and the reusable workflow it ships
with: how to call them, what every input does, and the most common end-to-end
pipelines (deploy to GitHub Pages, open a PR with regenerated Markdown,
attach to a release).

## Contents

- [Usage examples](usage.md) — every flavor of "render docs in CI" with copy-pasteable YAML
- [Inputs reference](inputs.md) — every action input, default, and which mode it applies to
- [Recipes](recipes.md) — full end-to-end pipelines (GitHub Pages, docs PR, release asset)

## TL;DR — generate from a `.csproj`, then render automatically

```yaml
# .github/workflows/docs.yml
name: Docs

on:
  push:
    branches: [main]
  workflow_dispatch:

jobs:
  generate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: JKamsker/InSpectra@v1
        with:
          mode: dotnet
          project: src/MyCli         # path to .csproj or directory
          configuration: Release
          format: html               # html / markdown / markdown-monolith / markdown-hybrid
          output-dir: docs/cli

      - uses: actions/upload-artifact@v4
        with:
          name: cli-docs
          path: docs/cli
```

That's the entire setup. The action takes care of:

1. **.NET SDK installation** — reads your project's `<TargetFramework>` and
   installs the matching SDK. Versions already on the runner are skipped.
2. **`InSpectra.Cli` package** — auto-added to your `.csproj` so
   `cli opencli` and `cli xmldoc` work without any source changes. Skipped
   if you already reference it.
3. **`InSpectra.Gen` tool** — installed from NuGet, then used to generate an
   enriched `opencli.json` and render it into the format you asked for.
4. **XML enrichment** — auto-detected when your CLI exposes `cli xmldoc`,
   so the generated `opencli.json` is already enriched before rendering.

You don't need a separate `actions/setup-dotnet` step, you don't need
`dotnet tool install`, and you don't need to add any `PackageReference`
to your project by hand.

## When to use which mode

| Mode | Use when | Required input |
|---|---|---|
| `dotnet` | The CLI source lives in the same repo and you want every commit to produce fresh docs | `project` |
| `package` | You want docs generated from a published .NET tool package without installing it globally first | `package-id`, `package-version` |
| `exec` | You have a pre-built binary, a globally installed .NET tool, or any executable on PATH | `cli-name` |
| `file` | You've already exported `opencli.json` (e.g. checked into the repo) and just want to render it | `opencli-json` |

[Usage examples →](usage.md)

## Prerequisites

Your CLI must support the OpenCLI specification.

- **For `dotnet` mode** (recommended for in-repo CLIs): nothing — the action
  adds the `InSpectra.Cli` package for you, and `Spectre.Console.Cli`-based
  CLIs get `cli opencli` / `cli xmldoc` automatically once the package is
  referenced.
- **For `exec` mode**: your CLI needs to implement `cli opencli` (and
  optionally `cli xmldoc`). Adding the `InSpectra.Cli` NuGet package to your
  project is the easiest way.
- **For `file` mode**: export your `opencli.json` once with `inspectra generate …`
  (or run the binary's `cli opencli` and redirect stdout) and check it into
  the repo.

If your CLI uses custom export arguments (not `cli opencli`), pass them via
`opencli-args`. Same for `xmldoc-args`.

## See also

- The top-level [README](../../README.md) for the CLI itself
- [`JKamsker/InSpectra`](https://github.com/JKamsker/InSpectra) on GitHub
- The [OpenCLI specification](https://opencli.org/)
