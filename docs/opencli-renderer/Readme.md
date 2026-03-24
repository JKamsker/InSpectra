# opencli-renderer

- Browse the tree docs: [tree/index.md](tree/index.md)
- OpenCLI snapshot: [opencli.json](opencli.json)
- XML snapshot: [xmldoc.xml](xmldoc.xml)

- Version: `0.1.0`
- OpenCLI: `0.1-draft`

Command-line reference for `opencli-renderer`. Available command areas include render.

## Table of Contents

- [Overview](#overview)
- [Commands](#commands)
  - [render](#command-render)
    - [render exec](#command-render-exec)
      - [render exec html](#command-render-exec-html)
      - [render exec markdown](#command-render-exec-markdown)
    - [render file](#command-render-file)
      - [render file html](#command-render-file-html)
      - [render file markdown](#command-render-file-markdown)

<a id="overview"></a>
## Overview

### CLI Scope

- Top-level command groups: `1`
- Documented commands: `7`
- Leaf commands: `4`

### Available Commands

- [render](#command-render) — Render documentation from OpenCLI exports.


<a id="commands"></a>
## Commands

<a id="command-render"></a>
## `render`

Render documentation from OpenCLI exports.

### Subcommands

- `exec` — Render docs by executing a CLI that exposes `cli  opencli`.
- `file` — Render docs from saved OpenCLI export files.

<a id="command-render-exec"></a>
### `render exec`

Render docs by executing a CLI that exposes `cli  opencli`.

#### Subcommands

- `html` — Render HTML from a live CLI process and optional  `cli xmldoc` enrichment.
- `markdown` — Render Markdown from a live CLI process and  optional `cli xmldoc` enrichment.

<a id="command-render-exec-html"></a>
#### `render exec html`

Render HTML from a live CLI process and optional  `cli xmldoc` enrichment.

##### Arguments

| Name | Required | Arity | Accepted Values | Group | Description |
| --- | --- | --- | --- | --- | --- |
| SOURCE | Yes | 1 | — | — | — |

##### Options

| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| --cwd | — | <PATH> | No | No | Declared | — | — | PATH · required · arity 1 |
| --dry-run | — | flag | No | No | Declared | — | — | — |
| --include-hidden | — | flag | No | No | Declared | — | — | — |
| --include-metadata | — | flag | No | No | Declared | — | — | — |
| --json | — | flag | No | No | Declared | — | — | — |
| --layout | — | <LAYOUT> | No | No | Declared | — | — | LAYOUT · required · arity 1 |
| --no-color | — | flag | No | No | Declared | — | — | — |
| --opencli-arg | — | <ARG> | No | No | Declared | — | — | ARG · required · arity 1 |
| --out | — | <FILE> | No | No | Declared | — | — | FILE · required · arity 1 |
| --out-dir | — | <DIR> | No | No | Declared | — | — | DIR · required · arity 1 |
| --output | — | <MODE> | No | No | Declared | — | — | MODE · required · arity 1 |
| --overwrite | — | flag | No | No | Declared | — | — | — |
| --quiet | -q | flag | No | No | Declared | — | — | — |
| --source-arg | — | <ARG> | No | No | Declared | — | — | ARG · required · arity 1 |
| --timeout | — | <SECONDS> | No | No | Declared | — | — | SECONDS · required · arity 1 |
| --verbose | — | flag | No | No | Declared | — | — | — |
| --with-xmldoc | — | flag | No | No | Declared | — | — | — |
| --xmldoc-arg | — | <ARG> | No | No | Declared | — | — | ARG · required · arity 1 |

<a id="command-render-exec-markdown"></a>
#### `render exec markdown`

Render Markdown from a live CLI process and  optional `cli xmldoc` enrichment.

##### Arguments

| Name | Required | Arity | Accepted Values | Group | Description |
| --- | --- | --- | --- | --- | --- |
| SOURCE | Yes | 1 | — | — | — |

##### Options

| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| --cwd | — | <PATH> | No | No | Declared | — | — | PATH · required · arity 1 |
| --dry-run | — | flag | No | No | Declared | — | — | — |
| --include-hidden | — | flag | No | No | Declared | — | — | — |
| --include-metadata | — | flag | No | No | Declared | — | — | — |
| --json | — | flag | No | No | Declared | — | — | — |
| --layout | — | <LAYOUT> | No | No | Declared | — | — | LAYOUT · required · arity 1 |
| --no-color | — | flag | No | No | Declared | — | — | — |
| --opencli-arg | — | <ARG> | No | No | Declared | — | — | ARG · required · arity 1 |
| --out | — | <FILE> | No | No | Declared | — | — | FILE · required · arity 1 |
| --out-dir | — | <DIR> | No | No | Declared | — | — | DIR · required · arity 1 |
| --output | — | <MODE> | No | No | Declared | — | — | MODE · required · arity 1 |
| --overwrite | — | flag | No | No | Declared | — | — | — |
| --quiet | -q | flag | No | No | Declared | — | — | — |
| --source-arg | — | <ARG> | No | No | Declared | — | — | ARG · required · arity 1 |
| --timeout | — | <SECONDS> | No | No | Declared | — | — | SECONDS · required · arity 1 |
| --verbose | — | flag | No | No | Declared | — | — | — |
| --with-xmldoc | — | flag | No | No | Declared | — | — | — |
| --xmldoc-arg | — | <ARG> | No | No | Declared | — | — | ARG · required · arity 1 |

<a id="command-render-file"></a>
### `render file`

Render docs from saved OpenCLI export files.

#### Subcommands

- `html` — Render HTML from an OpenCLI JSON file and optional XML enrichment file.
- `markdown` — Render Markdown from an OpenCLI JSON file and  optional XML enrichment file.

<a id="command-render-file-html"></a>
#### `render file html`

Render HTML from an OpenCLI JSON file and optional XML enrichment file.

##### Arguments

| Name | Required | Arity | Accepted Values | Group | Description |
| --- | --- | --- | --- | --- | --- |
| OPENCLI_JSON | Yes | 1 | — | — | — |

##### Options

| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| --dry-run | — | flag | No | No | Declared | — | — | — |
| --include-hidden | — | flag | No | No | Declared | — | — | — |
| --include-metadata | — | flag | No | No | Declared | — | — | — |
| --json | — | flag | No | No | Declared | — | — | — |
| --layout | — | <LAYOUT> | No | No | Declared | — | — | LAYOUT · required · arity 1 |
| --no-color | — | flag | No | No | Declared | — | — | — |
| --out | — | <FILE> | No | No | Declared | — | — | FILE · required · arity 1 |
| --out-dir | — | <DIR> | No | No | Declared | — | — | DIR · required · arity 1 |
| --output | — | <MODE> | No | No | Declared | — | — | MODE · required · arity 1 |
| --overwrite | — | flag | No | No | Declared | — | — | — |
| --quiet | -q | flag | No | No | Declared | — | — | — |
| --verbose | — | flag | No | No | Declared | — | — | — |
| --xmldoc | — | <PATH> | No | No | Declared | — | — | PATH · required · arity 1 |

<a id="command-render-file-markdown"></a>
#### `render file markdown`

Render Markdown from an OpenCLI JSON file and  optional XML enrichment file.

##### Arguments

| Name | Required | Arity | Accepted Values | Group | Description |
| --- | --- | --- | --- | --- | --- |
| OPENCLI_JSON | Yes | 1 | — | — | — |

##### Options

| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| --dry-run | — | flag | No | No | Declared | — | — | — |
| --include-hidden | — | flag | No | No | Declared | — | — | — |
| --include-metadata | — | flag | No | No | Declared | — | — | — |
| --json | — | flag | No | No | Declared | — | — | — |
| --layout | — | <LAYOUT> | No | No | Declared | — | — | LAYOUT · required · arity 1 |
| --no-color | — | flag | No | No | Declared | — | — | — |
| --out | — | <FILE> | No | No | Declared | — | — | FILE · required · arity 1 |
| --out-dir | — | <DIR> | No | No | Declared | — | — | DIR · required · arity 1 |
| --output | — | <MODE> | No | No | Declared | — | — | MODE · required · arity 1 |
| --overwrite | — | flag | No | No | Declared | — | — | — |
| --quiet | -q | flag | No | No | Declared | — | — | — |
| --verbose | — | flag | No | No | Declared | — | — | — |
| --xmldoc | — | <PATH> | No | No | Declared | — | — | PATH · required · arity 1 |
