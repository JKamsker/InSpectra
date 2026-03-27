import { Terminal } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { formatOptionValue, NormalizedCommand } from "../data/normalize";
import type { OpenCliArgument, OpenCliOption } from "../data/openCli";
import {
  resolveInputDescriptor,
  getOptionInputDescriptor,
  isBooleanOption,
  validateValue,
  validateOptionValue,
  type InputDescriptor,
} from "../data/validation";
import { CopyButton } from "./CopyButton";

interface ComposerPanelProps {
  command: NormalizedCommand | undefined;
  cliTitle: string;
  isOpen: boolean;
  width: number;
  onResize: (width: number) => void;
  rootArguments?: OpenCliArgument[];
  rootOptions?: OpenCliOption[];
}

export function ComposerPanel({ command, cliTitle, isOpen, width, onResize, rootArguments = [], rootOptions = [] }: ComposerPanelProps) {
  const [flagValues, setFlagValues] = useState<Record<string, boolean>>({});
  const [textValues, setTextValues] = useState<Record<string, string>>({});
  const [argValues, setArgValues] = useState<Record<string, string>>({});
  const [touchedArgs, setTouchedArgs] = useState<Record<string, boolean>>({});
  const [touchedOpts, setTouchedOpts] = useState<Record<string, boolean>>({});
  const resizeRef = useRef<HTMLDivElement>(null);

  const allOptions = command
    ? [
        ...command.declaredOptions,
        ...command.inheritedOptions.map((r) => r.option),
      ]
    : rootOptions;

  const allArguments = command ? command.arguments : rootArguments;

  const boolOptionKeys = new Set(
    allOptions.filter(isBooleanOption).map((o) => o.name),
  );

  const composerKey = command?.path ?? "__root__";

  useEffect(() => {
    setFlagValues({});
    setTextValues({});
    setArgValues({});
    setTouchedArgs({});
    setTouchedOpts({});
  }, [composerKey]);

  function buildPreview(): string {
    const parts = command
      ? [cliTitle, ...command.path.split(" ")]
      : [cliTitle];

    for (const arg of allArguments) {
      const value = argValues[arg.name];
      if (value?.trim()) {
        parts.push(value.includes(" ") ? `"${value}"` : value);
      }
    }

    const nameToDisplay = new Map(
      allOptions.map((opt) => [opt.name, opt.aliases?.[0] ?? opt.name]),
    );

    for (const [key, enabled] of Object.entries(flagValues)) {
      if (!enabled) continue;
      parts.push(nameToDisplay.get(key) ?? key);
      if (boolOptionKeys.has(key)) parts.push("true");
    }

    for (const [key, value] of Object.entries(textValues)) {
      if (value.trim()) {
        parts.push(nameToDisplay.get(key) ?? key);
        parts.push(value.includes(" ") ? `"${value}"` : value);
      }
    }

    return parts.join(" ");
  }

  function getValidationIssues(): { missing: number; invalid: number } {
    let missing = 0;
    let invalid = 0;

    for (const arg of allArguments) {
      const error = validateValue(argValues[arg.name] ?? "", arg, arg.required);
      if (error === "Required") missing++;
      else if (error) invalid++;
    }

    for (const opt of allOptions) {
      if (opt.arguments.length === 0 || boolOptionKeys.has(opt.name)) continue;
      const error = validateOptionValue(textValues[opt.name] ?? "", opt);
      if (error === "Required") missing++;
      else if (error) invalid++;
    }

    return { missing, invalid };
  }

  function handleResizeStart(e: React.MouseEvent) {
    e.preventDefault();
    const handle = resizeRef.current;
    if (handle) handle.classList.add("dragging");
    document.body.style.userSelect = "none";
    let latestWidth = width;

    function onMove(ev: MouseEvent) {
      latestWidth = Math.min(576, Math.max(224, document.documentElement.clientWidth - ev.clientX));
      onResize(latestWidth);
    }

    function onUp() {
      document.removeEventListener("mousemove", onMove);
      document.removeEventListener("mouseup", onUp);
      document.body.style.userSelect = "";
      if (handle) handle.classList.remove("dragging");
      localStorage.setItem("inspectra-composer-width", String(latestWidth));
    }

    document.addEventListener("mousemove", onMove);
    document.addEventListener("mouseup", onUp);
  }

  function renderSelectInput(
    id: string,
    value: string,
    descriptor: InputDescriptor,
    hasError: boolean,
    onChange: (v: string) => void,
    onBlur: () => void,
  ) {
    return (
      <select
        id={id}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        onBlur={onBlur}
        className={`composer-select${hasError ? " composer-input-error" : ""}`}
      >
        <option value="">-- select --</option>
        {(descriptor.selectOptions ?? []).map((opt) => (
          <option key={opt} value={opt}>{opt}</option>
        ))}
      </select>
    );
  }

  function renderTextInput(
    id: string,
    value: string,
    placeholder: string,
    descriptor: InputDescriptor | null,
    hasError: boolean,
    onChange: (v: string) => void,
    onBlur: () => void,
  ) {
    return (
      <input
        id={id}
        type="text"
        inputMode={descriptor?.kind === "number" ? "numeric" : undefined}
        placeholder={placeholder}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        onBlur={onBlur}
        className={`composer-input${hasError ? " composer-input-error" : ""}`}
      />
    );
  }

  const issues = getValidationIssues();
  const hasIssues = issues.missing > 0 || issues.invalid > 0;

  return (
    <aside className={`composer${isOpen ? " composer-open" : ""}`} style={{ width }}>
      <div className="composer-resize" ref={resizeRef} onMouseDown={handleResizeStart} />
      <div className="composer-header">
        <Terminal width={16} height={16} />
        Composer
      </div>
      <div className="composer-body">
        {!command && allOptions.length === 0 && allArguments.length === 0 ? (
          <div className="composer-empty">Select a command to start composing.</div>
        ) : allOptions.length === 0 && allArguments.length === 0 ? (
          <div className="composer-empty">This command has no configurable options.</div>
        ) : (
          <>
            <div className="composer-section-title">
              {command ? <>Options for <code>{command.path}</code></> : "Root options"}
            </div>

            {/* ── Arguments ── */}
            {allArguments.map((argument) => {
              const key = argument.name;
              const inputId = `composer-arg-${key.replace(/[^a-zA-Z0-9-]/g, "-")}`;
              const descriptor = resolveInputDescriptor(argument);
              const value = argValues[key] ?? "";
              const error = validateValue(value, argument, argument.required);
              const isTouched = touchedArgs[key];
              const showError = isTouched && error;

              const onBlur = () => setTouchedArgs((prev) => ({ ...prev, [key]: true }));
              const onChange = (v: string) => {
                setArgValues((prev) => ({ ...prev, [key]: v }));
                if (!touchedArgs[key]) setTouchedArgs((prev) => ({ ...prev, [key]: true }));
              };

              const isSelect = descriptor.kind === "select" || descriptor.kind === "boolean";

              return (
                <div className="composer-field" key={key}>
                  <label className="composer-opt-name" htmlFor={inputId}>
                    {key}
                    {argument.required && <span className="composer-required">*</span>}
                    {descriptor.typeLabel && <span className="composer-type-hint">{descriptor.typeLabel}</span>}
                  </label>
                  {isSelect
                    ? renderSelectInput(inputId, value, descriptor, !!showError, onChange, onBlur)
                    : renderTextInput(
                        inputId,
                        value,
                        argument.acceptedValues.length > 0 ? argument.acceptedValues.join(" | ") : key,
                        descriptor,
                        !!showError,
                        onChange,
                        onBlur,
                      )}
                  {showError && <span className="composer-error">{error}</span>}
                  {argument.description && (
                    <span className="composer-opt-desc">{argument.description}</span>
                  )}
                </div>
              );
            })}

            {/* ── Options ── */}
            {allOptions.map((option) => {
              const kind = formatOptionValue(option);
              const key = option.name;
              const displayName = option.aliases?.[0] ?? option.name;
              const inputId = `composer-opt-${key.replace(/[^a-zA-Z0-9-]/g, "-")}`;

              if (kind === "flag" || boolOptionKeys.has(key)) {
                return (
                  <div className="composer-field" key={key}>
                    <label className="composer-flag">
                      <input
                        type="checkbox"
                        checked={flagValues[key] ?? false}
                        onChange={(e) =>
                          setFlagValues((prev) => ({ ...prev, [key]: e.target.checked }))
                        }
                      />
                      <div>
                        <span className="composer-opt-name">
                          {displayName}
                          {option.required && <span className="composer-required">*</span>}
                        </span>
                        {option.description && (
                          <span className="composer-opt-desc">{option.description}</span>
                        )}
                      </div>
                    </label>
                  </div>
                );
              }

              const descriptor = getOptionInputDescriptor(option);
              const value = textValues[key] ?? "";
              const error = validateOptionValue(value, option);
              const isTouched = touchedOpts[key];
              const showError = isTouched && error;

              const onBlur = () => setTouchedOpts((prev) => ({ ...prev, [key]: true }));
              const onChange = (v: string) => {
                setTextValues((prev) => ({ ...prev, [key]: v }));
                if (!touchedOpts[key]) setTouchedOpts((prev) => ({ ...prev, [key]: true }));
              };

              const isSelect = descriptor && descriptor.kind === "select";

              return (
                <div className="composer-field" key={key}>
                  <label className="composer-opt-name" htmlFor={inputId}>
                    {displayName}
                    {option.required && <span className="composer-required">*</span>}
                    {descriptor?.typeLabel && <span className="composer-type-hint">{descriptor.typeLabel}</span>}
                  </label>
                  {isSelect
                    ? renderSelectInput(inputId, value, descriptor, !!showError, onChange, onBlur)
                    : renderTextInput(inputId, value, kind, descriptor, !!showError, onChange, onBlur)}
                  {showError && <span className="composer-error">{error}</span>}
                  {option.description && (
                    <span className="composer-opt-desc">{option.description}</span>
                  )}
                </div>
              );
            })}
          </>
        )}
      </div>
      <div className="composer-footer">
        <span className="composer-label">Generated Command</span>
        {hasIssues && (
          <div className="composer-validation-summary">
            {issues.missing > 0 && (
              <span>{issues.missing} required {issues.missing === 1 ? "field" : "fields"} missing</span>
            )}
            {issues.missing > 0 && issues.invalid > 0 && <span> &middot; </span>}
            {issues.invalid > 0 && (
              <span>{issues.invalid} invalid {issues.invalid === 1 ? "value" : "values"}</span>
            )}
          </div>
        )}
        <div className="composer-output-wrap">
          <pre className="composer-output">{buildPreview()}</pre>
          <CopyButton text={buildPreview()} className="composer-copy" title="Copy command" />
        </div>
      </div>
    </aside>
  );
}
