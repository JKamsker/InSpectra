# `generate exec`

- Root: [index](../index.md)
- Parent: [generate](index.md)

Generate opencli.json from a local executable or  script.

## Arguments

| Name | Required | Arity | Accepted Values | Group | Description |
| --- | --- | --- | --- | --- | --- |
| SOURCE | Yes | 1 | — | — | CLI executable or script to invoke. |

## Options

| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| --cli-framework | — | <NAME> | No | No | Declared | — | Hint or override the detected CLI framework for  non-native analysis. | NAME · required · arity 1 |
| --command | — | <NAME> | No | No | Declared | — | Override the root command name used for generated  OpenCLI documents. | NAME · required · arity 1 |
| --crawl-out | — | <PATH> | No | No | Declared | — | Write crawl.json when the selected acquisition  mode produces crawl data. | PATH · required · arity 1 |
| --cwd | — | <PATH> | No | No | Declared | — | Working directory to use when invoking the source  CLI. | PATH · required · arity 1 |
| --json | — | flag | No | No | Declared | — | Emit the stable machine-readable JSON envelope  instead of human output. | — |
| --opencli-arg | — | <ARG> | No | No | Declared | — | Override the arguments used to invoke the source  CLI's OpenCLI export command. | ARG · required · arity 1 |
| --opencli-mode | — | <MODE> | No | No | Declared | — | OpenCLI acquisition mode: native, auto, help,  clifx, static, or hook. | MODE · required · arity 1 |
| --out | — | <FILE> | No | No | Declared | — | Write the generated OpenCLI JSON to this file  instead of stdout. | FILE · required · arity 1 |
| --overwrite | — | flag | No | No | Declared | — | Allow an existing OpenCLI output file to be  replaced. | — |
| --output | — | <MODE> | No | No | Declared | — | Override the output mode. Supported values are  human and json. | MODE · required · arity 1 |
| --source-arg | — | <ARG> | No | No | Declared | — | Additional arguments passed directly to the source executable before the export command. | ARG · required · arity 1 |
| --timeout | — | <SECONDS> | No | No | Declared | — | Timeout in seconds for source execution. | SECONDS · required · arity 1 |
| --verbose | — | flag | No | No | Declared | — | Increase diagnostic detail in command failures. | — |
| --with-xmldoc | — | flag | No | No | Declared | — | Also run the source CLI's XML documentation export command and enrich the generated OpenCLI document with its output. | — |
| --xmldoc-arg | — | <ARG> | No | No | Declared | — | Override the arguments used to invoke the source  CLI's XML documentation export command. | ARG · required · arity 1 |
