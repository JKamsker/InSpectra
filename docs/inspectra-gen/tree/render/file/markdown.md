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
| --command-prefix | — | <TEXT> | No | No | Declared | — | Override the CLI command prefix used in  rendered Markdown examples. | TEXT · required · arity 1 |
| --dry-run | — | flag | No | No | Declared | — | Preview the resolved render plan without  writing files. | — |
| --include-hidden | — | flag | No | No | Declared | — | Include commands and options marked hidden by  the source CLI. | — |
| --include-metadata | — | flag | No | No | Declared | — | Include metadata sections in the rendered  Markdown or HTML output. | — |
| --json | — | flag | No | No | Declared | — | Emit the stable machine-readable JSON envelope instead of human output. | — |
| --layout | — | <LAYOUT> | No | No | Declared | — | Markdown layout mode. Supported values are  single, tree, and hybrid. | LAYOUT · required · arity 1 |
| --no-color | — | flag | No | No | Declared | — | Disable ANSI color sequences in human-readable console output. | — |
| --out | — | <FILE> | No | No | Declared | — | Single Markdown file to write when using the  single layout. | FILE · required · arity 1 |
| --out-dir | — | <DIR> | No | No | Declared | — | Output directory to write when using the tree  or hybrid layout. | DIR · required · arity 1 |
| --output | — | <MODE> | No | No | Declared | — | Override the output mode. Supported values are human and json. | MODE · required · arity 1 |
| --overwrite | — | flag | No | No | Declared | — | Allow existing output files or directories to  be replaced. | — |
| --quiet | -q | flag | No | No | Declared | — | Suppress non-essential console output. | — |
| --split-depth | — | <DEPTH> | No | No | Declared | — | Depth at which hybrid layout emits one file  per command group (defaults to 1). | DEPTH · required · arity 1 |
| --title | — | <TEXT> | No | No | Declared | — | Override the CLI title shown in Markdown  headings and overview text. | TEXT · required · arity 1 |
| --verbose | — | flag | No | No | Declared | — | Increase diagnostic detail in command failures. | — |
| --xmldoc | — | <PATH> | No | No | Declared | — | Optional XML documentation file used to enrich missing descriptions. | PATH · required · arity 1 |
