# `generate dotnet`

- Root: [index](../index.md)
- Parent: [generate](index.md)

Generate opencli.json from a .NET project.

## Arguments

| Name | Required | Arity | Accepted Values | Group | Description |
| --- | --- | --- | --- | --- | --- |
| PROJECT | Yes | 1 | — | — | Path to a .NET project file  (.csproj/.fsproj/.vbproj) or a directory containing one. |

## Options

| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| --cli-framework | — | <NAME> | No | No | Declared | — | Hint or override the detected CLI framework for  non-native analysis. | NAME · required · arity 1 |
| --command | — | <NAME> | No | No | Declared | — | Override the root command name used for generated  OpenCLI documents. | NAME · required · arity 1 |
| --configuration | -c | <CONFIG> | No | No | Declared | — | Build configuration passed to dotnet run/build  (e.g. Release). | CONFIG · required · arity 1 |
| --crawl-out | — | <PATH> | No | No | Declared | — | Write crawl.json when the selected acquisition  mode produces crawl data. | PATH · required · arity 1 |
| --cwd | — | <PATH> | No | No | Declared | — | Working directory used when invoking dotnet. | PATH · required · arity 1 |
| --framework | -f | <TFM> | No | No | Declared | — | Target framework moniker passed to dotnet  run/build (e.g. net10.0). | TFM · required · arity 1 |
| --json | — | flag | No | No | Declared | — | Emit the stable machine-readable JSON envelope  instead of human output. | — |
| --launch-profile | — | <NAME> | No | No | Declared | — | Launch profile to use for dotnet run native  mode. | NAME · required · arity 1 |
| --no-build | — | flag | No | No | Declared | — | Skip the implicit build step for dotnet  run/build. | — |
| --no-restore | — | flag | No | No | Declared | — | Skip the implicit restore step for dotnet  run/build. | — |
| --opencli-arg | — | <ARG> | No | No | Declared | — | Override the arguments used to invoke the  project's OpenCLI export command. | ARG · required · arity 1 |
| --opencli-mode | — | <MODE> | No | No | Declared | — | OpenCLI acquisition mode: native, auto, help,  clifx, static, or hook. | MODE · required · arity 1 |
| --out | — | <FILE> | No | No | Declared | — | Write the generated OpenCLI JSON to this file  instead of stdout. | FILE · required · arity 1 |
| --overwrite | — | flag | No | No | Declared | — | Allow an existing OpenCLI output file to be  replaced. | — |
| --output | — | <MODE> | No | No | Declared | — | Override the output mode. Supported values are  human and json. | MODE · required · arity 1 |
| --timeout | — | <SECONDS> | No | No | Declared | — | Timeout in seconds for dotnet execution. | SECONDS · required · arity 1 |
| --verbose | — | flag | No | No | Declared | — | Increase diagnostic detail in command failures. | — |
| --with-xmldoc | — | flag | No | No | Declared | — | Also run the source CLI's XML documentation export command and enrich the generated OpenCLI document with its output. | — |
| --xmldoc-arg | — | <ARG> | No | No | Declared | — | Override the arguments used to invoke the source  CLI's XML documentation export command. | ARG · required · arity 1 |
