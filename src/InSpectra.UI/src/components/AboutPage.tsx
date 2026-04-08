import { ArrowRight, Copy, Check, ExternalLink } from "lucide-react";
import { GitHubIcon } from "./GitHubIcon";
import { useState, useCallback } from "react";

const isWindows = typeof navigator !== "undefined" && /Windows/.test(navigator.userAgent);
const lc = isWindows ? "`" : "\\";

const snippets = {
  install: "dotnet tool install -g InSpectra.Gen",
  fileHtml: `inspectra render file html ${lc}\n  mycli.json --xmldoc mycli.xml ${lc}\n  --out-dir ./docs`,
  fileMd: `inspectra render file markdown ${lc}\n  mycli.json --out docs.md`,
  execHtml: `inspectra render exec html ${lc}\n  jf --with-xmldoc ${lc}\n  --out-dir ./jellyfin-docs`,
  execMd: `inspectra render exec markdown ${lc}\n  mytool --opencli-arg "cli opencli" ${lc}\n  --out docs.md`,
  dotnetHtml: `inspectra render dotnet html ${lc}\n  src/MyCli --configuration Release ${lc}\n  --no-build --with-xmldoc --out-dir ./docs`,
};

export function AboutPage() {
  const [copiedId, setCopiedId] = useState<string | null>(null);

  const copySnippet = useCallback((id: string, text: string) => {
    navigator.clipboard.writeText(text).then(() => {
      setCopiedId(id);
      setTimeout(() => setCopiedId((prev) => (prev === id ? null : prev)), 2000);
    });
  }, []);

  function CmdCopy({ id, text }: { id: string; text: string }) {
    const isCopied = copiedId === id;
    return (
      <button
        type="button"
        className={`qs-cmd-copy${isCopied ? " copied" : ""}`}
        onClick={() => copySnippet(id, text)}
        aria-label={isCopied ? "Copied" : "Copy command"}
      >
        {isCopied ? <Check aria-hidden="true" /> : <Copy aria-hidden="true" />}
      </button>
    );
  }

  return (
    <main className="qs-page">
      {/* Hero */}
      <section className="qs-hero">
        <div className="qs-badge">
          <span className="dot" />
          Built on the OpenCLI specification
        </div>
        <h1>
          Get started with<br />
          <span className="accent">InSpectra</span>
        </h1>
        <p className="qs-sub">
          Generate polished, interactive documentation from your CLI's OpenCLI
          spec — Markdown or HTML, one command.
        </p>
      </section>

      {/* Install */}
      <section className="qs-install-section">
        <div className="qs-install-card">
          <div className="qs-install-label">Install via .NET CLI</div>
          <div className="qs-install-block">
            <div className="qs-install-cmd">
              <span className="prompt">$</span>
              <span className="cmd">dotnet tool install</span>
              <span className="flag">-g</span>
              <span className="pkg">InSpectra.Gen</span>
            </div>
            <button
              type="button"
              className={`qs-copy-btn${copiedId === "install" ? " copied" : ""}`}
              onClick={() => copySnippet("install", snippets.install)}
            >
              {copiedId === "install" ? <Check aria-hidden="true" /> : <Copy aria-hidden="true" />}
              <span>{copiedId === "install" ? "Copied" : "Copy"}</span>
            </button>
          </div>
          <div className="qs-install-note">Requires .NET 10 SDK or later</div>
        </div>
      </section>

      {/* Quick Start */}
      <section className="qs-steps-section">
        <div className="qs-section-label">Quick Start</div>
        <h2 className="qs-section-title">Three steps to docs</h2>

        <div className="qs-timeline">
          {/* Step 1 */}
          <div className="qs-step">
            <div className="qs-step-rail">
              <span className="qs-step-num">1</span>
              <div className="qs-step-line" />
            </div>
            <div className="qs-step-body">
              <h3>Render from a file</h3>
              <p>
                Pass your <code>opencli.json</code> and optional{" "}
                <code>xmldoc.xml</code>. Choose Markdown or HTML output.
              </p>

              <div className="qs-cmd">
                <CmdCopy id="fileHtml" text={snippets.fileHtml} />
                <pre className="qs-code">
<span className="comment"># Generate an interactive HTML app bundle</span>{"\n"}
<span className="c">inspectra</span> <span className="a">render file html</span> <span className="lc">{lc}</span>{"\n"}
{"  "}mycli.json <span className="f">--xmldoc</span> mycli.xml <span className="lc">{lc}</span>{"\n"}
{"  "}<span className="f">--out-dir</span> ./docs</pre>
              </div>

              <div className="qs-cmd">
                <CmdCopy id="fileMd" text={snippets.fileMd} />
                <pre className="qs-code">
<span className="comment"># Or generate Markdown</span>{"\n"}
<span className="c">inspectra</span> <span className="a">render file markdown</span> <span className="lc">{lc}</span>{"\n"}
{"  "}mycli.json <span className="f">--out</span> docs.md</pre>
              </div>
            </div>
          </div>

          {/* Step 2 */}
          <div className="qs-step">
            <div className="qs-step-rail">
              <span className="qs-step-num">2</span>
              <div className="qs-step-line" />
            </div>
            <div className="qs-step-body">
              <h3>Or render from a live CLI &mdash; or from a .NET project on disk</h3>
              <p>
                Use <code>exec</code> mode to invoke a pre-built CLI directly, or{" "}
                <code>dotnet</code> mode to point at a <code>.csproj</code> and let
                InSpectra run <code>dotnet run -- cli opencli</code> for you.
              </p>

              <div className="qs-cmd">
                <CmdCopy id="execHtml" text={snippets.execHtml} />
                <pre className="qs-code">
<span className="comment"># Render directly from a running CLI</span>{"\n"}
<span className="c">inspectra</span> <span className="a">render exec html</span> <span className="lc">{lc}</span>{"\n"}
{"  "}jf <span className="f">--with-xmldoc</span> <span className="lc">{lc}</span>{"\n"}
{"  "}<span className="f">--out-dir</span> ./jellyfin-docs</pre>
              </div>

              <div className="qs-cmd">
                <CmdCopy id="dotnetHtml" text={snippets.dotnetHtml} />
                <pre className="qs-code">
<span className="comment"># Render from a .NET project source</span>{"\n"}
<span className="c">inspectra</span> <span className="a">render dotnet html</span> <span className="lc">{lc}</span>{"\n"}
{"  "}src/MyCli <span className="f">--configuration</span> Release <span className="lc">{lc}</span>{"\n"}
{"  "}<span className="f">--no-build</span> <span className="f">--with-xmldoc</span> <span className="f">--out-dir</span> ./docs</pre>
              </div>

              <div className="qs-cmd">
                <CmdCopy id="execMd" text={snippets.execMd} />
                <pre className="qs-code">
<span className="comment"># Custom OpenCLI arguments</span>{"\n"}
<span className="c">inspectra</span> <span className="a">render exec markdown</span> <span className="lc">{lc}</span>{"\n"}
{"  "}mytool <span className="f">--opencli-arg</span> "cli opencli" <span className="lc">{lc}</span>{"\n"}
{"  "}<span className="f">--out</span> docs.md</pre>
              </div>
            </div>
          </div>

          {/* Step 3 */}
          <div className="qs-step qs-step-last">
            <div className="qs-step-rail">
              <span className="qs-step-num">3</span>
            </div>
            <div className="qs-step-body">
              <h3>Open your docs</h3>
              <p>
                HTML bundles are fully self-contained — open{" "}
                <code>index.html</code> in any browser. Markdown renders
                natively on GitHub, GitLab, or any viewer.
              </p>
              <div className="qs-terminal-wrap">
                <div className="qs-terminal-bar">
                  <span className="dot-r" />
                  <span className="dot-y" />
                  <span className="dot-g" />
                  <span className="title">inspectra</span>
                  <span />
                </div>
                <div className="qs-terminal">
                  <div className="line"><span className="p">$</span> <span className="c">inspectra</span> <span className="a">render file html</span> <span>jellyfin.json</span> <span className="f">--xmldoc</span> <span>jellyfin.xml</span></div>
                  <div className="line"><span className="dim">&nbsp;&nbsp;Validating OpenCLI schema...</span> <span className="ok">OK</span></div>
                  <div className="line"><span className="dim">&nbsp;&nbsp;Normalizing 47 commands, 128 options...</span> <span className="ok">OK</span></div>
                  <div className="line"><span className="dim">&nbsp;&nbsp;Enriching from XML metadata...</span> <span className="ok">OK</span></div>
                  <div className="line"><span className="dim">&nbsp;&nbsp;Copying HTML app bundle...</span> <span className="ok">OK</span></div>
                  <div className="line"><span className="ok">&nbsp;&nbsp;&#10003; Written index.html and assets to jellyfin-docs/</span> <span className="dim">(relocatable static bundle)</span></div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Examples */}
      <section className="qs-examples-section">
        <div className="qs-section-label">Live Examples</div>
        <h2 className="qs-section-title">See it in action</h2>
        <p className="qs-section-desc">
          Real documentation generated by InSpectra. Click to explore the full
          interactive output.
        </p>

        <div className="qs-examples">
          <a href="#/pkg/InSpectra.Gen" className="qs-ex qs-ex-self">
            <span className="qs-ex-name">InSpectraGen</span>
            <span className="qs-ex-alias">inspectra</span>
            <span className="qs-ex-desc">
              The tool documents itself — full reference from its own OpenCLI
              spec.
            </span>
            <span className="qs-ex-link">
              View docs <ArrowRight aria-hidden="true" size={13} />
            </span>
          </a>
          <a href="#/pkg/JellyfinCli" className="qs-ex">
            <span className="qs-ex-name">Jellyfin CLI</span>
            <span className="qs-ex-alias">jf</span>
            <span className="qs-ex-desc">
              Manage your Jellyfin media server from the command line.
            </span>
            <span className="qs-ex-link">
              View docs <ArrowRight aria-hidden="true" size={13} />
            </span>
          </a>
          <a href="#/pkg/JDownloader-RemoteCli" className="qs-ex">
            <span className="qs-ex-name">JDownloader</span>
            <span className="qs-ex-alias">jdr</span>
            <span className="qs-ex-desc">
              Remote-control your JDownloader instance from the terminal.
            </span>
            <span className="qs-ex-link">
              View docs <ArrowRight aria-hidden="true" size={13} />
            </span>
          </a>

        </div>
      </section>

      {/* Footer */}
      <footer className="qs-footer">
        <div>
          Built on the{" "}
          <a
            href="https://opencli.org/"
            target="_blank"
            rel="noopener noreferrer"
          >
            OpenCLI specification
          </a>
        </div>
        <div className="qs-footer-links">
          <a
            href="https://github.com/JKamsker/InSpectra"
            target="_blank"
            rel="noopener noreferrer"
          >
            <GitHubIcon aria-hidden="true" size={14} />
            GitHub
          </a>
          <a
            href="https://opencli.org/"
            target="_blank"
            rel="noopener noreferrer"
          >
            <ExternalLink aria-hidden="true" size={14} />
            OpenCLI Spec
          </a>
        </div>
      </footer>
    </main>
  );
}
