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

### Self-Documentation

```bash
render self --out-dir <DIR> [OPTIONS]
```

Generates InSpectra's own documentation by self-invoking `cli opencli` and `cli xmldoc`, then rendering all formats into a single output directory. This is the canonical way to regenerate `docs/inspectra-gen/`.

The output directory will contain:

- `opencli.json` — the raw OpenCLI export (clean JSON, free of Spectre.Console line-wrapping artifacts)
- `xmldoc.xml` — the raw XML documentation export
- `tree/` — Markdown tree layout
- `html/` — HTML app bundle

Additional options:

| Option | Description |
| --- | --- |
| `--skip-markdown` | Skip Markdown tree generation |
| `--skip-html` | Skip HTML bundle generation |

All HTML feature flags (`--show-home`, `--no-composer`, etc.) and common options (`--include-hidden`, `--include-metadata`, `--overwrite`) are supported.

```bash
# Regenerate docs/inspectra-gen
inspectra render self --out-dir docs/inspectra-gen --overwrite
```

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

### HTML Feature Flags

When rendering HTML bundles, the following flags control which UI features are available to the end user. These flags only apply to `render file html` and `render exec html`.

By default, statically generated pages are locked down: only the inline command viewer and composer are active. Features like the home screen, NuGet browser, file upload, URL loading, and theme switching must be explicitly opted in. This "secure by default" approach ensures generated documentation pages expose exactly the features you choose.

| Flag | Default | Description |
| --- | --- | --- |
| `--show-home` | off | Show the Home button in the toolbar. Clicking it navigates back to the import/start screen. Required for `--enable-nuget-browser` and `--enable-package-upload` since those features live on the home screen. |
| `--no-composer` | off | Hide the Composer panel and its toolbar toggle. The Composer lets users interactively build CLI invocations from the documented options and arguments. Pass this flag if the generated page should be read-only reference documentation without the command-building UI. |
| `--no-dark` | off | Disable the dark theme and remove the theme toggle from the toolbar. The viewer is forced to light mode. Cannot be combined with `--no-light`. |
| `--no-light` | off | Disable the light theme and remove the theme toggle from the toolbar. The viewer is forced to dark mode. Cannot be combined with `--no-dark`. |
| `--enable-url` | off | Allow the viewer to load an OpenCLI spec from URL query parameters (`?opencli=`, `?xmldoc=`, `?dir=`). Without this flag, query parameters are ignored and the viewer only displays the inline data baked into the page. |
| `--enable-nuget-browser` | off | Enable the NuGet package browser, which lets users search and explore indexed .NET CLI tool packages. Requires `--show-home`. |
| `--enable-package-upload` | off | Enable the file upload drop zone on the home screen, allowing users to import an `opencli.json` (and optional `xmldoc.xml`) from disk. Requires `--show-home`. |

**Validation rules:**

- `--no-dark` and `--no-light` cannot both be set (at least one theme must be available).
- `--enable-nuget-browser` requires `--show-home` (the browser is accessed from the home screen).
- `--enable-package-upload` requires `--show-home` (the upload drop zone is on the home screen).

**Examples:**

Minimal static page (defaults: no home, no URL loading, no nuget, no upload, composer on, both themes):

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
2. **URL-driven loading**: Query parameters `?opencli=<url>`, `?xmldoc=<url>`, or `?dir=<url>` point the viewer at remote files. Only active when `--enable-url` is set (or in development mode).
3. **Manual import**: Users drop or pick `opencli.json` and optional `xmldoc.xml` from disk. Only shown when `--enable-package-upload` is set (or in development mode).

### Viewer Features

- **Command tree** with sidebar navigation, search filtering (Ctrl+F), and deep-link hash routing
- **Command palette** (Ctrl+K) for quick fuzzy search across all commands
- **Composer panel** for interactively building CLI invocations from documented options and arguments (toggleable, hideable via `--no-composer`)
- **Dark/light theme** with toggle button and localStorage persistence (lockable to one theme via `--no-dark` or `--no-light`)
- **NuGet browser** for searching and exploring indexed .NET CLI tool packages (opt-in via `--enable-nuget-browser`)
- **Overview panel** showing root-level arguments, options, and command summary
- **Recursive option inheritance** display
- **Metadata sections** (when `--include-metadata` is set)

### URL Parameters

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
