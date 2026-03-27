# `render file markdown`

- Root: [index](../../index.md)
- Parent: [render file](index.md)

Render Markdown from an OpenCLI JSON file and  optional XML enrichment file.

## Arguments

| Name | Required | Arity | Accepted Values | Group | Description |
| --- | --- | --- | --- | --- | --- |
| OPENCLI_JSON | Yes | 1 | — | — | Path to the OpenCLI JSON export file to  render. |

## Options

| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| --dry-run | — | flag | No | No | Declared | — | Preview the resolved render plan without  writing files. | — |
| --include-hidden | — | flag | No | No | Declared | — | Include commands and options marked hidden by  the source CLI. | — |
| --include-metadata | — | flag | No | No | Declared | — | Include metadata sections in the rendered  Markdown or HTML output. | — |
| --json | — | flag | No | No | Declared | — | Emit the stable machine-readable JSON envelope instead of human output. | — |
| --layout | — | <LAYOUT> | No | No | Declared | — | Markdown layout mode. Supported values are  single and tree. | LAYOUT · required · arity 1 |
| --no-color | — | flag | No | No | Declared | — | Disable ANSI color sequences in human-readable console output. | — |
| --out | — | <FILE> | No | No | Declared | — | Single Markdown file to write when using the  single layout. | FILE · required · arity 1 |
| --out-dir | — | <DIR> | No | No | Declared | — | Output directory to write when using the tree  layout. | DIR · required · arity 1 |
| --output | — | <MODE> | No | No | Declared | — | Override the output mode. Supported values are human and json. | MODE · required · arity 1 |
| --overwrite | — | flag | No | No | Declared | — | Allow existing output files or directories to  be replaced. | — |
| --quiet | -q | flag | No | No | Declared | — | Suppress non-essential console output. | — |
| --verbose | — | flag | No | No | Declared | — | Increase diagnostic detail in the rendered  summary output. | — |
| --xmldoc | — | <PATH> | No | No | Declared | — | Optional XML documentation file used to enrich missing descriptions. | PATH · required · arity 1 |
