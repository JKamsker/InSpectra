# InSpectra

[![CI](https://github.com/JKamsker/InSpectra/actions/workflows/ci.yml/badge.svg)](https://github.com/JKamsker/InSpectra/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/InSpectra.Gen?logo=nuget&label=InSpectra.Gen)](https://www.nuget.org/packages/InSpectra.Gen)
[![NuGet Downloads](https://img.shields.io/nuget/dt/InSpectra.Gen?logo=nuget&label=downloads)](https://www.nuget.org/packages/InSpectra.Gen)
[![.NET](https://img.shields.io/badge/.NET-10.0-512bd4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Website](https://img.shields.io/badge/website-inspectra.kamsker.at-blue)](https://inspectra.kamsker.at)

**Turn any CLI's [OpenCLI](https://opencli.org/) spec into beautiful, navigable documentation — Markdown or interactive HTML.**

> Website: [inspectra.kamsker.at](https://inspectra.kamsker.at) | [Live examples](https://inspectra.kamsker.at/examples/inspectra/) | [Try it](https://inspectra.kamsker.at/try/)

---

## Table of Contents

- [Features](#features)
- [Install](#install)
- [Quick Start](#quick-start)
- [GitHub Action](#github-action)
- [Reusable Workflow](#reusable-workflow)
- [CLI Reference](#cli-reference)
- [HTML Viewer](#html-viewer-inspectraui)
- [Architecture](#architecture)
- [Project Layout](#project-layout)
- [Contributing](#contributing)
- [Examples](#examples)

## Features

- **Markdown output** — GitHub-friendly single file, tree layout (one file per command), or hybrid layout (README + group files when groups exist; leaf-only CLIs may emit README only)
- **Interactive HTML viewer** — relocatable SPA bundle with sidebar navigation, search, dark/light theme, and deep-link hash routing
- **Command composer** — interactively build CLI invocations from documented options and arguments
- **Command palette** — fuzzy search across all commands (Ctrl+K)
- **NuGet browser** — search and explore indexed .NET CLI tool packages
- **XML enrichment** — additive metadata from XML docs, only fills missing descriptions
- **Validation first** — broken specs fail early, before any rendering
- **Acquisition modes** — native OpenCLI export, help crawling, CliFx analysis, static analysis, and startup-hook capture
- **Raw OpenCLI generation** — emit enriched `opencli.json` directly from a package, executable, or .NET project
- **Two-step pipeline** — generate a canonical `opencli.json`, then render Markdown or HTML from that saved artifact
- **GitHub Action** — one-step CI integration for any .NET CLI tool
- **Secure by default** — generated pages expose only the features you explicitly enable

## Install

### As a .NET global tool

```bash
dotnet tool install -g InSpectra.Gen
```

This installs the `inspectra` command globally.

### As a GitHub Action

```yaml
- uses: JKamsker/InSpectra@v1
  with:
    mode: dotnet
    project: src/MyCli       # path to your .csproj
    format: markdown         # or html
```

See [GitHub Action](#github-action) for full documentation, including the
auto-installed `InSpectra.Cli` package and SDK detection.

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Node.js (only needed for local frontend builds, CI, and `dotnet pack` / `dotnet publish`)

### Generate from a live CLI (exec mode)

```bash
# Generate an enriched OpenCLI document
inspectra generate exec mycli --with-xmldoc --out ./opencli.json
```

### Generate from a .NET project on disk (dotnet mode)

```bash
# Point at a .csproj or directory containing one
inspectra generate dotnet src/MyCli \
  --configuration Release \
  --no-build \
  --with-xmldoc \
  --out ./opencli.json
```

### Generate from a published .NET tool package

```bash
inspectra generate package JellyfinCli --version 1.1.0 --out ./opencli.json
```

### Render from a saved OpenCLI JSON file

```bash
# HTML bundle
inspectra render file html opencli.json --out-dir ./docs

# Markdown with XML enrichment
inspectra render file markdown opencli.json \
  --xmldoc xmldoc.xml \
  --out docs.md
```

The intended flow is:

1. `inspectra generate ...` from a project, executable, or package.
2. `inspectra render file ...` from that generated `opencli.json`.

Open `./docs/index.html` in a browser. The bundle is relocatable because the viewer is built with `base: "./"`.

## GitHub Action

The `JKamsker/InSpectra@v1` action generates interactive CLI documentation in your CI pipeline.

> Looking for a deeper guide? See **[`docs/CI/`](docs/CI/README.md)** for the
> full integration manual: [usage patterns](docs/CI/usage.md),
> [inputs reference](docs/CI/inputs.md), and end-to-end
> [recipes](docs/CI/recipes.md) (Pages, docs PR, drift check, release asset).

### Basic usage (exec mode)

```yaml
steps:
  - uses: actions/checkout@v4

  - uses: JKamsker/InSpectra@v1
    with:
      cli-name: mycli
```

### Generating docs for a .NET tool

```yaml
steps:
  - uses: actions/checkout@v4

  - uses: JKamsker/InSpectra@v1
    with:
      cli-name: mycli
      dotnet-tool: MyCompany.MyCli    # installs via dotnet tool install -g
```

### Generating docs from a .NET project on disk (dotnet mode)

When you want docs generated from the **current source** (no published NuGet
tool, no pre-built binary), point the action at a `.csproj`. The action then:

1. Detects the project's `<TargetFramework>` and installs the matching .NET SDK
   (skipping versions already on the runner).
2. Adds a `<PackageReference Include="InSpectra.Cli" />` for you so you don't
   have to maintain the dependency by hand. Skipped if already referenced.
3. Runs `dotnet run --project <csproj> -- cli opencli` to extract the spec
   and renders it.

```yaml
steps:
  - uses: actions/checkout@v4

  - uses: JKamsker/InSpectra@v1
    with:
      mode: dotnet
      project: src/MyCli              # path to .csproj or directory
      configuration: Release
      no-build: 'false'               # set true if you build in a previous step
      format: markdown                # html / markdown / markdown-monolith / markdown-hybrid
      output-dir: docs/cli
```

`actions/setup-dotnet` and `dotnet tool install` are no longer needed in the
caller workflow — the action handles both, generates `opencli.json`, then renders it.
The action does not revert an auto-added `InSpectra.Cli` reference during the
job, so later commit or diff steps should scope themselves to the docs output
or set `skip-inspectra-cli: 'true'` if the project already manages that dependency.

### File mode (from saved spec)

```yaml
steps:
  - uses: actions/checkout@v4

  - uses: JKamsker/InSpectra@v1
    with:
      mode: file
      format: html
      opencli-json: docs/opencli.json
      xmldoc: docs/xmldoc.xml
```

### Markdown output

```yaml
- uses: JKamsker/InSpectra@v1
  with:
    cli-name: mycli
    format: markdown            # tree layout (one file per command)

- uses: JKamsker/InSpectra@v1
  with:
    cli-name: mycli
    format: markdown-monolith   # single file

- uses: JKamsker/InSpectra@v1
  with:
    cli-name: mycli
    format: markdown-hybrid     # README.md + group files when groups exist
    split-depth: '2'
```

### Action inputs

| Input | Default | Description |
| --- | --- | --- |
| `mode` | `exec` | `exec`, `file`, `dotnet`, or `package` |
| `format` | `html` | `html`, `markdown` (tree), `markdown-monolith` (single file), or `markdown-hybrid` (README + group files as needed; leaf-only CLIs may emit README only) |
| `split-depth` | | Depth for `markdown-hybrid` output. Group files are emitted only when groups exist; leaf-only CLIs may emit README only. Ignored for other formats. |
| `cli-name` | | CLI executable name or path (exec mode) |
| `dotnet-tool` | | NuGet package to `dotnet tool install -g` (exec mode) |
| `dotnet-tool-version` | | Version constraint for the dotnet tool |
| `opencli-json` | | Path to opencli.json (file mode) |
| `xmldoc` | | Path to xmldoc.xml (file mode) |
| `project` | | Path to a `.csproj` / `.fsproj` / `.vbproj` (or directory containing one) for dotnet mode |
| `package-id` | | NuGet package id for package mode |
| `package-version` | | NuGet package version for package mode |
| `configuration` | | Build configuration for the dotnet acquisition step (e.g. `Release`) |
| `framework` | | Target framework for the dotnet acquisition step (e.g. `net10.0`) |
| `launch-profile` | | Launch profile for the dotnet acquisition step |
| `no-build` | `false` | Pass `--no-build` to the dotnet acquisition step (use after a separate build step) |
| `no-restore` | `false` | Pass `--no-restore` to the dotnet acquisition step |
| `output-dir` | `inspectra-output` | Directory where rendered output is written |
| `label` | | Custom label shown in the viewer header (e.g. `v1.2.3`) |
| `title` | | Override the CLI title shown in the viewer header and overview |
| `command-prefix` | | Override the CLI command prefix used in generated examples and the composer |
| `single-file` | `false` | Force a single self-contained HTML file. HTML is also self-contained by default at `compression-level: 2` |
| `compression-level` | `2` | HTML bundle compression level (`0`, `1`, or `2`); `2` is the default self-extracting single-file bundle |
| `theme` | | Initial HTML theme (`light` or `dark`) |
| `color-theme` | | HTML accent preset (`cyan`, `indigo`, `emerald`, `amber`, `rose`, `blue`) |
| `accent` | | Custom HTML accent color for light mode |
| `accent-dark` | | Custom HTML accent color for dark mode (falls back to `accent`) |
| `no-theme-picker` | `false` | Hide the HTML color theme picker in the toolbar |
| `extra-args` | | Additional flags forwarded to the `inspectra` CLI |
| `inspectra-version` | latest | InSpectra.Gen NuGet tool version |
| `inspectra-cli-package` | `InSpectra.Cli` | NuGet package id auto-added to the target project in dotnet mode |
| `inspectra-cli-version` | latest | Version pin for the auto-added InSpectra.Cli package |
| `skip-inspectra-cli` | `false` | Skip the automatic InSpectra.Cli PackageReference (e.g. when the project already manages it) |
| `dotnet-version` | `10.0.x` | .NET SDK version(s) for InSpectra. In dotnet mode the action also auto-detects the project's `TargetFramework` and installs that SDK; already-installed versions are skipped |
| `dotnet-quality` | stable | .NET SDK quality channel (`preview` for pre-release) |
| `opencli-args` | | Override the OpenCLI export arguments used during `generate` (`exec` / `dotnet` / `package`) |
| `xmldoc-args` | | Override the xmldoc export arguments used during `generate` (`exec` / `dotnet` / `package`) |
| `timeout` | | Timeout in seconds for each generate-mode export command (`30` default for `exec`; `120` for `dotnet` and `package`) |
| `opencli-mode` | | `native`, `auto`, `help`, `clifx`, `static`, or `hook` |
| `command` | | Override the generated root command name |
| `cli-framework` | | Hint or override the detected CLI framework for non-native analysis |

With the default `compression-level: 2`, HTML output is self-contained even if
`single-file` is left at its default `false`.

### Action output

| Output | Description |
| --- | --- |
| `output-dir` | Path to the rendered output directory |

HTML viewer flags such as `--show-home`, `--enable-url`, `--enable-nuget-browser`,
`--enable-package-upload`, `--no-composer`, `--no-dark`, and `--no-light` are
still available through `extra-args` rather than dedicated action inputs.

### End-to-end example: deploy to GitHub Pages

```yaml
name: Deploy CLI docs

on:
  push:
    branches: [main]

permissions:
  contents: write

jobs:
  docs:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: JKamsker/InSpectra@v1
        with:
          mode: dotnet
          project: src/MyCli
          configuration: Release
          output-dir: _site

      - uses: peaceiris/actions-gh-pages@v4
        with:
          github_token: ${{ secrets.GITHUB_TOKEN }}
          publish_dir: ./_site
```

### End-to-end example: open a PR with regenerated Markdown

The pattern for "always-fresh CLI reference checked into the repo" is the
combination of `mode: dotnet` and [`peter-evans/create-pull-request`]:

```yaml
name: Update CLI Docs

on:
  push:
    branches: [main]
    paths: ['src/MyCli/**']
  workflow_dispatch:

permissions:
  contents: write
  pull-requests: write

jobs:
  regenerate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: JKamsker/InSpectra@v1
        with:
          mode: dotnet
          project: src/MyCli
          format: markdown
          output-dir: docs/cli

      - uses: peter-evans/create-pull-request@v6
        with:
          branch: chore/update-cli-docs
          title: 'docs: regenerate CLI reference'
          add-paths: docs/cli
```

[`peter-evans/create-pull-request`]: https://github.com/peter-evans/create-pull-request

## Reusable Workflow

For convenience, a reusable workflow wraps the action with checkout, tool install, and artifact upload:

```yaml
jobs:
  docs:
    uses: JKamsker/InSpectra/.github/workflows/inspectra-generate.yml@v1
    with:
      cli-name: mycli
      dotnet-tool: MyCompany.MyCli
      output-dir: docs/cli
      title: My CLI
      command-prefix: mycli
```

The workflow accepts the same inputs as the action, plus:

| Input | Default | Description |
| --- | --- | --- |
| `setup-command` | | Custom shell command to make the CLI available on PATH |
| `artifact-name` | `inspectra-docs` | Name of the uploaded artifact |

## CLI Reference

```text
inspectra generate [exec|dotnet|package] [OPTIONS]
inspectra render file [markdown|html] [OPTIONS]
```

### Generate

```bash
generate exec <SOURCE> [OPTIONS]
generate dotnet <PROJECT> [OPTIONS]
generate package <PACKAGE_ID> --version <VERSION> [OPTIONS]
```

Generate supports:

- `--out <FILE>` to write `opencli.json` instead of stdout
- `--opencli-mode native|auto|help|clifx|static|hook`
- `--with-xmldoc` to enrich the generated OpenCLI document before it is written
- `--xmldoc-arg <ARG>` to override the XML documentation export command
- `--crawl-out <PATH>` when the selected acquisition mode produces crawl data

### Render Markdown

```bash
render file markdown <OPENCLI_JSON> [OPTIONS]
```

Markdown rendering supports:

- `--out <FILE>` for single-file output
- `--out-dir <DIR>` with `--layout tree` or `--layout hybrid`
- `--layout single|tree|hybrid`
- `--split-depth <N>` with `--layout hybrid` (defaults to `1`) — controls the depth at which per-group Markdown files are emitted. Depth `1` produces `README.md` plus one file per top-level group when groups exist; depth `2` also emits a file per second-level group; leaf-only CLIs may legitimately emit `README.md` only.

### Render HTML

```bash
render file html <OPENCLI_JSON> --out-dir <DIR> [OPTIONS]
```

### Dotnet generate mode

```bash
generate dotnet <PROJECT> [OPTIONS]
```

Resolves `<PROJECT>` to a `.csproj` / `.fsproj` / `.vbproj` (a directory with
exactly one is also accepted) and uses the current project output to generate
`opencli.json`. Supports these dotnet-specific build flags:

| Option | Description |
| --- | --- |
| `-c`, `--configuration <CONFIG>` | Build configuration (e.g. `Release`) |
| `-f`, `--framework <TFM>` | Target framework moniker (e.g. `net10.0`) |
| `--launch-profile <NAME>` | Launch profile to use |
| `--no-build` | Pass `--no-build` after a separate `dotnet build` |
| `--no-restore` | Pass `--no-restore` to the dotnet acquisition step |
| `--with-xmldoc` | Also invoke `cli xmldoc` so the generated `opencli.json` is enriched |
| `--timeout <SECONDS>` | Per-invocation timeout (default `120`) |

HTML uses bundle-directory output only:

- `--out-dir <DIR>` is required
- `--out` is rejected
- `--layout` is rejected
- machine-readable JSON reports `layout: "app"`

### Common Options

| Option | Description |
| --- | --- |
| `--xmldoc <PATH>` | XML enrichment file for `render file ...` |
| `--with-xmldoc` | Enrich generated `opencli.json` during `generate ...` |
| `--source-arg <ARG>` | Argument passed to the source CLI during `generate exec ...` |
| `--opencli-arg <ARG>` | Override the OpenCLI export invocation during `generate ...` |
| `--xmldoc-arg <ARG>` | Override the xmldoc invocation during `generate ...` |
| `--include-hidden` | Include hidden commands and options |
| `--include-metadata` | Include metadata sections in rendered output |
| `--overwrite` | Overwrite existing output |
| `--dry-run` | Preview output without writing files |
| `--json` | Emit machine-readable render results |
| `--timeout <SECONDS>` | Acquisition timeout for `generate ...` |

### HTML Feature Flags

When rendering HTML bundles, the following flags control which UI features are available to the end user. These flags only apply to `render file html`.

By default, statically generated pages embed the inline command viewer and composer on the default `#/` overview route. The Home toolbar button, browse/import/package routes, and URL-driven loading must be explicitly enabled, while theme switching remains available unless you disable it with the theme flags. This keeps generated documentation pages self-contained by default while still letting you opt into the extra static entry points you want to expose.

| Flag | Default | Description |
| --- | --- | --- |
| `--show-home` | off | Show the Home button in the generated static viewer toolbar. Required for `--enable-nuget-browser` and `--enable-package-upload`. |
| `--no-composer` | off | Hide the Composer panel and its toolbar toggle. |
| `--no-dark` | off | Disable the dark theme and force light mode. Cannot be combined with `--no-light`. |
| `--no-light` | off | Disable the light theme and force dark mode. Cannot be combined with `--no-dark`. |
| `--enable-url` | off | Allow `?opencli=` or `?dir=` to load alternate inputs, with optional `?xmldoc=` enrichment. When enabled, query parameters override the embedded static bootstrap input. |
| `--enable-nuget-browser` | off | Enable the `#/browse` package browser route, package deep links such as `#/pkg/<id>`, and the Browse toolbar button in generated static HTML. Requires `--show-home`. |
| `--enable-package-upload` | off | Enable the `#/import` route and import controls in generated static HTML. Requires `--show-home`. |

**Validation rules:**

- `--no-dark` and `--no-light` cannot both be set (at least one theme must be available).
- `--enable-nuget-browser` requires `--show-home` so the generated static toolbar can expose the browse entry point.
- `--enable-package-upload` requires `--show-home` so the generated static toolbar can expose the import entry point.

**Examples:**

Minimal static page (defaults):

```bash
inspectra render file html mycli.json --out-dir ./docs
```

Full-featured hosted viewer with all interactive features:

```bash
inspectra render file html mycli.json --out-dir ./docs \
  --show-home \
  --enable-url \
  --enable-nuget-browser \
  --enable-package-upload
```

Read-only dark-mode documentation without the composer:

```bash
inspectra render file html mycli.json --out-dir ./docs \
  --no-composer \
  --no-light
```

Light-mode-only page with file upload but no NuGet browser:

```bash
inspectra render file html mycli.json --out-dir ./docs \
  --show-home \
  --enable-package-upload \
  --no-dark
```

**How it works:** Feature flags are serialized into the HTML bootstrap payload (the `<script id="inspectra-bootstrap">` JSON block). The viewer reads them at startup and conditionally renders UI elements. When running the viewer in development mode (no bootstrap), all features are enabled by default. When an older bootstrap without the `features` key is loaded, the viewer falls back to secure defaults (everything off except both themes).

## HTML Viewer (InSpectraUI)

The HTML renderer copies `src/InSpectra.UI/dist/**` and patches `index.html` with a bootstrap payload.

### Boot Modes

The bundled viewer supports three boot paths:

1. **Inline bootstrap** (default for generated pages): The full OpenCLI document is embedded in the HTML as a JSON payload. The page is self-contained and works offline.
2. **URL-driven loading**: Query parameters `?opencli=<url>` or `?dir=<url>` point the viewer at remote files, and optional `?xmldoc=<url>` adds XML enrichment to that source. `?xmldoc=<url>` alone does not activate URL loading. Only active when `--enable-url` is set (or in development mode), and when active they override the embedded static input.
3. **Manual import**: Users open `#/import` to drop or pick `opencli.json` and optional `xmldoc.xml` from disk. The route and toolbar entry are only shown when `--show-home --enable-package-upload` is set (or in development mode).

### Viewer Features

- **Command tree** with sidebar navigation, search filtering (Ctrl+F), and deep-link hash routing
- **Command palette** (Ctrl+K) for quick fuzzy search across all commands
- **Composer panel** for interactively building CLI invocations from documented options and arguments (toggleable, hideable via `--no-composer`)
- **Dark/light theme** with toggle button and localStorage persistence (lockable to one theme via `--no-dark` or `--no-light`)
- **NuGet browser** for searching and exploring indexed .NET CLI tool packages (opt-in via `--show-home --enable-nuget-browser`)
- **Overview panel** showing root-level arguments, options, and command summary
- **Recursive option inheritance** display
- **Metadata sections** (when `--include-metadata` is set)

### URL Parameters

`?dir=<url>` resolves:

- `<dir>/opencli.json`
- optional `<dir>/xmldoc.xml`

Missing inferred `xmldoc.xml` is non-fatal.

`?opencli=<url>` loads that OpenCLI JSON directly. Add `?xmldoc=<url>` alongside it when you want XML enrichment from a separate file.

## Architecture

### Data flow

```text
project|exec|package -> generate opencli.json -> render file -> Markdown|HTML
```

### HTML runtime model

- v1 uses raw `opencli.json` plus optional `xmldoc.xml` as the canonical browser input
- acquisition and rendering are now separate CLI stages
- injected HTML output defaults to inline bootstrap mode
- internal links-mode support remains available for hosted scenarios

### Bundle resolution

At runtime, HTML assets are resolved in this order:

1. packaged `InSpectra.UI/dist` beside the installed tool
2. repo-local `src/InSpectra.UI/dist`
3. repo-local `npm ci` plus `npm run build` if `dist` is missing and `npm` is available
4. otherwise a clear error telling you how to build the frontend

`dotnet pack` and `dotnet publish` do not run npm implicitly. They fail if `src/InSpectra.UI/dist/index.html` is missing.

## Project Layout

```text
src/InSpectra.Gen/                          Thin CLI shell (commands, output, shell composition)
src/InSpectra.Gen.Engine/                   Backend engine (OpenCli, modes, rendering, tooling, targets, use cases)
src/InSpectra.Gen.Core/                     Foundational cross-module primitives (Cli*Exception types)
src/InSpectra.Gen.StartupHook/              .NET startup hook for live command-tree capture
src/InSpectra.UI/                           Vite + React + TypeScript viewer app
tests/InSpectra.Gen.Tests/                  xUnit test suite + architecture policy tests for shell/integration surfaces
tests/InSpectra.Gen.Engine.Tests/           xUnit test suite for the backend engine
docs/                                       Website, architecture charter, self-generated docs
examples/                                   Example renders (Jellyfin, JDownloader)
.github/actions/render/                     GitHub Action (composite)
.github/workflows/                          CI, Pages deployment, reusable workflow
```

## Contributing

### Build from source

```bash
# Build the viewer bundle
cd src/InSpectra.UI
npm ci && npm test && npm run build
cd ../..

# Build and test the .NET tool
dotnet build InSpectra.Gen.sln --configuration Release
dotnet test InSpectra.Gen.sln --configuration Release
```

### Testing

```bash
# Frontend tests
cd src/InSpectra.UI && npm test

# Backend tests
dotnet test InSpectra.Gen.sln --configuration Release
```

Coverage includes:

- frontend bootstrap precedence and import flows
- XML enrichment and normalization behavior
- HTML output contract and bootstrap injection
- bundle resolution order
- Markdown output paths and layout handling

### CI

CI builds the frontend before running the .NET test and packaging flow. Each build produces a versioned NuGet package (`0.0.<build-number>`) uploaded as a CI artifact. GitHub Pages publishes HTML examples as bundle directories at [inspectra.kamsker.at](https://inspectra.kamsker.at).

## Examples

| Example | Source | Live |
| --- | --- | --- |
| InSpectra (self-doc) | [docs/inspectra-gen](docs/inspectra-gen/) | [View](https://inspectra.kamsker.at/examples/inspectra/) |
| Jellyfin CLI | [examples/jellyfin-cli](examples/jellyfin-cli/) | [View](https://inspectra.kamsker.at/examples/jellyfin-cli/) |
| JDownloader RemoteCli | [examples/jdownloader-remotecli](examples/jdownloader-remotecli/) | [View](https://inspectra.kamsker.at/examples/jdownloader-remotecli/) |
