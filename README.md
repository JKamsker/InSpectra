# OpenCLI Renderer

Turn an OpenCLI export into either Markdown or a relocatable HTML app bundle.

```bash
opencli-renderer render file html mycli.json --out-dir ./docs
```

OpenCLI Renderer is a .NET 10 tool that reads [OpenCLI](https://opencli.org/) JSON exports, optionally enriches them with XML metadata, and renders either:

- GitHub-friendly Markdown
- an interactive HTML viewer bundle with `index.html` plus built JS/CSS assets

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
cd src/InSpectreUI
npm ci
npm test
npm run build
cd ../..
```

### Render Markdown

```bash
dotnet run --project src/OpenCli.Renderer -- \
  render file markdown examples/jellyfin-cli/opencli.json \
  --xmldoc examples/jellyfin-cli/xmldoc.xml \
  --out jellyfin-docs.md
```

### Render HTML

```bash
dotnet run --project src/OpenCli.Renderer -- \
  render file html examples/jellyfin-cli/opencli.json \
  --xmldoc examples/jellyfin-cli/xmldoc.xml \
  --out-dir jellyfin-docs
```

Open `jellyfin-docs/index.html` in a browser. The bundle is relocatable because the viewer is built with `base: "./"`.

## Command Surface

```text
opencli-renderer render [file|exec] [markdown|html] [OPTIONS]
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

## HTML Viewer

The HTML renderer copies `src/InSpectreUI/dist/**` and patches `index.html` with a bootstrap payload.

The bundled viewer supports three boot paths:

- injected inline data from the renderer
- URL-driven loading with `?opencli=...`, `?xmldoc=...`, or `?dir=...`
- manual import by dropping or picking `opencli.json` and optional `xmldoc.xml`

Other viewer behavior:

- hash routing for deep links, so static hosts and `file`-based viewers work without rewrites
- hidden-item filtering
- metadata toggling
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

1. packaged `InSpectreUI/dist` beside the installed tool
2. repo-local `src/InSpectreUI/dist`
3. repo-local `npm ci` plus `npm run build` if `dist` is missing and `npm` is available
4. otherwise a clear error telling you how to build the frontend

`dotnet pack` and `dotnet publish` do not run npm implicitly. They fail if `src/InSpectreUI/dist/index.html` is missing.

## Project Layout

```text
src/OpenCli.Renderer/
src/InSpectreUI/
tests/OpenCli.Renderer.Tests/
docs/
examples/
```

- `src/OpenCli.Renderer/` contains the CLI, render services, and packaging logic
- `src/InSpectreUI/` is a standalone Vite + React + TypeScript app
- `tests/OpenCli.Renderer.Tests/` covers CLI contracts, render services, and bundle lookup
- `examples/` contains tracked Markdown example renders plus source snapshots

## Testing

```bash
cd src/InSpectreUI
npm test
npm run build
cd ../..

dotnet test OpenCli.Renderer.sln --configuration Release
```

Coverage includes:

- frontend bootstrap precedence and import flows
- XML enrichment and normalization behavior
- HTML output contract and bootstrap injection
- bundle resolution order
- Markdown output paths and layout handling

## CI

CI builds the frontend before running the .NET test and packaging flow. GitHub Pages publishes HTML examples as bundle directories, so each example is served from its own `index.html`.

## Examples

- [examples/jellyfin-cli](examples/jellyfin-cli/)
- [examples/jdownloader-remotecli](examples/jdownloader-remotecli/)
- [docs/opencli-renderer](docs/opencli-renderer/)

The hosted HTML examples live under the Pages site as bundle directories. The repository keeps Markdown renders and source snapshots checked in.
