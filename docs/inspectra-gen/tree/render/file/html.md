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
| --dry-run | — | flag | No | No | Declared | — | Preview the resolved render plan without  writing files. | — |
| --enable-nuget-browser | — | flag | No | No | Declared | — | Enable the NuGet package browser on the viewer home screen. | — |
| --enable-package-upload | — | flag | No | No | Declared | — | Enable local package upload on the viewer home screen. | — |
| --enable-url | — | flag | No | No | Declared | — | Allow the viewer to load OpenCLI inputs from  URL query parameters. | — |
| --include-hidden | — | flag | No | No | Declared | — | Include commands and options marked hidden by  the source CLI. | — |
| --include-metadata | — | flag | No | No | Declared | — | Include metadata sections in the rendered  Markdown or HTML output. | — |
| --json | — | flag | No | No | Declared | — | Emit the stable machine-readable JSON envelope instead of human output. | — |
| --no-color | — | flag | No | No | Declared | — | Disable ANSI color sequences in human-readable console output. | — |
| --no-composer | — | flag | No | No | Declared | — | Hide the interactive command composer from the generated HTML app. | — |
| --no-dark | — | flag | No | No | Declared | — | Disable dark mode in the generated HTML app. | — |
| --no-light | — | flag | No | No | Declared | — | Disable light mode in the generated HTML  app. | — |
| --out-dir | — | <DIR> | No | No | Declared | — | Directory where the HTML app bundle should be  written. | DIR · required · arity 1 |
| --output | — | <MODE> | No | No | Declared | — | Override the output mode. Supported values are human and json. | MODE · required · arity 1 |
| --overwrite | — | flag | No | No | Declared | — | Allow existing output files or directories to  be replaced. | — |
| --quiet | -q | flag | No | No | Declared | — | Suppress non-essential console output. | — |
| --show-home | — | flag | No | No | Declared | — | Show the viewer home screen button in the  generated HTML app. | — |
| --verbose | — | flag | No | No | Declared | — | Increase diagnostic detail in the rendered  summary output. | — |
| --xmldoc | — | <PATH> | No | No | Declared | — | Optional XML documentation file used to enrich missing descriptions. | PATH · required · arity 1 |
