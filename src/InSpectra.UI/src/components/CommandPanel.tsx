import { ChevronRight, CornerDownRight, Fingerprint, Shield, SquareTerminal } from "lucide-react";
import { type ReactNode } from "react";
import { buildCommandHash } from "../data/navigation";
import { CopyButton } from "./CopyButton";
import {
  NormalizedCommand,
  formatArity,
  formatOptionValue,
  getMetadataValue,
} from "../data/normalize";

interface CommandPanelProps {
  command: NormalizedCommand;
  cliTitle: string;
  includeMetadata: boolean;
  onCommandSelect: (path: string | undefined) => void;
  deepLinkHash?: string;
}

function resolveExample(example: string, command: NormalizedCommand, cliTitle: string): string {
  const fullPath = cliTitle ? `${cliTitle} ${command.path}` : command.path;
  // Already includes the full path (with CLI title)
  if (example === fullPath || example.startsWith(fullPath + " ")) return example;
  // Already includes the command path (without CLI title)
  if (example === command.path || example.startsWith(command.path + " ")) {
    return cliTitle ? `${cliTitle} ${example}` : example;
  }
  // Starts with just the leaf command name — replace with full path
  const name = command.command.name;
  if (example === name || example.startsWith(name + " ")) {
    return fullPath + example.slice(name.length);
  }
  // Starts with args only — prepend full path
  return `${fullPath} ${example}`;
}

export function CommandPanel({ command, cliTitle, includeMetadata, onCommandSelect, deepLinkHash }: CommandPanelProps) {
  const badges = [
    ...(command.command.interactive ? ["Interactive"] : []),
    ...(command.command.hidden ? ["Hidden"] : []),
    ...(command.command.aliases.length > 0 ? [`Aliases: ${command.command.aliases.join(", ")}`] : []),
  ];

  const displayPath = cliTitle ? `${cliTitle} ${command.path}` : command.path;

  return (
    <>
      <section className="panel command-hero">
        <div className="breadcrumb-row">
          {displayPath.split(" ").map((segment, index, all) => {
            const isLast = index === all.length - 1;
            const titleOffset = cliTitle ? 1 : 0;
            const commandPath = index < titleOffset ? undefined : all.slice(titleOffset, index + 1).join(" ");
            return (
              <span key={`${segment}-${index}`} className="crumb">
                {index > 0 ? <ChevronRight aria-hidden="true" /> : null}
                {isLast ? (
                  <strong>{segment}</strong>
                ) : (
                  <button type="button" className="crumb-link" onClick={() => onCommandSelect(commandPath)}>
                    {segment}
                  </button>
                )}
              </span>
            );
          })}
        </div>

        <div className="eyebrow">Command</div>
        <h1>{command.command.name}</h1>
        <p className="lede">{command.command.description ?? "No description available for this command."}</p>

        <div className="chip-row">
          {badges.map((badge) => (
            <span key={badge} className="info-chip">
              {badge}
            </span>
          ))}
          <span className="info-chip subtle">Deep link {deepLinkHash ?? buildCommandHash(command.path)}</span>
        </div>
      </section>

      <CommandGroup
        icon={<CornerDownRight aria-hidden="true" />}
        title="Subcommands"
        items={command.commands.map((child) => ({
          key: child.path,
          title: child.command.name,
          body: child.command.description ?? "No description",
          footnote: child.commands.length > 0 ? `${child.commands.length} nested commands` : "Leaf command",
          action: () => onCommandSelect(child.path),
          actionLabel: "Open",
        }))}
      />

      <CommandGroup
        icon={<SquareTerminal aria-hidden="true" />}
        title="Arguments"
        items={command.arguments.map((argument) => ({
          key: argument.name,
          title: argument.name,
          body: argument.description ?? "No description",
          footnote: `${argument.required ? "Required" : "Optional"} · arity ${formatArity(argument)}${formatClrType(argument.metadata)}`,
        }))}
      />

      <CommandGroup
        icon={<Shield aria-hidden="true" />}
        title="Declared options"
        items={command.declaredOptions.map((option) => ({
          key: option.name,
          title: option.name,
          body: option.description ?? "No description",
          footnote: `${formatOptionValue(option)}${option.required ? " · required" : ""}${option.recursive ? " · recursive" : ""}${formatClrType(option.metadata)}`,
        }))}
      />

      <CommandGroup
        icon={<Fingerprint aria-hidden="true" />}
        title="Inherited options"
        items={command.inheritedOptions.map((option) => ({
          key: option.option.name,
          title: option.option.name,
          body: option.option.description ?? "No description",
          footnote: `${formatOptionValue(option.option)} · inherited from ${option.inheritedFromPath}`,
        }))}
      />

      {command.command.examples.length > 0 ? (
        <section className="panel section-card">
          <div className="section-heading">
            <SquareTerminal aria-hidden="true" />
            <h2>Examples</h2>
          </div>
          <div className="example-stack">
            {command.command.examples.map((example) => {
              const resolved = resolveExample(example, command, cliTitle);
              return (
                <div key={example} className="example-wrap">
                  <pre className="example-block">
                    <code>{resolved}</code>
                  </pre>
                  <CopyButton text={resolved} />
                </div>
              );
            })}
          </div>
        </section>
      ) : null}

      {command.command.exitCodes.length > 0 ? (
        <section className="panel section-card">
          <div className="section-heading">
            <Fingerprint aria-hidden="true" />
            <h2>Exit codes</h2>
          </div>
          <div className="detail-grid">
            {command.command.exitCodes.map((exitCode) => (
              <article key={exitCode.code} className="detail-card">
                <strong>{exitCode.code}</strong>
                <p>{exitCode.description ?? "No description"}</p>
              </article>
            ))}
          </div>
        </section>
      ) : null}

      {includeMetadata && command.command.metadata.length > 0 ? (
        <section className="panel section-card">
          <div className="section-heading">
            <Fingerprint aria-hidden="true" />
            <h2>Metadata</h2>
          </div>
          <div className="detail-grid">
            {command.command.metadata.map((item) => (
              <article key={item.name} className="detail-card">
                <strong>{item.name}</strong>
                <p>{typeof item.value === "string" ? item.value : JSON.stringify(item.value)}</p>
              </article>
            ))}
          </div>
        </section>
      ) : null}
    </>
  );
}

function CommandGroup({
  icon,
  title,
  items,
}: {
  icon: ReactNode;
  title: string;
  items: Array<{
    key: string;
    title: string;
    body: string;
    footnote: string;
    action?: () => void;
    actionLabel?: string;
  }>;
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
          <article
            key={item.key}
            className={`detail-card${item.action ? " clickable" : ""}`}
            onClick={item.action}
            role={item.action ? "button" : undefined}
            tabIndex={item.action ? 0 : undefined}
            onKeyDown={item.action ? (e) => { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); item.action!(); } } : undefined}
          >
            <strong>{item.title}</strong>
            <p>{item.body}</p>
            <small>{item.footnote}</small>
          </article>
        ))}
      </div>
    </section>
  );
}

function formatClrType(metadata: { name: string; value?: unknown }[]): string {
  const clrType = getMetadataValue(metadata, "ClrType");
  return clrType ? ` · ${clrType}` : "";
}
