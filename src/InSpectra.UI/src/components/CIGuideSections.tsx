import { AlertCircle, Info } from "lucide-react";
import { ciGuideCopyText, CIGuideCopyButton, PagesWorkflowPanel, PrerequisitesSnippetPanel, UsagePanel } from "./CIGuideCode";
import { ciGuideInputs, usageSnippets, usageTabs, type UsageTab } from "./CIGuidePageData";

interface UsageSectionProps {
  activeUsageTab: UsageTab;
  onSelectUsageTab: (tab: UsageTab) => void;
}

export function CIGuideUsageSection({ activeUsageTab, onSelectUsageTab }: UsageSectionProps) {
  return (
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
            {usageTabs.map((tab) => (
              <button
                key={tab.id}
                type="button"
                className={`ci-guide-tab${activeUsageTab === tab.id ? " active" : ""}`}
                onClick={() => onSelectUsageTab(tab.id)}
              >
                {tab.label}
              </button>
            ))}
          </div>
          <CIGuideCopyButton text={usageSnippets[activeUsageTab]} />
          <UsagePanel tab={activeUsageTab} />
        </div>

        <div className="ci-guide-prose">
          <p>
            The action installs <strong>.NET</strong> and <strong>InSpectra.Gen</strong> automatically.
            In <code>dotnet</code> mode it also reads your project's{" "}
            <code>TargetFramework</code> and installs the matching SDK
            (skipping versions already on the runner). When <code>opencli-mode</code>{" "}
            is left empty or set to <code>native</code>, and unless you set{" "}
            <code>skip-inspectra-cli: 'true'</code>, it may also add the{" "}
            <code>InSpectra.Cli</code> <code>PackageReference</code> to the checked-out
            project so <code>cli opencli</code> / <code>cli xmldoc</code> are available
            for the rest of the job. In <code>exec</code> mode you still install your CLI
            yourself (or use <code>dotnet-tool</code>).
          </p>
        </div>
      </div>
    </section>
  );
}

export function CIGuideInputsSection() {
  return (
    <section id="inputs" className="ci-guide-section">
      <div className="ci-guide-step-marker"><span>02</span></div>
      <div className="ci-guide-section-content">
        <div className="ci-guide-section-label">Reference</div>
        <h2 className="ci-guide-section-title">Inputs</h2>
        <p className="ci-guide-section-desc">
          Common inputs used by the examples on this page. This is not an exhaustive
          mirror of <code>JKamsker/InSpectra@v1</code>.
        </p>

        <div className="ci-guide-callout">
          <div className="ci-guide-callout-icon">
            <Info size={15} aria-hidden="true" />
          </div>
          <div className="ci-guide-callout-body">
            <div className="ci-guide-callout-title">Automatic XML enrichment</div>
            <p>
              In <code>exec</code> and <code>dotnet</code> modes, the action automatically probes for{" "}
              <code>cli xmldoc</code> support and uses it when available. In <code>package</code>{" "}
              mode, XML enrichment is enabled by default with the same command unless you override{" "}
              <code>xmldoc-args</code>.
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
              {ciGuideInputs.map((input) => (
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
  );
}

export function CIGuidePagesSection() {
  return (
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
            Upload the generated site as a Pages artifact, then deploy it. Grant{" "}
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
          <CIGuideCopyButton text={ciGuideCopyText.pages} />
          <PagesWorkflowPanel />
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
  );
}

export function CIGuidePrerequisitesSection() {
  return (
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
            <strong>For dotnet mode</strong> (recommended when the CLI source lives in the
            same repo), start with a normal checkout step in your workflow, then point the
            action at the project. When <code>opencli-mode</code> is left empty or set to{" "}
            <code>native</code>, the action can auto-add the <code>InSpectra.Cli</code>{" "}
            <code>PackageReference</code> if it isn't already there and run{" "}
            <code>dotnet run --project &lt;PROJECT&gt; -- cli opencli</code>. That
            project-file change remains in the checked-out workspace for later steps unless you
            set <code>skip-inspectra-cli: 'true'</code> or manage the dependency yourself. CLIs
            built with{" "}
            <a
              href="https://github.com/spectreconsole/spectre.console"
              target="_blank"
              rel="noopener noreferrer"
              className="ci-guide-link"
            >
              Spectre.Console.Cli
            </a>{" "}
            are a common fit because <code>InSpectra.Cli</code> wires up the export commands for you.
          </p>
          <p>
            <strong>For exec mode</strong>, your CLI needs to implement the <code>cli opencli</code> command
            which outputs the OpenCLI JSON spec to stdout. Optionally implement <code>cli xmldoc</code>{" "}
            for richer descriptions. Adding the <code>InSpectra.Cli</code> NuGet package to your project
            provides both commands.
          </p>
          <p>
            <strong>For package mode</strong>, the published .NET tool package should expose{" "}
            <code>cli opencli</code>. If it also exposes <code>cli xmldoc</code>, the action uses that
            XML export by default unless you override <code>xmldoc-args</code>.
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
          <CIGuideCopyButton text={ciGuideCopyText.prerequisites} />
          <PrerequisitesSnippetPanel />
        </div>
      </div>
    </section>
  );
}
