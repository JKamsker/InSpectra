import type { ReactNode } from "react";

export type UsageTab =
  | "from-source"
  | "dotnet-tool"
  | "from-file"
  | "markdown"
  | "build-render"
  | "release-asset";

export interface CIGuideInput {
  name: string;
  desc: ReactNode;
  defaultVal: ReactNode;
}

export const usageTabs: { id: UsageTab; label: string }[] = [
  { id: "from-source", label: "From Source" },
  { id: "dotnet-tool", label: ".NET Tool" },
  { id: "from-file", label: "From File" },
  { id: "markdown", label: "Markdown" },
  { id: "build-render", label: "Build + Render" },
  { id: "release-asset", label: "Release Asset" },
];

export const ciGuideSectionIds = ["usage", "inputs", "pages", "prerequisites"] as const;

export const ciGuideNavLinks = [
  { id: "usage", num: "01", label: "Usage" },
  { id: "inputs", num: "02", label: "Inputs" },
  { id: "pages", num: "03", label: "Pages" },
  { id: "prerequisites", num: "04", label: "Prerequisites" },
];

export const pipelineSteps = [
  { label: "Push", sub: "git push" },
  { label: "Install", sub: ".NET + tools" },
  { label: "Generate", sub: "inspectra" },
  { label: "Deploy", sub: "gh-pages" },
];

export const usageSnippets: Record<UsageTab, string> = {
  "from-source": `# .github/workflows/docs.yml — render straight from a .csproj
steps:
  - uses: actions/checkout@v6

  - uses: JKamsker/InSpectra@v1
    with:
      mode: dotnet
      project: src/MyCli              # .csproj path or directory
      configuration: Release
      format: html                    # html / markdown / markdown-monolith / markdown-hybrid
      output-dir: docs/cli

  - uses: actions/upload-artifact@v4
    with:
      name: cli-docs
      path: docs/cli`,

  "dotnet-tool": `# .github/workflows/docs.yml
steps:
  - uses: actions/checkout@v6

  - uses: JKamsker/InSpectra@v1
    with:
      dotnet-tool: MyCli.Tool        # installs the CLI for you
      cli-name: mycli
      output-dir: docs/cli

  - uses: actions/upload-artifact@v4
    with:
      name: cli-docs
      path: docs/cli`,

  "from-file": `# Render from pre-exported opencli.json
steps:
  - uses: actions/checkout@v6

  - uses: JKamsker/InSpectra@v1
    with:
      mode: file
      opencli-json: docs/opencli.json
      xmldoc: docs/xmldoc.xml         # optional
      output-dir: docs/cli`,

  markdown: `# Generate Markdown instead of HTML
steps:
  - uses: actions/checkout@v6

  - uses: JKamsker/InSpectra@v1
    with:
      dotnet-tool: MyCli.Tool
      cli-name: mycli
      format: markdown               # or markdown-monolith
      output-dir: docs/cli`,

  "build-render": `# Build your CLI from source, then render
steps:
  - uses: actions/checkout@v6

  - uses: actions/setup-dotnet@v5
    with:
      dotnet-version: 10.0.x

  - run: dotnet build src/MyCli --configuration Release

  - run: dotnet publish src/MyCli -o ./publish --no-build -c Release

  - uses: JKamsker/InSpectra@v1
    with:
      cli-name: ./publish/mycli       # path to built binary
      output-dir: _site`,

  "release-asset": `# Attach docs to a GitHub Release
steps:
  - uses: actions/checkout@v6

  - uses: JKamsker/InSpectra@v1
    with:
      dotnet-tool: MyCli.Tool
      cli-name: mycli
      output-dir: cli-docs

  - run: zip -r cli-docs.zip cli-docs/

  - uses: softprops/action-gh-release@v2
    with:
      files: cli-docs.zip`,
};

export const pagesSnippet = `name: Deploy CLI Docs

on:
  push:
    branches: [main]

permissions:
  contents: read
  pages: write
  id-token: write

jobs:
  generate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v6
      - uses: JKamsker/InSpectra@v1
        with:
          dotnet-tool: MyCli.Tool
          cli-name: mycli
          output-dir: _site
      - uses: actions/upload-pages-artifact@v4
        with: { path: _site }

  deploy:
    needs: generate
    runs-on: ubuntu-latest
    environment:
      name: github-pages
      url: \${{ steps.deploy.outputs.page_url }}
    steps:
      - uses: actions/deploy-pages@v4
        id: deploy`;

export const prerequisitesSnippet = `# If your CLI uses different export commands
- uses: JKamsker/InSpectra@v1
  with:
    cli-name: mycli
    opencli-args: 'export spec'       # instead of 'cli opencli'
    xmldoc-args: 'export xmldoc'      # instead of 'cli xmldoc'
    output-dir: docs`;

export const ciGuideInputs: CIGuideInput[] = [
  {
    name: "mode",
    desc: <>Render mode: <code>exec</code> invokes a live CLI, <code>file</code> reads from saved JSON, <code>dotnet</code> runs a .NET project from source, and <code>package</code> analyzes a published .NET tool package.</>,
    defaultVal: <><code>exec</code></>,
  },
  {
    name: "format",
    desc: <>Output format: <code>html</code> (interactive SPA), <code>markdown</code> (tree), <code>markdown-monolith</code> (single file), or <code>markdown-hybrid</code> (README plus group files as needed).</>,
    defaultVal: <><code>html</code></>,
  },
  {
    name: "cli-name",
    desc: <>CLI executable name or path. Required for exec mode.</>,
    defaultVal: <>Required (exec)</>,
  },
  {
    name: "dotnet-tool",
    desc: <>NuGet package to <code>dotnet tool install -g</code>. Requires <code>cli-name</code>.</>,
    defaultVal: <>Optional</>,
  },
  {
    name: "dotnet-tool-version",
    desc: <>Version constraint for the dotnet tool install.</>,
    defaultVal: <>Latest</>,
  },
  {
    name: "opencli-json",
    desc: <>Path to your <code>opencli.json</code> file. Required for file mode.</>,
    defaultVal: <>Required (file)</>,
  },
  {
    name: "xmldoc",
    desc: <>Path to <code>xmldoc.xml</code> for enrichment. File mode only.</>,
    defaultVal: <>Optional</>,
  },
  {
    name: "project",
    desc: <>Path to a <code>.csproj</code> / <code>.fsproj</code> / <code>.vbproj</code> (or directory containing one). Required for dotnet mode.</>,
    defaultVal: <>Required (dotnet)</>,
  },
  {
    name: "configuration",
    desc: <>Build configuration for <code>dotnet run</code> (e.g. <code>Release</code>).</>,
    defaultVal: <>Optional (dotnet)</>,
  },
  {
    name: "framework",
    desc: <>Target framework for <code>dotnet run</code> (e.g. <code>net10.0</code>).</>,
    defaultVal: <>Optional (dotnet)</>,
  },
  {
    name: "launch-profile",
    desc: <>Launch profile for <code>dotnet run</code>.</>,
    defaultVal: <>Optional (dotnet)</>,
  },
  {
    name: "no-build",
    desc: <>Pass <code>--no-build</code> to <code>dotnet run</code>. Use after a separate build step.</>,
    defaultVal: <><code>false</code></>,
  },
  {
    name: "no-restore",
    desc: <>Pass <code>--no-restore</code> to <code>dotnet run</code>.</>,
    defaultVal: <><code>false</code></>,
  },
  {
    name: "output-dir",
    desc: <>Directory where the output is written.</>,
    defaultVal: <><code>inspectra-output</code></>,
  },
  {
    name: "opencli-args",
    desc: <>Override the OpenCLI export arguments (exec / dotnet / package mode).</>,
    defaultVal: <><code>cli opencli</code></>,
  },
  {
    name: "xmldoc-args",
    desc: <>Override the xmldoc export arguments (exec / dotnet / package mode).</>,
    defaultVal: <><code>cli xmldoc</code></>,
  },
  {
    name: "timeout",
    desc: <>Timeout in seconds for each CLI export command (exec / dotnet / package mode).</>,
    defaultVal: <><code>30</code> (exec) / <code>120</code> (dotnet, package)</>,
  },
  {
    name: "extra-args",
    desc: <>Additional flags forwarded to the <code>inspectra</code> CLI.</>,
    defaultVal: <>Optional</>,
  },
  {
    name: "inspectra-version",
    desc: <>Pin a specific InSpectra.Gen NuGet version.</>,
    defaultVal: <>Latest</>,
  },
  {
    name: "inspectra-cli-package",
    desc: <>NuGet package id auto-added to the target project in dotnet mode (provides <code>cli opencli</code> / <code>cli xmldoc</code>).</>,
    defaultVal: <><code>InSpectra.Cli</code></>,
  },
  {
    name: "inspectra-cli-version",
    desc: <>Version constraint for the auto-added <code>InSpectra.Cli</code> package.</>,
    defaultVal: <>Latest</>,
  },
  {
    name: "skip-inspectra-cli",
    desc: <>Skip the automatic <code>InSpectra.Cli</code> <code>PackageReference</code> when the project already manages it.</>,
    defaultVal: <><code>false</code></>,
  },
  {
    name: "dotnet-version",
    desc: <>.NET SDK version(s) for InSpectra. In dotnet mode the action also auto-detects the project's <code>TargetFramework</code>; already-installed versions are skipped.</>,
    defaultVal: <><code>10.0.x</code></>,
  },
  {
    name: "dotnet-quality",
    desc: <>.NET SDK quality channel (<code>preview</code> for pre-release SDKs).</>,
    defaultVal: <>Optional</>,
  },
];
