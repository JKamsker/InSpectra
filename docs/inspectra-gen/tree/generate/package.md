# `generate package`

- Root: [index](../index.md)
- Parent: [generate](index.md)

Generate opencli.json by installing and analyzing a  .NET tool package.

## Arguments

| Name | Required | Arity | Accepted Values | Group | Description |
| --- | --- | --- | --- | --- | --- |
| PACKAGE_ID | Yes | 1 | — | — | NuGet package id for the .NET tool package to  analyze. |

## Options

| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| --cli-framework | — | <NAME> | No | No | Declared | — | Hint or override the detected CLI framework for  non-native analysis. | NAME · required · arity 1 |
| --command | — | <NAME> | No | No | Declared | — | Override the root command name used for generated  OpenCLI documents. | NAME · required · arity 1 |
| --crawl-out | — | <PATH> | No | No | Declared | — | Write crawl.json when the selected acquisition  mode produces crawl data. | PATH · required · arity 1 |
| --json | — | flag | No | No | Declared | — | Emit the stable machine-readable JSON envelope  instead of human output. | — |
| --opencli-arg | — | <ARG> | No | No | Declared | — | Override the arguments used to invoke the  installed tool's OpenCLI export command. | ARG · required · arity 1 |
| --opencli-mode | — | <MODE> | No | No | Declared | — | OpenCLI acquisition mode: native, auto, help,  clifx, static, or hook. | MODE · required · arity 1 |
| --out | — | <FILE> | No | No | Declared | — | Write the generated OpenCLI JSON to this file  instead of stdout. | FILE · required · arity 1 |
| --overwrite | — | flag | No | No | Declared | — | Allow an existing OpenCLI output file to be  replaced. | — |
| --output | — | <MODE> | No | No | Declared | — | Override the output mode. Supported values are  human and json. | MODE · required · arity 1 |
| --timeout | — | <SECONDS> | No | No | Declared | — | Timeout in seconds for package install and command execution. | SECONDS · required · arity 1 |
| --verbose | — | flag | No | No | Declared | — | Increase diagnostic detail in command failures. | — |
| --version | — | <VERSION> | No | No | Declared | — | Package version to install and analyze. | VERSION · required · arity 1 |
| --with-xmldoc | — | flag | No | No | Declared | — | Also run the source CLI's XML documentation export command and enrich the generated OpenCLI document with its output. | — |
| --xmldoc-arg | — | <ARG> | No | No | Declared | — | Override the arguments used to invoke the source  CLI's XML documentation export command. | ARG · required · arity 1 |
