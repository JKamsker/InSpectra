# `render exec html`

- Root: [index](../../index.md)
- Parent: [render exec](index.md)

Render an HTML app bundle from a live CLI process and optional `cli xmldoc` enrichment.

HTML output is bundle-directory-only. Use `--out-dir`; `--out` and `--layout` are rejected.

## Arguments

| Name | Required | Arity | Accepted Values | Group | Description |
| --- | --- | --- | --- | --- | --- |
| SOURCE | Yes | 1 | — | — | — |

## Options

| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| --cwd | — | <PATH> | No | No | Declared | — | — | PATH · required · arity 1 |
| --dry-run | — | flag | No | No | Declared | — | — | — |
| --include-hidden | — | flag | No | No | Declared | — | — | — |
| --include-metadata | — | flag | No | No | Declared | — | — | — |
| --json | — | flag | No | No | Declared | — | — | — |
| --no-color | — | flag | No | No | Declared | — | — | — |
| --opencli-arg | — | <ARG> | No | No | Declared | — | — | ARG · required · arity 1 |
| --out-dir | — | <DIR> | No | No | Declared | — | — | DIR · required · arity 1 |
| --output | — | <MODE> | No | No | Declared | — | — | MODE · required · arity 1 |
| --overwrite | — | flag | No | No | Declared | — | — | — |
| --quiet | -q | flag | No | No | Declared | — | — | — |
| --source-arg | — | <ARG> | No | No | Declared | — | — | ARG · required · arity 1 |
| --timeout | — | <SECONDS> | No | No | Declared | — | — | SECONDS · required · arity 1 |
| --verbose | — | flag | No | No | Declared | — | — | — |
| --with-xmldoc | — | flag | No | No | Declared | — | — | — |
| --xmldoc-arg | — | <ARG> | No | No | Declared | — | — | ARG · required · arity 1 |
