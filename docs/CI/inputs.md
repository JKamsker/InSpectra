# Inputs Reference

Every input accepted by `JKamsker/InSpectra@v1`. The same inputs are exposed
on the reusable workflow at
`JKamsker/InSpectra/.github/workflows/inspectra-generate.yml@v1`.

## Mode and format

| Input | Default | Description |
|---|---|---|
| `mode` | `exec` | `exec` (invoke a live CLI), `file` (from saved opencli.json), or `dotnet` (run a .NET project from source) |
| `format` | `html` | `html` (interactive SPA), `markdown` (tree layout), or `markdown-monolith` (single file) |
| `output-dir` | `inspectra-output` | Directory where the rendered output is written |
| `label` | | Custom label shown in the viewer header (e.g. `v1.2.3`) |

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
| `configuration` | | Build configuration for `dotnet run` (e.g. `Release`) |
| `framework` | | Target framework moniker (e.g. `net10.0`) |
| `launch-profile` | | Launch profile for `dotnet run` |
| `no-build` | `false` | Pass `--no-build` to `dotnet run` (use after a separate build step) |
| `no-restore` | `false` | Pass `--no-restore` to `dotnet run` |

### Auto-installed `InSpectra.Cli` package

In `dotnet` mode the action automatically adds an `<PackageReference>` for the
package that provides `cli opencli` / `cli xmldoc`. The csproj is restored to
its original state by the underlying CI checkout being throwaway, so this
mutation never reaches your repo.

| Input | Default | Description |
|---|---|---|
| `inspectra-cli-package` | `InSpectra.Cli` | NuGet package id to add. Override for prereleases or a private feed |
| `inspectra-cli-version` | _latest_ | Pin a specific version of the package |
| `skip-inspectra-cli` | `false` | Set to `true` if your project already manages this dependency and you want the action to leave it alone |

If the package is already referenced by the `.csproj`, the auto-add is a
no-op (your existing pin is preserved).

## Argument overrides (exec / dotnet)

| Input | Default | Description |
|---|---|---|
| `opencli-args` | `cli opencli` | Override the OpenCLI export arguments. Useful if your CLI uses a different command (e.g. `export spec`) |
| `xmldoc-args` | `cli xmldoc` | Override the XML documentation export arguments |
| `timeout` | `30` (`exec`) / `120` (`dotnet`) | Per-invocation timeout in seconds |

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

## Outputs

| Output | Description |
|---|---|
| `output-dir` | Path to the rendered output directory (echoes the `output-dir` input) |

## Reusable workflow extras

When using `JKamsker/InSpectra/.github/workflows/inspectra-generate.yml@v1`,
two additional inputs are available:

| Input | Default | Description |
|---|---|---|
| `setup-command` | | Custom shell command to make your CLI available on PATH (alternative to `dotnet-tool`) |
| `artifact-name` | `inspectra-docs` | Name of the uploaded artifact |

---

See [Usage examples](usage.md) for snippets and [Recipes](recipes.md) for
full end-to-end pipelines.
