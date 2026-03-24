# `render file html`

- Root: [index](../../index.md)
- Parent: [render file](index.md)

Render an HTML app bundle from an OpenCLI JSON file and optional XML enrichment file.

HTML output is bundle-directory-only. Use `--out-dir`; `--out` and `--layout` are rejected.

## Arguments

| Name | Required | Arity | Accepted Values | Group | Description |
| --- | --- | --- | --- | --- | --- |
| OPENCLI_JSON | Yes | 1 | — | — | — |

## Options

| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| --dry-run | — | flag | No | No | Declared | — | — | — |
| --include-hidden | — | flag | No | No | Declared | — | — | — |
| --include-metadata | — | flag | No | No | Declared | — | — | — |
| --json | — | flag | No | No | Declared | — | — | — |
| --no-color | — | flag | No | No | Declared | — | — | — |
| --out-dir | — | <DIR> | No | No | Declared | — | — | DIR · required · arity 1 |
| --output | — | <MODE> | No | No | Declared | — | — | MODE · required · arity 1 |
| --overwrite | — | flag | No | No | Declared | — | — | — |
| --quiet | -q | flag | No | No | Declared | — | — | — |
| --verbose | — | flag | No | No | Declared | — | — | — |
| --xmldoc | — | <PATH> | No | No | Declared | — | — | PATH · required · arity 1 |
