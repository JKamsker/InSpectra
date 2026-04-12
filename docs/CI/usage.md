# Usage Examples

Every common shape of "render docs in CI" using `JKamsker/InSpectra@v1`.
Pick the one that matches your situation, copy it into a file under
`.github/workflows/`, and adjust the inputs.

- [From source (`dotnet` mode)](#from-source-dotnet-mode) — recommended when the CLI source is in this repo
- [From a published .NET tool (`exec` mode)](#from-a-published-net-tool-exec-mode)
- [From a saved `opencli.json` (`file` mode)](#from-a-saved-openclijson-file-mode)
- [Markdown output instead of HTML](#markdown-output-instead-of-html)
- [Build then render (legacy `exec` against a built binary)](#build-then-render-legacy-exec-against-a-built-binary)
- [Attach to a GitHub Release](#attach-to-a-github-release)

> Every snippet below is a partial step list. Wrap them in a `jobs.<id>.steps:`
> block, give the job a `runs-on:`, and set `permissions:` if needed.

## From source (`dotnet` mode)

The action checks out your repo, parses your `.csproj` to install the right
.NET SDK, adds `InSpectra.Cli` for you, generates an enriched `opencli.json`,
then renders from that saved spec.

```yaml
# .github/workflows/docs.yml — render straight from a .csproj
steps:
  - uses: actions/checkout@v4

  - uses: JKamsker/InSpectra@v1
    with:
      mode: dotnet
      project: src/MyCli           # .csproj path or directory
      configuration: Release
      format: html                 # html / markdown / markdown-monolith / markdown-hybrid
      output-dir: docs/cli

  - uses: actions/upload-artifact@v4
    with:
      name: cli-docs
      path: docs/cli
```

**Tips**

- `project` accepts a `.csproj` / `.fsproj` / `.vbproj` path **or** a
  directory containing exactly one project file.
- For faster CI, build the project in a previous step and pass `no-build: 'true'`
  so the action doesn't rebuild for each `dotnet run` invocation.
- If your project already references a specific `InSpectra.Cli` version, the
  action skips the auto-add. To opt out entirely, set `skip-inspectra-cli: 'true'`.
- The auto-added `InSpectra.Cli` reference stays in the checked-out workspace
  for the rest of the job, so keep later commit or diff steps scoped to the
  docs output unless you want that project-file change included.

## From a published .NET tool (`exec` mode)

The action installs your CLI from NuGet first, then invokes it directly.
Useful when the docs lag the source by a release cycle and you want CI to
mirror what users actually have.

```yaml
steps:
  - uses: actions/checkout@v4

  - uses: JKamsker/InSpectra@v1
    with:
      dotnet-tool: MyCompany.MyCli   # installs via dotnet tool install -g
      cli-name: mycli                # the executable name after install
      output-dir: docs/cli
```

## From a saved `opencli.json` (`file` mode)

Useful when:

- the CLI doesn't run on a Linux runner (Windows-only tool, native deps),
- you want to render docs at a fixed point in time, or
- you've already exported the spec and want a pure render step.

```yaml
steps:
  - uses: actions/checkout@v4

  - uses: JKamsker/InSpectra@v1
    with:
      mode: file
      opencli-json: docs/opencli.json
      xmldoc: docs/xmldoc.xml         # optional
      output-dir: docs/cli
```

## Markdown output instead of HTML

Set `format` to `markdown` (tree layout, one file per command),
`markdown-monolith` (single file), or `markdown-hybrid`
(`README.md` plus group files when groups exist; leaf-only CLIs may emit
`README.md` only). `split-depth` only applies to
`markdown-hybrid`. All three work with any mode.

```yaml
steps:
  - uses: actions/checkout@v4

  - uses: JKamsker/InSpectra@v1
    with:
      mode: dotnet
      project: src/MyCli
      format: markdown-hybrid        # or markdown / markdown-monolith
      split-depth: '2'
      output-dir: docs/cli
```

## Build then render (legacy `exec` against a built binary)

Still useful when you need a custom build step (e.g. publishing self-contained,
signing, vendoring native deps) before the binary can run. The action still
acquires `opencli.json` first and then renders from that generated file.

```yaml
steps:
  - uses: actions/checkout@v4

  - uses: actions/setup-dotnet@v5
    with:
      dotnet-version: 10.0.x

  - run: dotnet build src/MyCli --configuration Release
  - run: dotnet publish src/MyCli -o ./publish --no-build -c Release

  - uses: JKamsker/InSpectra@v1
    with:
      cli-name: ./publish/mycli       # path to built binary
      output-dir: _site
```

## Attach to a GitHub Release

```yaml
steps:
  - uses: actions/checkout@v4

  - uses: JKamsker/InSpectra@v1
    with:
      mode: dotnet
      project: src/MyCli
      configuration: Release
      output-dir: cli-docs

  - run: zip -r cli-docs.zip cli-docs/

  - uses: softprops/action-gh-release@v2
    with:
      files: cli-docs.zip
```

---

See [Inputs reference](inputs.md) for every action input and
[Recipes](recipes.md) for full end-to-end pipelines.
