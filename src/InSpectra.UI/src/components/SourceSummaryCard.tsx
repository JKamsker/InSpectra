import { Search } from "lucide-react";
import { ProbePackageSummary } from "../data/toolProbe";

interface SourceSummaryCardProps {
  summary: ProbePackageSummary;
}

export function SourceSummaryCard({ summary }: SourceSummaryCardProps) {
  return (
    <section className="section-card panel">
      <div className="section-heading">
        <Search aria-hidden="true" />
        <h2>NuGet Tool Probe</h2>
      </div>

      <div className="chip-row">
        <span className="info-chip">{summary.id}</span>
        <span className="info-chip subtle">{summary.version}</span>
        <span className="info-chip subtle">{summary.confidence}</span>
        {summary.targetFramework ? <span className="info-chip subtle">{summary.targetFramework}</span> : null}
      </div>

      <div className="detail-grid source-summary-grid">
        <div className="detail-card">
          <strong>Command</strong>
          <p>{summary.commandName || "Unknown"}</p>
        </div>
        <div className="detail-card">
          <strong>Entry point</strong>
          <p>{summary.entryPoint || "Unknown"}</p>
        </div>
        <div className="detail-card">
          <strong>Spectre.Console.Cli</strong>
          <p>{summary.isSpectreCli ? "Detected" : "Not detected"}</p>
        </div>
        <div className="detail-card">
          <strong>Packaged OpenCLI</strong>
          <p>{summary.hasPackagedOpenCli ? "Bundled snapshot" : "Static recovery"}</p>
        </div>
      </div>
    </section>
  );
}
