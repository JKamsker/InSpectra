import { pagesSnippet, prerequisitesSnippet, type UsageTab } from "./CIGuidePageData";
import { CopyButton } from "./CopyButton";

export function YamlComment({ children }: { children: string }) {
  return <span className="ci-guide-syn-comment">{children}</span>;
}

export function CIGuideCopyButton({ text }: { text: string }) {
  return (
    <CopyButton
      text={text}
      className="ci-guide-copy-btn"
      title="Copy to clipboard"
      copiedTitle="Copied"
      label="Copy"
      copiedLabel="Copied"
      iconSize={13}
      resetDelayMs={2000}
    />
  );
}

export function UsagePanel({ tab }: { tab: UsageTab }) {
  switch (tab) {
    case "from-source":
      return (
        <div className="ci-guide-panel active">
          <YamlComment># .github/workflows/docs.yml — render straight from a .csproj</YamlComment>{"\n"}
          <span className="ci-guide-syn-key">steps</span>:{"\n"}
          {"  "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">actions/checkout@v6</span>{"\n"}
          {"\n"}
          {"  "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">JKamsker/InSpectra@v1</span>{"\n"}
          {"    "}<span className="ci-guide-syn-flag">with</span>:{"\n"}
          {"      "}<span className="ci-guide-syn-arg">mode</span>: dotnet{"\n"}
          {"      "}<span className="ci-guide-syn-arg">project</span>: src/MyCli{"              "}<YamlComment># .csproj path or directory</YamlComment>{"\n"}
          {"      "}<span className="ci-guide-syn-arg">configuration</span>: Release{"\n"}
          {"      "}<span className="ci-guide-syn-arg">format</span>: html{"                    "}<YamlComment># html / markdown / markdown-monolith / markdown-hybrid</YamlComment>{"\n"}
          {"      "}<span className="ci-guide-syn-arg">output-dir</span>: docs/cli{"\n"}
          {"\n"}
          {"  "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">actions/upload-artifact@v4</span>{"\n"}
          {"    "}<span className="ci-guide-syn-flag">with</span>:{"\n"}
          {"      "}<span className="ci-guide-syn-arg">name</span>: cli-docs{"\n"}
          {"      "}<span className="ci-guide-syn-arg">path</span>: docs/cli
        </div>
      );
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

export function PagesWorkflowPanel() {
  return (
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
      {"      "}- <span className="ci-guide-syn-flag">uses</span>: <span className="ci-guide-syn-str">actions/upload-pages-artifact@v4</span>{"\n"}
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
  );
}

export function PrerequisitesSnippetPanel() {
  return (
    <div className="ci-guide-code-body">
      <YamlComment># If your CLI uses different export commands</YamlComment>{"\n"}
      <span className="ci-guide-syn-key">- uses</span>: <span className="ci-guide-syn-str">JKamsker/InSpectra@v1</span>{"\n"}
      {"  "}<span className="ci-guide-syn-flag">with</span>:{"\n"}
      {"    "}<span className="ci-guide-syn-arg">cli-name</span>: mycli{"\n"}
      {"    "}<span className="ci-guide-syn-arg">opencli-args</span>: <span className="ci-guide-syn-str">'export spec'</span>{"       "}<YamlComment># instead of 'cli opencli'</YamlComment>{"\n"}
      {"    "}<span className="ci-guide-syn-arg">xmldoc-args</span>: <span className="ci-guide-syn-str">'export xmldoc'</span>{"      "}<YamlComment># instead of 'cli xmldoc'</YamlComment>{"\n"}
      {"    "}<span className="ci-guide-syn-arg">output-dir</span>: docs
    </div>
  );
}

export const ciGuideCopyText = {
  pages: pagesSnippet,
  prerequisites: prerequisitesSnippet,
};
