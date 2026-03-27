# `render exec html`

- Root: [index](../../index.md)
- Parent: [render exec](index.md)

Render an HTML app bundle from a live CLI process  and optional `cli xmldoc` enrichment.

## Arguments

| Name | Required | Arity | Accepted Values | Group | Description |
| --- | --- | --- | --- | --- | --- |
| SOURCE | Yes | 1 | — | — | CLI executable or script to invoke for cli  opencli exports. |

## Options

| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| --cwd | — | <PATH> | No | No | Declared | — | Working directory to use when invoking the  source CLI. | PATH · required · arity 1 |
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
| --opencli-arg | — | <ARG> | No | No | Declared | — | Override the arguments used to invoke the  source CLI's OpenCLI export command. | ARG · required · arity 1 |
| --out-dir | — | <DIR> | No | No | Declared | — | Directory where the HTML app bundle should be  written. | DIR · required · arity 1 |
| --output | — | <MODE> | No | No | Declared | — | Override the output mode. Supported values are human and json. | MODE · required · arity 1 |
| --overwrite | — | flag | No | No | Declared | — | Allow existing output files or directories to  be replaced. | — |
| --quiet | -q | flag | No | No | Declared | — | Suppress non-essential console output. | — |
| --show-home | — | flag | No | No | Declared | — | Show the viewer home screen button in the  generated HTML app. | — |
| --source-arg | — | <ARG> | No | No | Declared | — | Additional arguments passed directly to the  source executable before the export command. | ARG · required · arity 1 |
| --timeout | — | <SECONDS> | No | No | Declared | — | Timeout in seconds for each export command  executed against the source CLI. | SECONDS · required · arity 1 |
| --verbose | — | flag | No | No | Declared | — | Increase diagnostic detail in the rendered  summary output. | — |
| --with-xmldoc | — | flag | No | No | Declared | — | Also invoke the source CLI's cli xmldoc  command for XML enrichment. | — |
| --xmldoc-arg | — | <ARG> | No | No | Declared | — | Override the arguments used to invoke the  source CLI's XML documentation export command. | ARG · required · arity 1 |
