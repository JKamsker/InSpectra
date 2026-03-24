# InSpectra

Turn an OpenCLI export into either Markdown or a relocatable HTML app bundle.

```bash
inspectra render file html mycli.json --out-dir ./docs
```

InSpectra is a .NET 10 tool that reads [OpenCLI](https://opencli.org/) JSON exports, optionally enriches them with XML metadata, and renders either:

- GitHub-friendly Markdown
- an interactive HTML viewer bundle with `index.html` plus built JS/CSS assets

## Install

```bash
dotnet tool install -g InSpectra.Gen
```

This installs the `inspectra` command globally.

## Why

- OpenCLI JSON stays the source of truth. Render docs directly from what the CLI exposes.
- Validation happens before rendering, so broken specs fail early.
- XML enrichment is additive. It only fills missing descriptions.
- Markdown and HTML share the same normalization and enrichment rules.

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Node.js for local frontend builds, CI, and `dotnet pack` / `dotnet publish`

### Build the viewer bundle

```bash
cd src/InSpectra.UI
npm ci
npm test
npm run build
cd ../..
```

### Render Markdown

```bash
dotnet run --project src/InSpectra.Gen -- \
  render file markdown examples/jellyfin-cli/opencli.json \
  --xmldoc examples/jellyfin-cli/xmldoc.xml \
  --out jellyfin-docs.md
```

### Render HTML

```bash
dotnet run --project src/InSpectra.Gen -- \
  render file html examples/jellyfin-cli/opencli.json \
  --xmldoc examples/jellyfin-cli/xmldoc.xml \
  --out-dir jellyfin-docs
```

Open `jellyfin-docs/index.html` in a browser. The bundle is relocatable because the viewer is built with `base: "./"`.

## Command Surface

```text
inspectra render [file|exec] [markdown|html] [OPTIONS]
```

### Markdown

```bash
render file markdown <OPENCLI_JSON> [OPTIONS]
render exec markdown <SOURCE> [OPTIONS]
```

Markdown supports:

- `--out <FILE>` for single-file output
- `--out-dir <DIR>` with `--layout tree`
- `--layout single|tree`

### HTML

```bash
render file html <OPENCLI_JSON> --out-dir <DIR> [OPTIONS]
render exec html <SOURCE> --out-dir <DIR> [OPTIONS]
```

HTML uses bundle-directory output only:

- `--out-dir <DIR>` is required
- `--out` is rejected
- `--layout` is rejected
- machine-readable JSON reports `layout: "app"`

### Common Options

| Option | Description |
| --- | --- |
| `--xmldoc <PATH>` | XML enrichment file for `render file ...` |
| `--with-xmldoc` | Also invoke `cli xmldoc` in exec mode |
| `--source-arg <ARG>` | Argument passed to the source CLI |
| `--opencli-arg <ARG>` | Override the OpenCLI export invocation |
| `--xmldoc-arg <ARG>` | Override the xmldoc invocation |
| `--include-hidden` | Include hidden commands and options |
| `--include-metadata` | Include metadata sections in rendered output |
| `--overwrite` | Overwrite existing output |
| `--dry-run` | Preview output without writing files |
| `--json` | Emit machine-readable render results |
| `--timeout <SECONDS>` | Exec-mode timeout |

## HTML Viewer (InSpectraUI)

The HTML renderer copies `src/InSpectra.UI/dist/**` and patches `index.html` with a bootstrap payload.

The bundled viewer supports three boot paths:

- injected inline data from the renderer
- URL-driven loading with `?opencli=...`, `?xmldoc=...`, or `?dir=...`
- manual import by dropping or picking `opencli.json` and optional `xmldoc.xml`

Other viewer features:

- dark mode with theme toggle and localStorage persistence
- command palette (Ctrl+K) for quick command search
- composer panel for interactively building CLI commands
- Ctrl+F to focus sidebar search
- hash routing for deep links
- hidden-item filtering and metadata toggling
- recursive option inheritance
- command-tree filtering

`?dir=<url>` resolves:

- `<dir>/opencli.json`
- optional `<dir>/xmldoc.xml`

Missing inferred `xmldoc.xml` is non-fatal.

## Architecture

### Data flow

```text
OpenCLI JSON ──┐
XML metadata ──┴─> validate -> enrich -> normalize -> render -> write
```

### HTML runtime model

- v1 uses raw `opencli.json` plus optional `xmldoc.xml` as the canonical browser input
- the .NET HTML pipeline keeps the existing acquisition and validation flow
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
src/InSpectra.Gen/       CLI tool and render services
src/InSpectra.UI/        Vite + React + TypeScript viewer app
tests/InSpectra.Gen.Tests/
docs/
examples/
```

## Testing

```bash
cd src/InSpectra.UI
npm test
npm run build
cd ../..

dotnet test InSpectra.Gen.sln --configuration Release
```

Coverage includes:

- frontend bootstrap precedence and import flows
- XML enrichment and normalization behavior
- HTML output contract and bootstrap injection
- bundle resolution order
- Markdown output paths and layout handling

## CI

CI builds the frontend before running the .NET test and packaging flow. Each build produces a versioned NuGet package (`0.0.<build-number>`) uploaded as a CI artifact. GitHub Pages publishes HTML examples as bundle directories.

## Examples

- [examples/jellyfin-cli](examples/jellyfin-cli/)
- [examples/jdownloader-remotecli](examples/jdownloader-remotecli/)
- [docs/inspectra-gen](docs/inspectra-gen/)

The hosted HTML examples live under the Pages site as bundle directories. The repository keeps Markdown renders and source snapshots checked in.
