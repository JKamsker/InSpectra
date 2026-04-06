import { useState, useCallback } from "react";
import { Copy, Check, Info, AlertCircle } from "lucide-react";

type UsageTab = "dotnet-tool" | "from-file" | "markdown" | "build-render" | "release-asset";

function CopyButton({ text }: { text: string }) {
  const [copied, setCopied] = useState(false);

  const handleCopy = useCallback(() => {
    navigator.clipboard.writeText(text).then(() => {
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    });
  }, [text]);

  return (
    <button
      type="button"
      className={`ci-guide-copy-btn${copied ? " copied" : ""}`}
      onClick={handleCopy}
      aria-label={copied ? "Copied" : "Copy to clipboard"}
    >
      {copied ? <Check size={13} aria-hidden="true" /> : <Copy size={13} aria-hidden="true" />}
      <span>{copied ? "Copied" : "Copy"}</span>
    </button>
  );
}

/* ── Plain-text versions for copying ── */

const usageSnippets: Record<UsageTab, string> = {
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

const pagesSnippet = `name: Deploy CLI Docs

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
      - uses: actions/upload-pages-artifact@v3
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

const prerequisitesSnippet = `# If your CLI uses different export commands
- uses: JKamsker/InSpectra@v1
  with:
    cli-name: mycli
    opencli-args: 'export spec'       # instead of 'cli opencli'
    xmldoc-args: 'export xmldoc'      # instead of 'cli xmldoc'
    output-dir: docs`;

/* ── Syntax-highlighted renderers ── */

function YamlComment({ children }: { children: string }) {
  return <span className="ci-guide-syn-comment">{children}</span>;
}

function UsagePanel({ tab }: { tab: UsageTab }) {
  switch (tab) {
    case "dotnet-tool":
      return (
        <div className="ci-guide-panel active">
          <YamlComment># .github/workflows/docs.yml</YamlComment>{"\n"}
          <span className="ci-guide-syn-key">steps</span>:{"\n"}
          {"  "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">actions/checkout@v6</span>{"\n"}
          {"\n"}
          {"  "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">JKamsker/InSpectra@v1</span>{"\n"}
          {"    "}<span className="ci-guide-syn-flag">with</span>:{"\n"}
          {"      "}<span className="ci-guide-syn-arg">dotnet-tool</span>: MyCli.Tool{"        "}<YamlComment># installs the CLI for you</YamlComment>{"\n"}
          {"      "}<span className="ci-guide-syn-arg">cli-name</span>: mycli{"\n"}
          {"      "}<span className="ci-guide-syn-arg">output-dir</span>: docs/cli{"\n"}
          {"\n"}
          {"  "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">actions/upload-artifact@v4</span>{"\n"}
          {"    "}<span className="ci-guide-syn-flag">with</span>:{"\n"}
          {"      "}<span className="ci-guide-syn-arg">name</span>: cli-docs{"\n"}
          {"      "}<span className="ci-guide-syn-arg">path</span>: docs/cli
        </div>
      );
    case "from-file":
      return (
        <div className="ci-guide-panel active">
          <YamlComment># Render from pre-exported opencli.json</YamlComment>{"\n"}
          <span className="ci-guide-syn-key">steps</span>:{"\n"}
          {"  "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">actions/checkout@v6</span>{"\n"}
          {"\n"}
          {"  "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">JKamsker/InSpectra@v1</span>{"\n"}
          {"    "}<span className="ci-guide-syn-flag">with</span>:{"\n"}
          {"      "}<span className="ci-guide-syn-arg">mode</span>: file{"\n"}
          {"      "}<span className="ci-guide-syn-arg">opencli-json</span>: docs/opencli.json{"\n"}
          {"      "}<span className="ci-guide-syn-arg">xmldoc</span>: docs/xmldoc.xml{"         "}<YamlComment># optional</YamlComment>{"\n"}
          {"      "}<span className="ci-guide-syn-arg">output-dir</span>: docs/cli
        </div>
      );
    case "markdown":
      return (
        <div className="ci-guide-panel active">
          <YamlComment># Generate Markdown instead of HTML</YamlComment>{"\n"}
          <span className="ci-guide-syn-key">steps</span>:{"\n"}
          {"  "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">actions/checkout@v6</span>{"\n"}
          {"\n"}
          {"  "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">JKamsker/InSpectra@v1</span>{"\n"}
          {"    "}<span className="ci-guide-syn-flag">with</span>:{"\n"}
          {"      "}<span className="ci-guide-syn-arg">dotnet-tool</span>: MyCli.Tool{"\n"}
          {"      "}<span className="ci-guide-syn-arg">cli-name</span>: mycli{"\n"}
          {"      "}<span className="ci-guide-syn-arg">format</span>: markdown{"               "}<YamlComment># or markdown-monolith</YamlComment>{"\n"}
          {"      "}<span className="ci-guide-syn-arg">output-dir</span>: docs/cli
        </div>
      );
    case "build-render":
      return (
        <div className="ci-guide-panel active">
          <YamlComment># Build your CLI from source, then render</YamlComment>{"\n"}
          <span className="ci-guide-syn-key">steps</span>:{"\n"}
          {"  "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">actions/checkout@v6</span>{"\n"}
          {"\n"}
          {"  "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">actions/setup-dotnet@v5</span>{"\n"}
          {"    "}<span className="ci-guide-syn-flag">with</span>:{"\n"}
          {"      "}<span className="ci-guide-syn-arg">dotnet-version</span>: 10.0.x{"\n"}
          {"\n"}
          {"  "}- <span className="ci-guide-syn-arg">run</span>: dotnet build src/MyCli --configuration Release{"\n"}
          {"\n"}
          {"  "}- <span className="ci-guide-syn-arg">run</span>: dotnet publish src/MyCli -o ./publish --no-build -c Release{"\n"}
          {"\n"}
          {"  "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">JKamsker/InSpectra@v1</span>{"\n"}
          {"    "}<span className="ci-guide-syn-flag">with</span>:{"\n"}
          {"      "}<span className="ci-guide-syn-arg">cli-name</span>: ./publish/mycli{"       "}<YamlComment># path to built binary</YamlComment>{"\n"}
          {"      "}<span className="ci-guide-syn-arg">output-dir</span>: _site
        </div>
      );
    case "release-asset":
      return (
        <div className="ci-guide-panel active">
          <YamlComment># Attach docs to a GitHub Release</YamlComment>{"\n"}
          <span className="ci-guide-syn-key">steps</span>:{"\n"}
          {"  "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">actions/checkout@v6</span>{"\n"}
          {"\n"}
          {"  "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">JKamsker/InSpectra@v1</span>{"\n"}
          {"    "}<span className="ci-guide-syn-flag">with</span>:{"\n"}
          {"      "}<span className="ci-guide-syn-arg">dotnet-tool</span>: MyCli.Tool{"\n"}
          {"      "}<span className="ci-guide-syn-arg">cli-name</span>: mycli{"\n"}
          {"      "}<span className="ci-guide-syn-arg">output-dir</span>: cli-docs{"\n"}
          {"\n"}
          {"  "}- <span className="ci-guide-syn-arg">run</span>: zip -r cli-docs.zip cli-docs/{"\n"}
          {"\n"}
          {"  "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">softprops/action-gh-release@v2</span>{"\n"}
          {"    "}<span className="ci-guide-syn-flag">with</span>:{"\n"}
          {"      "}<span className="ci-guide-syn-arg">files</span>: cli-docs.zip
        </div>
      );
  }
}

/* ── Input reference data ── */

interface InputDef {
  name: string;
  desc: React.ReactNode;
  defaultVal: React.ReactNode;
}

const inputs: InputDef[] = [
  {
    name: "mode",
    desc: <>Render mode: <code>exec</code> invokes a live CLI, <code>file</code> reads from saved JSON.</>,
    defaultVal: <><code>exec</code></>,
  },
  {
    name: "format",
    desc: <>Output format: <code>html</code> (interactive SPA), <code>markdown</code> (tree), or <code>markdown-monolith</code> (single file).</>,
    defaultVal: <><code>html</code></>,
  },
  {
    name: "cli-name",
    desc: <>CLI executable name or path. Required for exec mode.</>,
    defaultVal: <>Required</>,
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
    defaultVal: <>Required</>,
  },
  {
    name: "xmldoc",
    desc: <>Path to <code>xmldoc.xml</code> for enrichment. File mode only.</>,
    defaultVal: <>Optional</>,
  },
  {
    name: "output-dir",
    desc: <>Directory where the output is written.</>,
    defaultVal: <><code>inspectra-output</code></>,
  },
  {
    name: "opencli-args",
    desc: <>Override the OpenCLI export arguments (exec mode).</>,
    defaultVal: <><code>cli opencli</code></>,
  },
  {
    name: "xmldoc-args",
    desc: <>Override the xmldoc export arguments (exec mode).</>,
    defaultVal: <><code>cli xmldoc</code></>,
  },
  {
    name: "timeout",
    desc: <>Timeout in seconds for each CLI export command (exec mode).</>,
    defaultVal: <>Optional</>,
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
    name: "dotnet-version",
    desc: <>.NET SDK version to install.</>,
    defaultVal: <><code>10.0.x</code></>,
  },
  {
    name: "dotnet-quality",
    desc: <>.NET SDK quality channel (<code>preview</code> for pre-release SDKs).</>,
    defaultVal: <>Optional</>,
  },
];

/* ── Pipeline steps ── */

const pipelineSteps = [
  { label: "Push", sub: "git push" },
  { label: "Install", sub: ".NET + tools" },
  { label: "Generate", sub: "inspectra" },
  { label: "Deploy", sub: "gh-pages" },
];

/* ── Main component ── */

export function CIGuidePage() {
  const [activeUsageTab, setActiveUsageTab] = useState<UsageTab>("dotnet-tool");

  const usageTabs: { id: UsageTab; label: string }[] = [
    { id: "dotnet-tool", label: ".NET Tool" },
    { id: "from-file", label: "From File" },
    { id: "markdown", label: "Markdown" },
    { id: "build-render", label: "Build + Render" },
    { id: "release-asset", label: "Release Asset" },
  ];

  return (
    <main className="ci-guide-page">
      {/* ── Hero ── */}
      <section className="ci-guide-hero">
        <div className="ci-guide-hero-glow" aria-hidden="true" />
        <div className="ci-guide-badge">
          <span className="ci-guide-dot" />
          GitHub Actions
        </div>
        <h1>
          Automate your <span className="ci-guide-accent">CLI docs</span>
        </h1>
        <p className="ci-guide-hero-sub">
          Generate InSpectraUI documentation in CI and deploy to GitHub Pages, attach as a
          release asset, or download as an artifact. One workflow call, zero config.
        </p>

        {/* Pipeline visualization */}
        <div className="ci-guide-pipeline" aria-hidden="true">
          {pipelineSteps.map((step, i) => (
            <div key={step.label} className="ci-guide-pipe-group">
              {i > 0 && (
                <div className="ci-guide-pipe-line">
                  <div className="ci-guide-pipe-pulse" />
                </div>
              )}
              <div className="ci-guide-pipe-node">
                <span className="ci-guide-pipe-ring" />
                <div className="ci-guide-pipe-text">
                  <span className="ci-guide-pipe-label">{step.label}</span>
                  <span className="ci-guide-pipe-sub">{step.sub}</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* ── Quick nav ── */}
      <nav className="ci-guide-nav" aria-label="Page sections">
        <a href="#usage" className="ci-guide-nav-link">Usage</a>
        <span className="ci-guide-nav-sep" aria-hidden="true" />
        <a href="#inputs" className="ci-guide-nav-link">Input Reference</a>
        <span className="ci-guide-nav-sep" aria-hidden="true" />
        <a href="#pages" className="ci-guide-nav-link">GitHub Pages</a>
        <span className="ci-guide-nav-sep" aria-hidden="true" />
        <a href="#prerequisites" className="ci-guide-nav-link">Prerequisites</a>
      </nav>

      {/* ── Timeline content ── */}
      <div className="ci-guide-timeline">

        {/* ── 01 · Usage ── */}
        <section id="usage" className="ci-guide-section">
          <div className="ci-guide-step-marker"><span>01</span></div>
          <div className="ci-guide-section-content">
            <div className="ci-guide-section-label">Getting Started</div>
            <h2 className="ci-guide-section-title">Add one step to your workflow</h2>
            <p className="ci-guide-section-desc">
              Install your CLI, then call the InSpectra action. It handles .NET, InSpectra, rendering,
              and verification. XML documentation is auto-detected and used for enrichment when available.
            </p>

            <div className="ci-guide-terminal">
              <div className="ci-guide-terminal-bar">
                <span className="ci-guide-terminal-dot ci-guide-tdot-red" />
                <span className="ci-guide-terminal-dot ci-guide-tdot-yellow" />
                <span className="ci-guide-terminal-dot ci-guide-tdot-green" />
                <span className="ci-guide-terminal-title">docs.yml</span>
              </div>
              <div className="ci-guide-tab-bar">
                {usageTabs.map((t) => (
                  <button
                    key={t.id}
                    type="button"
                    className={`ci-guide-tab${activeUsageTab === t.id ? " active" : ""}`}
                    onClick={() => setActiveUsageTab(t.id)}
                  >
                    {t.label}
                  </button>
                ))}
              </div>
              <CopyButton text={usageSnippets[activeUsageTab]} />
              <UsagePanel tab={activeUsageTab} />
            </div>

            <div className="ci-guide-prose">
              <p>
                The action installs <strong>.NET</strong> and <strong>InSpectra.Gen</strong> automatically.
                It does <em>not</em> install your CLI &mdash; add that step before the action.
              </p>
            </div>
          </div>
        </section>

        {/* ── 02 · Input Reference ── */}
        <section id="inputs" className="ci-guide-section">
          <div className="ci-guide-step-marker"><span>02</span></div>
          <div className="ci-guide-section-content">
            <div className="ci-guide-section-label">Reference</div>
            <h2 className="ci-guide-section-title">Inputs</h2>
            <p className="ci-guide-section-desc">
              All inputs accepted by <code>JKamsker/InSpectra@v1</code>.
            </p>

            <div className="ci-guide-callout">
              <div className="ci-guide-callout-icon">
                <Info size={15} aria-hidden="true" />
              </div>
              <div className="ci-guide-callout-body">
                <div className="ci-guide-callout-title">Automatic XML enrichment</div>
                <p>
                  In exec mode, the action automatically probes for <code>cli xmldoc</code> support and uses it
                  when available. No flag needed &mdash; richer descriptions are included transparently.
                </p>
              </div>
            </div>

            <div className="ci-guide-table-wrap">
              <table className="ci-guide-table">
                <thead>
                  <tr>
                    <th>Input</th>
                    <th>Description</th>
                    <th>Default</th>
                  </tr>
                </thead>
                <tbody>
                  {inputs.map((input) => (
                    <tr key={input.name}>
                      <td className="ci-guide-table-name"><code>{input.name}</code></td>
                      <td className="ci-guide-table-desc">{input.desc}</td>
                      <td className="ci-guide-table-default">{input.defaultVal}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </section>

        {/* ── 03 · GitHub Pages ── */}
        <section id="pages" className="ci-guide-section">
          <div className="ci-guide-step-marker"><span>03</span></div>
          <div className="ci-guide-section-content">
            <div className="ci-guide-section-label">Deployment</div>
            <h2 className="ci-guide-section-title">GitHub Pages</h2>
            <p className="ci-guide-section-desc">
              Add a deploy job after the generate job. The reusable workflow uploads the artifact;
              you control when and how it gets deployed.
            </p>

            <div className="ci-guide-prose">
              <p>
                <strong>1. Configure your repo</strong> &mdash;
                Go to <strong>Settings &rarr; Pages</strong> and set the source to{" "}
                <strong>GitHub Actions</strong> (not "Deploy from a branch").
              </p>
              <p>
                <strong>2. Add a deploy job</strong> &mdash;
                Download the artifact, re-upload as a Pages artifact, and deploy. Grant{" "}
                <code>pages: write</code> and <code>id-token: write</code> permissions.
              </p>
            </div>

            <div className="ci-guide-terminal">
              <div className="ci-guide-terminal-bar">
                <span className="ci-guide-terminal-dot ci-guide-tdot-red" />
                <span className="ci-guide-terminal-dot ci-guide-tdot-yellow" />
                <span className="ci-guide-terminal-dot ci-guide-tdot-green" />
                <span className="ci-guide-terminal-title">.github/workflows/docs.yml</span>
              </div>
              <CopyButton text={pagesSnippet} />
              <div className="ci-guide-code-body">
                <span className="ci-guide-syn-key">name</span>: Deploy CLI Docs{"\n"}
                {"\n"}
                <span className="ci-guide-syn-key">on</span>:{"\n"}
                {"  "}<span className="ci-guide-syn-arg">push</span>:{"\n"}
                {"    "}<span className="ci-guide-syn-arg">branches</span>: [main]{"\n"}
                {"\n"}
                <span className="ci-guide-syn-key">permissions</span>:{"\n"}
                {"  "}<span className="ci-guide-syn-arg">contents</span>: read{"\n"}
                {"  "}<span className="ci-guide-syn-arg">pages</span>: write{"\n"}
                {"  "}<span className="ci-guide-syn-arg">id-token</span>: write{"\n"}
                {"\n"}
                <span className="ci-guide-syn-key">jobs</span>:{"\n"}
                {"  "}<span className="ci-guide-syn-arg">generate</span>:{"\n"}
                {"    "}<span className="ci-guide-syn-flag">runs-on</span>: ubuntu-latest{"\n"}
                {"    "}<span className="ci-guide-syn-flag">steps</span>:{"\n"}
                {"      "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">actions/checkout@v6</span>{"\n"}
                {"      "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">JKamsker/InSpectra@v1</span>{"\n"}
                {"        "}<span className="ci-guide-syn-flag">with</span>:{"\n"}
                {"          "}<span className="ci-guide-syn-arg">dotnet-tool</span>: MyCli.Tool{"\n"}
                {"          "}<span className="ci-guide-syn-arg">cli-name</span>: mycli{"\n"}
                {"          "}<span className="ci-guide-syn-arg">output-dir</span>: _site{"\n"}
                {"      "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">actions/upload-pages-artifact@v3</span>{"\n"}
                {"        "}<span className="ci-guide-syn-flag">with</span>: {"{ "}<span className="ci-guide-syn-arg">path</span>: _site{" }"}{"\n"}
                {"\n"}
                {"  "}<span className="ci-guide-syn-arg">deploy</span>:{"\n"}
                {"    "}<span className="ci-guide-syn-flag">needs</span>: generate{"\n"}
                {"    "}<span className="ci-guide-syn-flag">runs-on</span>: ubuntu-latest{"\n"}
                {"    "}<span className="ci-guide-syn-flag">environment</span>:{"\n"}
                {"      "}<span className="ci-guide-syn-arg">name</span>: github-pages{"\n"}
                {"      "}<span className="ci-guide-syn-arg">url</span>: <span className="ci-guide-syn-str">{"${{ steps.deploy.outputs.page_url }}"}</span>{"\n"}
                {"    "}<span className="ci-guide-syn-flag">steps</span>:{"\n"}
                {"      "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">actions/deploy-pages@v4</span>{"\n"}
                {"        "}<span className="ci-guide-syn-arg">id</span>: deploy
              </div>
            </div>

            <div className="ci-guide-callout ci-guide-callout-warn">
              <div className="ci-guide-callout-icon">
                <AlertCircle size={15} aria-hidden="true" />
              </div>
              <div className="ci-guide-callout-body">
                <div className="ci-guide-callout-title">Custom domain</div>
                <p>
                  To use a custom domain, configure it in your repository's Pages settings.
                  GitHub handles the CNAME automatically when deploying via Actions.
                </p>
              </div>
            </div>
          </div>
        </section>

        {/* ── 04 · Prerequisites ── */}
        <section id="prerequisites" className="ci-guide-section">
          <div className="ci-guide-step-marker"><span>04</span></div>
          <div className="ci-guide-section-content">
            <div className="ci-guide-section-label">Requirements</div>
            <h2 className="ci-guide-section-title">Prerequisites</h2>
            <p className="ci-guide-section-desc">
              Your CLI must support the OpenCLI specification for InSpectra to generate documentation from it.
            </p>

            <div className="ci-guide-prose">
              <p>
                <strong>For exec mode</strong>, your CLI needs to implement the <code>cli opencli</code> command
                which outputs the OpenCLI JSON spec to stdout. Optionally implement <code>cli xmldoc</code>{" "}
                for richer descriptions. CLIs built with{" "}
                <a
                  href="https://github.com/spectreconsole/spectre.console"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="ci-guide-link"
                >
                  Spectre.Console.Cli
                </a>{" "}
                get this for free.
              </p>
              <p>
                <strong>For file mode</strong>, export your <code>opencli.json</code> once and check it into your
                repository. This works with any CLI that can produce an OpenCLI spec, even if it's not available
                at CI runtime.
              </p>
              <p>
                If your CLI uses custom export arguments (not <code>cli opencli</code>), pass them via{" "}
                <code>opencli-args</code>. Similarly, use <code>xmldoc-args</code> to override the XML
                documentation export command.
              </p>
            </div>

            <div className="ci-guide-terminal">
              <div className="ci-guide-terminal-bar">
                <span className="ci-guide-terminal-dot ci-guide-tdot-red" />
                <span className="ci-guide-terminal-dot ci-guide-tdot-yellow" />
                <span className="ci-guide-terminal-dot ci-guide-tdot-green" />
                <span className="ci-guide-terminal-title">Custom export arguments</span>
              </div>
              <CopyButton text={prerequisitesSnippet} />
              <div className="ci-guide-code-body">
                <YamlComment># If your CLI uses different export commands</YamlComment>{"\n"}
                <span className="ci-guide-syn-key">- uses</span>: <span className="ci-guide-syn-str">JKamsker/InSpectra@v1</span>{"\n"}
                {"  "}<span className="ci-guide-syn-flag">with</span>:{"\n"}
                {"    "}<span className="ci-guide-syn-arg">cli-name</span>: mycli{"\n"}
                {"    "}<span className="ci-guide-syn-arg">opencli-args</span>: <span className="ci-guide-syn-str">'export spec'</span>{"       "}<YamlComment># instead of 'cli opencli'</YamlComment>{"\n"}
                {"    "}<span className="ci-guide-syn-arg">xmldoc-args</span>: <span className="ci-guide-syn-str">'export xmldoc'</span>{"      "}<YamlComment># instead of 'cli xmldoc'</YamlComment>{"\n"}
                {"    "}<span className="ci-guide-syn-arg">output-dir</span>: docs
              </div>
            </div>
          </div>
        </section>
      </div>
    </main>
  );
}
