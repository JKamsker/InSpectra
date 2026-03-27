# `render self`

- Root: [index](../index.md)
- Parent: [render](index.md)

Render documentation for InSpectra itself. Exports  opencli.json, xmldoc.xml, Markdown tree, and HTML bundle.

## Options

| Name | Aliases | Value | Required | Recursive | Scope | Group | Description | Arguments |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| --enable-nuget-browser | — | flag | No | No | Declared | — | Enable the NuGet package browser on the  self-documentation viewer home screen. | — |
| --enable-package-upload | — | flag | No | No | Declared | — | Enable local package upload on the  self-documentation viewer home screen. | — |
| --enable-url | — | flag | No | No | Declared | — | Allow the viewer to load alternate OpenCLI inputs  from URL query parameters. | — |
| --include-hidden | — | flag | No | No | Declared | — | Include hidden commands and options from  InSpectra's own CLI surface. | — |
| --include-metadata | — | flag | No | No | Declared | — | Include metadata sections in the generated  Markdown tree and HTML bundle. | — |
| --no-composer | — | flag | No | No | Declared | — | Hide the interactive command composer from the  generated self-documentation app. | — |
| --no-dark | — | flag | No | No | Declared | — | Disable dark mode in the generated  self-documentation app. | — |
| --no-light | — | flag | No | No | Declared | — | Disable light mode in the generated  self-documentation app. | — |
| --out-dir | — | <DIR> | No | No | Declared | — | Directory where the self-documentation bundle  should be written. | DIR · required · arity 1 |
| --overwrite | — | flag | No | No | Declared | — | Allow existing self-documentation output to be  replaced. | — |
| --show-home | — | flag | No | No | Declared | — | Show the viewer home screen button in the  generated self-documentation app. | — |
| --skip-html | — | flag | No | No | Declared | — | Skip generating the HTML app bundle under html/. | — |
| --skip-markdown | — | flag | No | No | Declared | — | Skip generating the Markdown tree output under  tree/. | — |
