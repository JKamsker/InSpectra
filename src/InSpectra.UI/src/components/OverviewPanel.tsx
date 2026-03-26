import { Boxes, Layers3, PackageOpen, Sparkles, Wand2 } from "lucide-react";
import { type ReactNode } from "react";
import { buildFacts, buildSummary } from "../data/overview";
import { NormalizedCliDocument, formatOptionValue } from "../data/normalize";

interface OverviewPanelProps {
  document: NormalizedCliDocument;
  includeMetadata: boolean;
  onCommandSelect: (path: string) => void;
}

export function OverviewPanel({ document, includeMetadata, onCommandSelect }: OverviewPanelProps) {
  const facts = buildFacts(document);
  const summary = buildSummary(document);

  const isEmpty =
    document.commands.length === 0 &&
    document.rootArguments.length === 0 &&
    document.rootOptions.length === 0;

  if (isEmpty) {
    return (
      <section className="hero-band panel empty-package">
        <PackageOpen aria-hidden="true" className="empty-package-icon" />
        <div className="eyebrow">Overview</div>
        <h1>{document.source.info.title || "Untitled CLI"}</h1>
        <p className="lede">
          This package doesn't declare any commands, arguments, or options.
          It may not include an OpenCLI manifest, or the manifest is empty.
        </p>
      </section>
    );
  }

  return (
    <>
      <section className="hero-band panel">
        <div className="eyebrow">Overview</div>
        <h1>{document.source.info.title || "Untitled CLI"}</h1>
        <p className="lede">{summary ?? document.source.info.description ?? "No summary available."}</p>

        <div className="hero-grid">
          {facts.map(([label, value], index) => (
            <article key={label} className={`stat-card stat-card-${index + 1}`}>
              <span>{label}</span>
              <strong>{value}</strong>
            </article>
          ))}
        </div>
      </section>

      <section className="panel section-card">
        <div className="section-heading">
          <Boxes aria-hidden="true" />
          <h2>Command surface</h2>
        </div>
        <div className="command-card-grid">
          {document.commands.map((command) => (
            <button
              key={command.path}
              type="button"
              className="command-card"
              onClick={() => onCommandSelect(command.path)}
            >
              <strong>{command.command.name}</strong>
              <span>{command.command.description ?? "No description"}</span>
              <small>{command.commands.length > 0 ? `${command.commands.length} subcommands` : "Leaf command"}</small>
            </button>
          ))}
        </div>
      </section>

      <DetailList
        icon={<Layers3 aria-hidden="true" />}
        title="Root arguments"
        items={document.rootArguments.map((argument) => ({
          key: argument.name,
          title: argument.name,
          body: argument.description ?? "No description",
          footnote:
            argument.acceptedValues.length > 0
              ? `Accepted values: ${argument.acceptedValues.join(", ")}`
              : undefined,
        }))}
      />

      <DetailList
        icon={<Wand2 aria-hidden="true" />}
        title="Root options"
        items={document.rootOptions.map((option) => ({
          key: option.name,
          title: option.name,
          body: option.description ?? "No description",
          footnote: `${formatOptionValue(option)}${option.recursive ? " · recursive" : ""}`,
        }))}
      />

      {includeMetadata && document.source.metadata.length > 0 ? (
        <DetailList
          icon={<Sparkles aria-hidden="true" />}
          title="Metadata"
          items={document.source.metadata.map((item) => ({
            key: item.name,
            title: item.name,
            body: formatMetadataValue(item.value),
          }))}
        />
      ) : null}
    </>
  );
}

function DetailList({
  icon,
  title,
  items,
}: {
  icon: ReactNode;
  title: string;
  items: Array<{ key: string; title: string; body: string; footnote?: string }>;
}) {
  if (items.length === 0) return null;

  return (
    <section className="panel section-card">
      <div className="section-heading">
        {icon}
        <h2>{title}</h2>
      </div>
      <div className="detail-grid">
        {items.map((item) => (
          <article key={item.key} className="detail-card">
            <strong>{item.title}</strong>
            <p>{item.body}</p>
            {item.footnote ? <small>{item.footnote}</small> : null}
          </article>
        ))}
      </div>
    </section>
  );
}

function formatMetadataValue(value: unknown): string {
  return typeof value === "string" ? value : JSON.stringify(value);
}
