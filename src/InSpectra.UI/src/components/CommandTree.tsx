import { ChevronDown, ChevronRight, TerminalSquare } from "lucide-react";
import { createContext, useCallback, useContext, useEffect, useRef, useState } from "react";
import { NormalizedCommand } from "../data/normalize";

interface CommandTreeProps {
  commands: NormalizedCommand[];
  searchTerm: string;
  selectedPath?: string;
  onSelect: (path: string, hasChildren: boolean) => void;
}

const ExpansionContext = createContext<{
  manualExpanded: Set<string>;
  autoExpanded: Set<string>;
  toggleManual: (path: string) => void;
  toggleAuto: (path: string) => void;
}>({
  manualExpanded: new Set(),
  autoExpanded: new Set(),
  toggleManual: () => {},
  toggleAuto: () => {},
});

export function CommandTree({ commands, searchTerm, selectedPath, onSelect }: CommandTreeProps) {
  const [manualExpanded, setManualExpanded] = useState<Set<string>>(new Set());
  const [autoExpanded, setAutoExpanded] = useState<Set<string>>(new Set());
  const prevSelectedPath = useRef<string | undefined>(undefined);

  useEffect(() => {
    if (prevSelectedPath.current === selectedPath) return;

    const oldPath = prevSelectedPath.current;
    const newPath = selectedPath;
    prevSelectedPath.current = selectedPath;

    setAutoExpanded((prev) => {
      const next = new Set(prev);

      // Auto-expand ancestors of the new selected path
      if (newPath) {
        const parts = newPath.split(" ");
        for (let i = 1; i <= parts.length; i++) {
          next.add(parts.slice(0, i).join(" "));
        }
      }

      // Only collapse the first divergent ancestor of the old path
      if (oldPath) {
        const oldParts = oldPath.split(" ");
        const newParts = newPath ? newPath.split(" ") : [];

        let commonLen = 0;
        while (
          commonLen < oldParts.length &&
          commonLen < newParts.length &&
          oldParts[commonLen] === newParts[commonLen]
        ) {
          commonLen++;
        }

        if (commonLen < oldParts.length) {
          const divergentPath = oldParts.slice(0, commonLen + 1).join(" ");
          next.delete(divergentPath);
        }
      }

      return next;
    });
  }, [selectedPath]);

  const toggleManual = useCallback((path: string) => {
    setManualExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(path)) {
        next.delete(path);
      } else {
        next.add(path);
      }
      return next;
    });
  }, []);

  const toggleAuto = useCallback((path: string) => {
    setAutoExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(path)) {
        next.delete(path);
      } else {
        next.add(path);
      }
      return next;
    });
  }, []);

  if (commands.length === 0) {
    return <p className="sidebar-empty">No commands are available in this snapshot.</p>;
  }

  return (
    <ExpansionContext.Provider value={{ manualExpanded, autoExpanded, toggleManual, toggleAuto }}>
      <div className="command-tree">
        {sortCommands(commands).map((command) => (
          <TreeNode
            key={command.path}
            command={command}
            searchTerm={searchTerm}
            selectedPath={selectedPath}
            onSelect={onSelect}
          />
        ))}
      </div>
    </ExpansionContext.Provider>
  );
}

function TreeNode({
  command,
  searchTerm,
  selectedPath,
  onSelect,
}: {
  command: NormalizedCommand;
  searchTerm: string;
  selectedPath?: string;
  onSelect: (path: string, hasChildren: boolean) => void;
}) {
  const { manualExpanded, autoExpanded, toggleManual, toggleAuto } = useContext(ExpansionContext);
  const normalizedSearch = searchTerm.trim().toLowerCase();
  const matches = matchesCommand(command, normalizedSearch);

  if (!matches) {
    return null;
  }

  const hasChildren = command.commands.length > 0;
  const isSelected = selectedPath === command.path;
  const isManualExpanded = manualExpanded.has(command.path);
  const isAutoExpanded = autoExpanded.has(command.path);
  const isExpanded = normalizedSearch.length > 0 || isManualExpanded || (hasChildren && isAutoExpanded);

  return (
    <div className="tree-node">
      <button
        type="button"
        className={`tree-row ${isSelected ? "selected" : ""}`}
        onClick={() => {
          if (isSelected && hasChildren) {
            toggleAuto(command.path);
          } else {
            onSelect(command.path, hasChildren);
          }
        }}
      >
        <span
          className="tree-toggle"
          onClick={(event) => {
            if (!hasChildren) return;
            event.stopPropagation();
            if (isAutoExpanded && !isManualExpanded) {
              toggleAuto(command.path);
            } else {
              toggleManual(command.path);
            }
          }}
        >
          {hasChildren ? (
            isExpanded ? <ChevronDown aria-hidden="true" /> : <ChevronRight aria-hidden="true" />
          ) : (
            <TerminalSquare aria-hidden="true" />
          )}
        </span>
        <span className="tree-copy">
          <strong>{command.command.name}</strong>
        </span>
      </button>

      {hasChildren && isExpanded ? (
        <div className="tree-children">
          {sortCommands(command.commands).map((child) => (
            <TreeNode
              key={child.path}
              command={child}
              searchTerm={searchTerm}
              selectedPath={selectedPath}
              onSelect={onSelect}
            />
          ))}
        </div>
      ) : null}
    </div>
  );
}

function sortCommands(commands: NormalizedCommand[]): NormalizedCommand[] {
  return [...commands].sort((a, b) => {
    const aHasChildren = a.commands.length > 0 ? 0 : 1;
    const bHasChildren = b.commands.length > 0 ? 0 : 1;
    if (aHasChildren !== bHasChildren) return aHasChildren - bHasChildren;
    return a.command.name.localeCompare(b.command.name);
  });
}

function matchesCommand(command: NormalizedCommand, searchTerm: string): boolean {
  if (searchTerm.length === 0) {
    return true;
  }

  const haystacks = [command.path, command.command.name, command.command.description ?? ""]
    .join(" ")
    .toLowerCase();

  if (haystacks.includes(searchTerm)) {
    return true;
  }

  return command.commands.some((child) => matchesCommand(child, searchTerm));
}
