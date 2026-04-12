# Inputs Reference

Every input accepted by `JKamsker/InSpectra@v1`.
`JKamsker/InSpectra/.github/workflows/inspectra-generate.yml@v1` exposes the
same action inputs, plus a small wrapper-specific set for setup and artifact upload.

## Mode and format

| Input | Default | Description |
|---|---|---|
| `mode` | `exec` | `exec` (generate `opencli.json` from a live CLI, then render it), `file` (render from saved `opencli.json`), `dotnet` (generate from a .NET project), or `package` (analyze a published .NET tool package) |
| `format` | `html` | `html` (interactive SPA), `markdown` (tree layout), `markdown-monolith` (single file), or `markdown-hybrid` (README + group files as needed; leaf-only CLIs may emit README only) |
| `split-depth` | | Depth for `markdown-hybrid` output. Depth `1` emits one file per top-level group when groups exist; depth `2` also emits second-level group files. Leaf-only CLIs may legitimately emit README only. Ignored for other formats |
| `output-dir` | `inspectra-output` | Directory where the rendered output is written |
| `label` | | Custom label shown in the viewer header (e.g. `v1.2.3`) |
| `title` | | Override the CLI title shown in the viewer header and overview |
| `command-prefix` | | Override the CLI command prefix used in generated examples and the composer |

## `exec` mode

| Input | Default | Description |
|---|---|---|
| `cli-name` | _required_ | CLI executable name or path |
| `dotnet-tool` | | NuGet package id to install via `dotnet tool install -g` before invoking |
| `dotnet-tool-version` | | Version constraint for the dotnet tool install |

## `file` mode

| Input | Default | Description |
|---|---|---|
| `opencli-json` | _required_ | Path to a saved `opencli.json` |
| `xmldoc` | | Path to a saved `xmldoc.xml` for enrichment |

## `dotnet` mode

| Input | Default | Description |
|---|---|---|
| `project` | _required_ | Path to a `.csproj` / `.fsproj` / `.vbproj` (or directory containing exactly one) |
| `configuration` | | Build configuration for the dotnet acquisition step (e.g. `Release`) |
| `framework` | | Target framework moniker (e.g. `net10.0`) |
| `launch-profile` | | Launch profile for the dotnet acquisition step |
| `no-build` | `false` | Pass `--no-build` to the dotnet acquisition step (use after a separate build step) |
| `no-restore` | `false` | Pass `--no-restore` to the dotnet acquisition step |

## `package` mode

| Input | Default | Description |
|---|---|---|
| `package-id` | _required_ | NuGet package id for the .NET tool package to analyze |
| `package-version` | _required_ | NuGet package version to install and analyze |

`package` mode enables XML enrichment by default and uses `cli xmldoc` unless
you override it with `xmldoc-args`.

### Auto-installed `InSpectra.Cli` package

In `dotnet` mode the action automatically adds an `<PackageReference>` for the
package that provides `cli opencli` / `cli xmldoc`. This auto-add only runs
when `opencli-mode` is `native` (or left empty so the CLI default remains
native).

| Input | Default | Description |
|---|---|---|
| `inspectra-cli-package` | `InSpectra.Cli` | NuGet package id to add. Override for prereleases or a private feed |
| `inspectra-cli-version` | _latest_ | Pin a specific version of the package |
| `skip-inspectra-cli` | `false` | Set to `true` if your project already manages this dependency and you want the action to leave it alone |

If the package is already referenced by the `.csproj`, the auto-add is a
no-op (your existing pin is preserved).

The action does not revert that project-file edit during the job, so the
checked-out workspace remains modified for later steps. Scope commit / diff
steps to the docs output (for example with `add-paths`) or set
`skip-inspectra-cli: 'true'` if your project already manages the dependency.

## Argument overrides (`exec` / `dotnet` / `package`)

| Input | Default | Description |
|---|---|---|
| `opencli-args` | `cli opencli` | Override the OpenCLI export arguments for generate-based modes (`exec`, `dotnet`, `package`). Useful if your CLI uses a different command (e.g. `export spec`) |
| `xmldoc-args` | `cli xmldoc` | Override the XML documentation export arguments for generate-based modes (`exec`, `dotnet`, `package`). `exec` and `dotnet` probe those args before enabling enrichment; `package` mode enables enrichment by default and uses these args directly |
| `timeout` | `30` (`exec`) / `120` (`dotnet`, `package`) | Timeout in seconds for each generate-mode export command |

## Analysis options (`exec` / `dotnet` / `package`)

| Input | Default | Description |
|---|---|---|
| `opencli-mode` | CLI default | `native`, `auto`, `help`, `clifx`, `static`, or `hook` |
| `command` | detected | Override the generated root command name |
| `cli-framework` | detected | Hint or override the CLI framework used by non-native analysis |

## .NET SDK setup

The action installs `.NET` itself — you don't need a separate
`actions/setup-dotnet` step.

| Input | Default | Description |
|---|---|---|
| `dotnet-version` | `10.0.x` | .NET SDK version(s) needed by InSpectra. In `dotnet` mode the action **also** parses your project's `<TargetFramework>` and adds the matching SDK to the install list. SDKs already on the runner are skipped |
| `dotnet-quality` | _stable_ | .NET SDK quality channel (`preview` for pre-release SDKs) |

## InSpectra.Gen tool

| Input | Default | Description |
|---|---|---|
| `inspectra-version` | _latest_ | Pin a specific `InSpectra.Gen` NuGet tool version |
| `extra-args` | | Additional flags forwarded verbatim to the `inspectra` CLI |

## HTML output options

These inputs only affect `format: html`.

| Input | Default | Description |
|---|---|---|
| `single-file` | `false` | Force a single self-contained HTML file. HTML is also self-contained by default at `compression-level: 2` |
| `compression-level` | `2` | HTML bundle compression level: `0` none, `1` compressed JSON, `2` self-extracting single-file bundle |
| `theme` | | Initial theme mode (`light` or `dark`) |
| `color-theme` | | Color theme preset (`cyan`, `indigo`, `emerald`, `amber`, `rose`, `blue`) |
| `accent` | | Custom accent color for light mode (hex) |
| `accent-dark` | | Custom accent color for dark mode (hex, falls back to `accent`) |
| `no-theme-picker` | `false` | Hide the color theme picker from the viewer toolbar |

Other HTML renderer flags such as `--show-home`, `--enable-url`,
`--enable-nuget-browser`, `--enable-package-upload`, `--no-composer`,
`--no-dark`, and `--no-light` are still supported by the CLI, but they are
passed through the action via `extra-args` rather than dedicated inputs.

With the default `compression-level: 2`, HTML output is self-contained even if
`single-file` is left at its default `false`.

## Outputs

| Output | Description |
|---|---|
| `output-dir` | Path to the rendered output directory (echoes the `output-dir` input) |

## Reusable workflow extras

When using `JKamsker/InSpectra/.github/workflows/inspectra-generate.yml@v1`,
these wrapper-specific inputs are available in addition to the full action surface:

| Input | Default | Description |
|---|---|---|
| `setup-command` | | Custom shell command to make your CLI available on PATH (alternative to `dotnet-tool`) |
| `artifact-name` | `inspectra-docs` | Name of the uploaded artifact |

---

See [Usage examples](usage.md) for snippets and [Recipes](recipes.md) for
full end-to-end pipelines.
