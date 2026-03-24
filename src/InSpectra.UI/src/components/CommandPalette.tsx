import { Search } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { collectCommandPaths, findCommandByPath, NormalizedCommand } from "../data/normalize";

interface CommandPaletteProps {
  commands: NormalizedCommand[];
  open: boolean;
  onClose: () => void;
  onSelect: (path: string) => void;
}

interface SearchEntry {
  path: string;
  description: string;
}

export function CommandPalette({ commands, open, onClose, onSelect }: CommandPaletteProps) {
  const [query, setQuery] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const resultsRef = useRef<HTMLDivElement>(null);

  const index: SearchEntry[] = buildIndex(commands);

  const filtered = query
    ? index.filter(
        (entry) =>
          entry.path.toLowerCase().includes(query.toLowerCase()) ||
          entry.description.toLowerCase().includes(query.toLowerCase()),
      )
    : index;

  useEffect(() => {
    if (open) {
      setQuery("");
      setActiveIndex(0);
      requestAnimationFrame(() => inputRef.current?.focus());
    }
  }, [open]);

  useEffect(() => {
    setActiveIndex(0);
  }, [query]);

  useEffect(() => {
    const active = resultsRef.current?.querySelector(".cmd-item.active");
    active?.scrollIntoView({ block: "nearest" });
  }, [activeIndex]);

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === "ArrowDown") {
      e.preventDefault();
      setActiveIndex((i) => filtered.length === 0 ? 0 : Math.min(i + 1, filtered.length - 1));
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setActiveIndex((i) => Math.max(i - 1, 0));
    } else if (e.key === "Enter") {
      e.preventDefault();
      if (filtered[activeIndex]) {
        onSelect(filtered[activeIndex].path);
        onClose();
      }
    } else if (e.key === "Escape") {
      e.preventDefault();
      onClose();
    }
  }

  if (!open) return null;

  return (
    <div className="cmd-palette" onKeyDown={handleKeyDown}>
      <div className="cmd-backdrop" onClick={onClose} />
      <div className="cmd-dialog" role="dialog" aria-modal="true" aria-label="Command palette">
        <div className="cmd-header">
          <Search width={16} height={16} />
          <input
            ref={inputRef}
            className="cmd-input"
            type="text"
            placeholder="Search commands…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
          <kbd>Esc</kbd>
        </div>
        <div className="cmd-results" ref={resultsRef}>
          {filtered.length === 0 ? (
            <div className="cmd-empty">No commands found.</div>
          ) : (
            filtered.map((entry, i) => (
              <button
                key={entry.path}
                type="button"
                className={`cmd-item${i === activeIndex ? " active" : ""}`}
                onMouseEnter={() => setActiveIndex(i)}
                onClick={() => {
                  onSelect(entry.path);
                  onClose();
                }}
              >
                <span className="cmd-path">{highlightMatch(entry.path, query)}</span>
                {entry.description && (
                  <span className="cmd-desc">{highlightMatch(entry.description, query)}</span>
                )}
              </button>
            ))
          )}
        </div>
        <div className="cmd-footer">
          <span>
            <kbd>&uarr;</kbd>
            <kbd>&darr;</kbd> Navigate
          </span>
          <span>
            <kbd>&crarr;</kbd> Open
          </span>
          <span>
            <kbd>Esc</kbd> Close
          </span>
        </div>
      </div>
    </div>
  );
}

function buildIndex(commands: NormalizedCommand[]): SearchEntry[] {
  const paths = collectCommandPaths(commands);
  return paths.map((path) => {
    const cmd = findCommandByPath(commands, path);
    return {
      path,
      description: cmd?.command.description ?? "",
    };
  });
}

function highlightMatch(text: string, query: string): React.ReactNode {
  if (!query) return text;
  const lowerText = text.toLowerCase();
  const lowerQuery = query.toLowerCase();
  const idx = lowerText.indexOf(lowerQuery);
  if (idx === -1) return text;

  return (
    <>
      {text.slice(0, idx)}
      <mark>{text.slice(idx, idx + query.length)}</mark>
      {text.slice(idx + query.length)}
    </>
  );
}
