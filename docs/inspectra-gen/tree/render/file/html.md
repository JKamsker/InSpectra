# `render file html`

- Root: [index](../../index.md)
- Parent: [render file](index.md)

Render an HTML app bundle from an OpenCLI JSON  file and optional XML enrichment file.

## Arguments

| Name | Required | Arity | Accepted Values | Group | Description |
| --- | --- | --- | --- | --- | --- |
| OPENCLI_JSON | Yes | 1 | — | — | Path to the OpenCLI JSON export file to  render. |

## Options

| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| --accent | — | <COLOR> | No | No | Declared | — | Custom accent color for light mode (hex, e.g.  "#7c3aed"). | COLOR · required · arity 1 |
| --accent-dark | — | <COLOR> | No | No | Declared | — | Custom accent color for dark mode (hex). Falls back to --accent if omitted. | COLOR · required · arity 1 |
| --color-theme | — | <NAME> | No | No | Declared | — | Set the color theme (cyan, indigo, emerald,  amber, rose, blue). | NAME · required · arity 1 |
| --command-prefix | — | <TEXT> | No | No | Declared | — | Override the CLI command prefix used in  generated examples and the composer. | TEXT · required · arity 1 |
| --compression-level | — | <LEVEL> | No | No | Declared | — | Compression level: 0 = none, 1 = compress embedded JSON in multi-file bundle mode, 2 = self-extracting single-file bundle (default). | LEVEL · required · arity 1 |
| --dry-run | — | flag | No | No | Declared | — | Preview the resolved render plan without  writing files. | — |
| --enable-nuget-browser | — | flag | No | No | Declared | — | Enable the `#/browse` package browser route, package deep links such as `#/pkg/<id>`, and the Browse toolbar button in generated static HTML. Requires `--show-home`. | — |
| --enable-package-upload | — | flag | No | No | Declared | — | Enable the `#/import` route and import controls in generated static HTML. Requires `--show-home`. | — |
| --enable-url | — | flag | No | No | Declared | — | Allow `?opencli=` or `?dir=` to load alternate inputs in generated static HTML, with optional `?xmldoc=` enrichment. When enabled, query parameters override the embedded input. | — |
| --include-hidden | — | flag | No | No | Declared | — | Include commands and options marked hidden by  the source CLI. | — |
| --include-metadata | — | flag | No | No | Declared | — | Include metadata sections in the rendered  Markdown or HTML output. | — |
| --json | — | flag | No | No | Declared | — | Emit the stable machine-readable JSON envelope instead of human output. | — |
| --label | — | <TEXT> | No | No | Declared | — | Custom label shown in the viewer header (e.g.  a version string). | TEXT · required · arity 1 |
| --no-color | — | flag | No | No | Declared | — | Disable ANSI color sequences in human-readable console output. | — |
| --no-composer | — | flag | No | No | Declared | — | Hide the interactive command composer from the generated HTML app. | — |
| --no-dark | — | flag | No | No | Declared | — | Disable dark mode in the generated HTML app. | — |
| --no-light | — | flag | No | No | Declared | — | Disable light mode in the generated HTML  app. | — |
| --no-theme-picker | — | flag | No | No | Declared | — | Hide the color theme picker from the viewer  toolbar. | — |
| --out-dir | — | <DIR> | No | No | Declared | — | Directory where the HTML app bundle should be  written. | DIR · required · arity 1 |
| --output | — | <MODE> | No | No | Declared | — | Override the output mode. Supported values are human and json. | MODE · required · arity 1 |
| --overwrite | — | flag | No | No | Declared | — | Allow existing output files or directories to  be replaced. | — |
| --quiet | -q | flag | No | No | Declared | — | Suppress non-essential console output. | — |
| --show-home | — | flag | No | No | Declared | — | Show the Home button in the generated static HTML toolbar. | — |
| --single-file | — | flag | No | No | Declared | — | Emit a single self-contained HTML file with  all assets inlined. Works from file:// without a web server. | — |
| --theme | — | <MODE> | No | No | Declared | — | Set the initial theme mode (light or dark). | MODE · required · arity 1 |
| --title | — | <TEXT> | No | No | Declared | — | Override the CLI title shown in the viewer  header and overview. | TEXT · required · arity 1 |
| --verbose | — | flag | No | No | Declared | — | Increase diagnostic detail in command failures. | — |
| --xmldoc | — | <PATH> | No | No | Declared | — | Optional XML documentation file used to enrich missing descriptions. | PATH · required · arity 1 |
